using Microsoft.SemanticKernel;
using SemanticKernelSummary.MVVM;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SemanticKernelSummary.AI
{
    public class TextSummarizer(IFactory<IPromptExecutionSettings> _promptExecutionSettingsFactory, IFactory<Kernel> _chatKernelFactory)
    {
        public string SelectedModel { get; set; } = string.Empty;

        public async Task<string> RecursiveSummarize(string largeText)
        {
            // Create glossary entries and generate embeddings for them.
            var glossaryEntries = TextChunker.ChunkFile(largeText).ToList();

            // Replace the original chunking loop in RecursiveSummarize with this rate-limited version
            var summaryParts = new ConcurrentDictionary<SummaryRecord, string>();
            await SummarizeRateLimitProcessing(glossaryEntries, summaryParts);

            largeText = SummarizeCombineResults(glossaryEntries, summaryParts);

            if (MoreChunkingNeeded(largeText, out glossaryEntries) == false)
            {
                return glossaryEntries.First().Paragraph;
            }

            return await RecursiveSummarize(largeText);
        }

        private static bool MoreChunkingNeeded(string largeText, out List<SummaryRecord> summaryRecords)
        {
            summaryRecords = [.. TextChunker.ChunkFile(largeText)];
            return summaryRecords.Count > 1;
        }

        private static string SummarizeCombineResults(List<SummaryRecord> glossaryEntries, ConcurrentDictionary<SummaryRecord, string> summaryParts)
        {
            string largeText;
            var sb = new StringBuilder();
            foreach (var chunk in glossaryEntries)
            {
                if (summaryParts.TryGetValue(chunk, out var summaryPart))
                {
                    sb.AppendLine(summaryPart);
                }
            }
            largeText = sb.ToString();
            return largeText;
        }

        private async Task SummarizeRateLimitProcessing(List<SummaryRecord> glossaryEntries, ConcurrentDictionary<SummaryRecord, string> summaryParts)
        {
            var summaryTasks = new List<Task>();
            using var semaphore = new SemaphoreSlim(5); // Limit to 5 concurrent tasks

            int processedChunks = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            var chunkEnumerator = glossaryEntries.GetEnumerator();
            while (chunkEnumerator.MoveNext())
            {
                await semaphore.WaitAsync();
                var chunk = chunkEnumerator.Current;
                var task = Task.Run(async () =>
                {
                    try
                    {
                        var result = await _chatKernelFactory
                            .Create(SelectedModel + ModelCategory.Chat.ToString())
                            .InvokePromptAsync("Summarize this text: " + chunk.Paragraph, GetChatExecutionSettings());
                        summaryParts.TryAdd(chunk, result.GetValue<string>()!);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });
                summaryTasks.Add(task);
                processedChunks++;

                // Rate limit: chunks per minute
                const int rateLimit = 30;
                if (processedChunks % rateLimit == 0 && (processedChunks * (60 / rateLimit)) > stopwatch.Elapsed.TotalSeconds)
                {
                    await Task.Delay(TimeSpan.FromSeconds((processedChunks * (60 / rateLimit)) - stopwatch.Elapsed.TotalSeconds));
                }
            }
            stopwatch.Stop();
            await Task.WhenAll(summaryTasks);
        }


        private KernelArguments GetChatExecutionSettings()
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
            return args;
        }
    }
}
