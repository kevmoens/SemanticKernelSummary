using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelSummary.AI
{
    public class TextChunker
    {

        public static IEnumerable<SummaryRecord> ChunkFile(string largeText, int chunckSize = 4000)
        {
            var records = new List<SummaryRecord>();


            // Define a token counter (optional)
            static int tokenCounter(string text)
            {
                //var tokenizer = Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel("gpt-4");
                //return tokenizer.CountTokens(text);
                return text.Split(' ').Length;
            }

            // Split the text into lines
            var lines = Microsoft.SemanticKernel.Text.TextChunker.SplitPlainTextLines(largeText, maxTokensPerLine: 500, tokenCounter);

            // Further split lines into paragraphs with overlap
            var paragraphs = Microsoft.SemanticKernel.Text.TextChunker.SplitPlainTextParagraphs(
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
    }
}
