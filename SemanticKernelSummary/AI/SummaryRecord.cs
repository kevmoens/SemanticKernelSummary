using Microsoft.Extensions.VectorData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelSummary.AI
{
	public sealed class SummaryRecord
	{
		[VectorStoreRecordKey]
		public ulong Key { get; set; }

		[VectorStoreRecordData]
		public string Paragraph { get; set; } = string.Empty;

		[VectorStoreRecordVector(1536)]
		public ReadOnlyMemory<float> DefinitionEmbedding { get; set; }

	}
}
