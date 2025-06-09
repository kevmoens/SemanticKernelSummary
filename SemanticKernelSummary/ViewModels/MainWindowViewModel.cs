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
using Microsoft.SemanticKernel.Text;
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

		public event PropertyChangedEventHandler? PropertyChanged;

		protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
		public MainWindowViewModel(IFactory<Kernel> chatKernelFactory,
							 IFactory<IEmbeddingGenerator<string, Embedding<float>>> embedGenFactory,
							 IFactory<IPromptExecutionSettings> promptExecutionSettingsFactory,
							 IDynamicServiceProvider dynamicServiceProvider)
		{
			_chatKernelFactory = chatKernelFactory;
			_embedGenFactory = embedGenFactory;
			_promptExecutionSettingsFactory = promptExecutionSettingsFactory;
			_dynamicServiceProvider = dynamicServiceProvider;
			OpenFileCommand = new DelegateCommand(OnOpenFile);
			ConnectCommand = new DelegateCommand(OnConnect);
			LLMPromptCommand = new DelegateCommand(OnLLMPrompt);
			LLMResetCommand = new DelegateCommand(OnLLMReset);
            SummarizeFileCommand = new DelegateCommand(OnSummarize);
			RAGCommand = new DelegateCommand(OnRAG);
		}
		private ChatHistory _chatHistory = new ChatHistory();

        private string _filePath = @"Data\MongooseIntegration.txt";

		public string FilePath
		{
			get { return _filePath; }
			set { _filePath = value; OnPropertyChanged(); }
		}

		private ObservableCollection<string> _models = [ModelNames.AzureOpenAI.ToString(), ModelNames.Ollama.ToString()];

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
		public async void OnRAG()
		{
			Result = string.Empty;
			await RAG();
		}
		public async void OnSummarize()
		{
			Result = string.Empty;
			await Summarize();
		}
		private async Task Summarize()
		{

			var largeText = System.IO.File.ReadAllText(FilePath);
			string result = await RecursiveSummarize(largeText);

			Result = result;
		}
		private async Task<string> RecursiveSummarize(string largeText)
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

			// Create glossary entries and generate embeddings for them.
			var glossaryEntries = ChuckFile(largeText).ToList();

			var summaryParts = new List<string>();
			foreach (var chunk in glossaryEntries)
			{
				var summary = await _chatKernelFactory.Create(SelectedModel + ModelCategory.Chat.ToString()).InvokePromptAsync("Summarize this text: " + chunk.Paragraph, args);
				summaryParts.Add(summary.GetValue<string>()!);
			}

			glossaryEntries = [.. ChuckFile(largeText)];
			if (glossaryEntries.Count == 1)
			{
				return glossaryEntries.First().Paragraph;
			}

			return await RecursiveSummarize(string.Join("\n", summaryParts));
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
			var glossaryEntries = ChuckFile(largeText, 1000).ToList();
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
		public static IEnumerable<SummaryRecord> ChuckFile(string largeText, int chunckSize = 4000)
		{
			var records = new List<SummaryRecord>();


			// Define a token counter (optional)
			static int tokenCounter(string text) => text.Split(' ').Length;

			// Split the text into lines
			var lines = TextChunker.SplitPlainTextLines(largeText, maxTokensPerLine: 500, tokenCounter);

			// Further split lines into paragraphs with overlap
			var paragraphs = TextChunker.SplitPlainTextParagraphs(
				lines,
				maxTokensPerParagraph: chunckSize,
				overlapTokens: 50,
				chunkHeader: "Document Chunk:",
				tokenCounter: tokenCounter
			);
			int i = 0;
			foreach (var paragraph in paragraphs)
			{ 
				var record = new SummaryRecord
					{
						Key = (ulong)i,
						Paragraph = paragraph,
						DefinitionEmbedding = new ReadOnlyMemory<float>(new float[1536])
					};
					records.Add(record);
		
				i++;
			}
			return records;
		}

		public async void OnLLMPrompt()
		{
			Result = string.Empty;
			Kernel kernel = _chatKernelFactory.Create(SelectedModel + ModelCategory.Chat.ToString());
            IChatCompletionService chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
            _chatHistory.AddUserMessage(Question);
			StringBuilder response = new StringBuilder();
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
