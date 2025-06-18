using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.InMemory;
using SemanticKernelSummary.AI;
using SemanticKernelSummary.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;

namespace SemanticKernelSummary.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly IFactory<Kernel> _chatKernelFactory;
        private readonly IFactory<IEmbeddingGenerator<string, Embedding<float>>> _embedGenFactory;
        private readonly IFactory<IPromptExecutionSettings> _promptExecutionSettingsFactory;
        private readonly IDynamicServiceProvider _dynamicServiceProvider;
        private readonly TextSummarizer _textSummarizer;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public MainWindowViewModel(IFactory<Kernel> chatKernelFactory,
                             IFactory<IEmbeddingGenerator<string, Embedding<float>>> embedGenFactory,
                             IFactory<IPromptExecutionSettings> promptExecutionSettingsFactory,
                             IDynamicServiceProvider dynamicServiceProvider,
                             TextSummarizer textSummarizer)
        {
            _chatKernelFactory = chatKernelFactory;
            _embedGenFactory = embedGenFactory;
            _promptExecutionSettingsFactory = promptExecutionSettingsFactory;
            _dynamicServiceProvider = dynamicServiceProvider;
            _textSummarizer = textSummarizer;
            OpenFileCommand = new DelegateCommand(OnOpenFile);
            ConnectCommand = new DelegateCommand(OnConnect);
            LLMPromptCommand = new DelegateCommand(OnLLMPrompt);
            LLMResetCommand = new DelegateCommand(OnLLMReset);
            SummarizeFileCommand = new DelegateCommand(OnSummarize);
            RAGCommand = new DelegateCommand(OnRAG);
        }
        private readonly ChatHistory _chatHistory = [];

        private string _filePath = @"Data\MongooseIntegration.txt";

        public string FilePath
        {
            get { return _filePath; }
            set { _filePath = value; OnPropertyChanged(); }
        }

        private ObservableCollection<string> _models = [ModelNames.AzureOpenAI.ToString(), ModelNames.Ollama.ToString(), ModelNames.ChatGPT.ToString()];

        public ObservableCollection<string> Models
        {
            get { return _models; }
            set { _models = value; OnPropertyChanged(); }
        }

        private string _selectedModel = "Ollama";

        public string SelectedModel
        {
            get { return _selectedModel; }
            set { _selectedModel = value; OnPropertyChanged(); }
        }

        private string _question = "List the wars the united states has been in and only list the wars and nothing else.";

        public string Question
        {
            get { return _question; }
            set { _question = value; OnPropertyChanged(); }
        }
        private string _result = string.Empty;

        public string Result
        {
            get { return _result; }
            set { _result = value; OnPropertyChanged(); }
        }
        private bool _useChunking;

        public bool UseChunking
        {
            get { return _useChunking; }
            set { _useChunking = value; OnPropertyChanged(); }
        }

        public ICommand OpenFileCommand { get; set; }
        public ICommand ConnectCommand { get; set; }
        public ICommand LLMPromptCommand { get; set; }
        public ICommand LLMResetCommand { get; set; }
        public ICommand SummarizeFileCommand { get; set; }
        public ICommand RAGCommand { get; set; }

        public void OnOpenFile()
        {
            var fileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
                InitialDirectory = Environment.CurrentDirectory
            };
            if (fileDialog.ShowDialog() == true)
            {
                FilePath = fileDialog.FileName;
            }

        }
        public void OnConnect()
        {
            AddOllama();
            AddAzureOpenAI();
            AddChatGPT();
        }

        private void AddOllama()
        {
            var modelId = System.Configuration.ConfigurationManager.AppSettings["OllamaModelId"]!;

            var endpointString = System.Configuration.ConfigurationManager.AppSettings["OllamaEndPoint"]!;
            if (string.IsNullOrWhiteSpace(endpointString))
            {
                throw new InvalidOperationException("OllamaModelId appSetting is missing or empty in app.config.");
            }
            var endpoint = new Uri(endpointString!);

            //Chat
            Kernel kernel = Kernel.CreateBuilder()
                .AddOllamaChatCompletion(modelId, endpoint)
                .Build();
            _dynamicServiceProvider.AddSingleton<Kernel>(ModelNames.Ollama.ToString() + ModelCategory.Chat.ToString(), kernel);

            _dynamicServiceProvider.AddTransient<IPromptExecutionSettings>(ModelNames.Ollama.ToString(), () => new OllamaPromptExecutionSettings());

            //Embedding
            var builder = Kernel.CreateBuilder();
            builder.Services.AddOllamaEmbeddingGenerator(modelId, endpoint);
            builder.Services.AddInMemoryVectorStore();
            Kernel embedKernel = builder.Build();

            var embeddingGenerator = embedKernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            _dynamicServiceProvider.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(ModelNames.Ollama.ToString() + ModelCategory.Embedding.ToString(), embeddingGenerator);
        }
        private void AddAzureOpenAI()
        {
            Kernel kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: System.Configuration.ConfigurationManager.AppSettings["AzureDeploymentName"]!,
                    endpoint: System.Configuration.ConfigurationManager.AppSettings["AzureEndPoint"]!,
                    apiKey: System.Configuration.ConfigurationManager.AppSettings["AzureApiKey"]!,
                    modelId: System.Configuration.ConfigurationManager.AppSettings["AzureModelId"]!)
                .Build();
            _dynamicServiceProvider.AddSingleton<Kernel>(ModelNames.AzureOpenAI.ToString() + ModelCategory.Chat.ToString(), kernel);

            _dynamicServiceProvider.AddTransient<IPromptExecutionSettings>(ModelNames.AzureOpenAI.ToString(), () => new OllamaPromptExecutionSettings());


            var embedKernel = Kernel.CreateBuilder()
                .AddAzureOpenAIEmbeddingGenerator(
                    deploymentName: System.Configuration.ConfigurationManager.AppSettings["AzureEmbedDeploymentName"]!,
                    endpoint: System.Configuration.ConfigurationManager.AppSettings["AzureEmbedEndPoint"]!,
                    apiKey: System.Configuration.ConfigurationManager.AppSettings["AzureEmbedApiKey"]!,
                    modelId: System.Configuration.ConfigurationManager.AppSettings["AzureEmbedModelId"]!)
                .AddInMemoryVectorStore()
                .Build();


            var embeddingGenerator = embedKernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            _dynamicServiceProvider.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(ModelNames.AzureOpenAI.ToString() + ModelCategory.Embedding.ToString(), embeddingGenerator);
        }
        private void AddChatGPT()
        {
            // LLM Chat Model
            Kernel kernel = Kernel.CreateBuilder()
                .AddOpenAIChatCompletion(
                modelId: System.Configuration.ConfigurationManager.AppSettings["ChatGPTModelID"]!,
                apiKey: System.Configuration.ConfigurationManager.AppSettings["ChatGPTApiKey"]!
                )
                .Build();
            _dynamicServiceProvider.AddSingleton<Kernel>(ModelNames.ChatGPT.ToString() + ModelCategory.Chat.ToString(), kernel);

            //Hacky but can we get away with OllamaPromptExecutionSettings?
            _dynamicServiceProvider.AddTransient<IPromptExecutionSettings>(ModelNames.ChatGPT.ToString(), () => new OllamaPromptExecutionSettings()); //We may need to wrap our own interface to handle because OpenAI doesn't use interface and the other 2 does


            // LLM Embedding Model
            var embedKernel = Kernel.CreateBuilder()
                .AddOpenAIEmbeddingGenerator(
                   modelId: System.Configuration.ConfigurationManager.AppSettings["ChatGPTEmbedModelID"]!,
                   apiKey: System.Configuration.ConfigurationManager.AppSettings["ChatGPTApiKey"]!
                )
                .Build();
            var embeddingGenerator = embedKernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
            _dynamicServiceProvider.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(ModelNames.ChatGPT.ToString() + ModelCategory.Embedding.ToString(), embeddingGenerator);
        }
        public async void OnRAG()
        {
            Result = string.Empty;
            await RAG();
        }
        public async void OnSummarize()
        {
            Result = string.Empty;
            if (UseChunking)
            {
                await Summarize();
                return;
            }

            await SummarizeOneRequest();

        }

        private async Task SummarizeOneRequest()
        {
            IPromptExecutionSettings settings = _promptExecutionSettingsFactory.Create(SelectedModel);
            if (settings is IPromptExecutionSettingsNumPedict pedict)
            {
                pedict.NumPredict = 500;
            }
            if (settings is IPromptExecutionSettingsTemperature temperature)
            {
                temperature.Temperature = 0.1f;
            }
            KernelArguments args = new(settings.Cast());
            var largeText = System.IO.File.ReadAllText(FilePath);

            // Note: If you are using Azure OpenAI, you may hit the rate limit for tokens.Getting this error:
            // Microsoft.SemanticKernel.HttpOperationException: 'HTTP 429 (: 429)
            // Requests to the ChatCompletions_Create Operation under Azure OpenAI API version 2025 - 03 - 01 - preview have exceeded token rate limit of your current AIServices S0 pricing tier. Please retry after 60 seconds.Please go here: https://aka.ms/oai/quotaincrease if you would like to further increase the default rate limit. For Free Account customers, upgrade to Pay as you Go here: https://aka.ms/429TrialUpgrade.'

            var summary = await _chatKernelFactory.Create(SelectedModel + ModelCategory.Chat.ToString()).InvokePromptAsync("Summarize this text: " + largeText, args);
            Result = summary.GetValue<string>()!;
        }

        private async Task Summarize()
        {
            var largeText = System.IO.File.ReadAllText(FilePath);
            _textSummarizer.SelectedModel = SelectedModel;
            string result = await _textSummarizer.RecursiveSummarize(largeText);
            Result = result;
        }

        private async Task RAG()
        {
            if (string.IsNullOrWhiteSpace(Question))
            {
                MessageBox.Show("Please fill out a question to ask about the File");
                return;
            }

            // Construct an InMemory vector store.
            var vectorStore = new InMemoryVectorStore(new InMemoryVectorStoreOptions() { EmbeddingGenerator = _embedGenFactory.Create(SelectedModel + ModelCategory.Embedding.ToString()) });

            // Get and create collection if it doesn't exist.
            var collection = vectorStore.GetCollection<ulong, SummaryRecord>("skglossary");
            await collection.CreateCollectionIfNotExistsAsync();

            var largeText = System.IO.File.ReadAllText(FilePath);
            // Create glossary entries and generate embeddings for them.
            var glossaryEntries = TextChunker.ChunkFile(largeText, 1000).ToList();
            var tasks = glossaryEntries.Select(entry => Task.Run(async () =>
            {
                entry.DefinitionEmbedding = (await _embedGenFactory.Create(SelectedModel + ModelCategory.Embedding.ToString()).GenerateAsync(entry.Paragraph)).Vector;
            }));
            await Task.WhenAll(tasks);

            // Upsert the glossary entries into the collection and return their keys.
            var upsertedKeysTasks = glossaryEntries.Select(x => collection.UpsertAsync(x));
            var upsertedKeys = await Task.WhenAll(upsertedKeysTasks);

            var queryEmbedding = (await _embedGenFactory.Create(SelectedModel + ModelCategory.Embedding.ToString()).GenerateAsync(Question)).Vector;

            IAsyncEnumerable<VectorSearchResult<SummaryRecord>> results = collection.SearchEmbeddingAsync(
                queryEmbedding,
                 5
                 );

            IPromptExecutionSettings settings = _promptExecutionSettingsFactory.Create(SelectedModel);
            if (settings is IPromptExecutionSettingsNumPedict pedict)
            {
                pedict.NumPredict = 1000;
            }
            if (settings is IPromptExecutionSettingsTemperature temperature)
            {
                temperature.Temperature = 0.1f;
            }

            KernelArguments args = new(settings.Cast());
            var summaryParts = new List<string>();
            await foreach (var chunk in results)
            {
                var summary = await _chatKernelFactory.Create(SelectedModel + ModelCategory.Chat.ToString()).InvokePromptAsync($"{Question} : from this text: " + chunk.Record.Paragraph, arguments: args);
                summaryParts.Add(summary.GetValue<string>()!);
            }

            var finalSummary = await _chatKernelFactory.Create(SelectedModel + ModelCategory.Chat.ToString()).InvokePromptAsync($"{Question} : from this text: " + (string.Join("\n", summaryParts)));
            Result = finalSummary.GetValue<string>()!;
        }

        public async void OnLLMPrompt()
        {
            Result = string.Empty;
            Kernel kernel = _chatKernelFactory.Create(SelectedModel + ModelCategory.Chat.ToString());
            IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            _chatHistory.AddUserMessage(Question);
            StringBuilder response = new();
            await foreach (var update in chatCompletion.GetStreamingChatMessageContentsAsync(_chatHistory))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    Result += update.Content;
                });
                response.Append(update.Content);
                await Task.Delay(1);
            }

            _chatHistory.AddAssistantMessage(response.ToString());
        }

        public void OnLLMReset()
        {
            _chatHistory.Clear();
        }

    }
}
