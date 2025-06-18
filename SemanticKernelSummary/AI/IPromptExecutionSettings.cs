using Microsoft.SemanticKernel;
using System.Collections.Generic;

namespace SemanticKernelSummary.AI
{
    public interface IPromptExecutionSettings
    {

		IDictionary<string, object>? ExtensionData { get; set; }
		FunctionChoiceBehavior? FunctionChoiceBehavior { get; set; }
		string? ServiceId { get; set; }
		bool IsFrozen { get; }
		string? ModelId { get; set; }
		IPromptExecutionSettings Clone();
		void Freeze();
		PromptExecutionSettings Cast();

	}
}
