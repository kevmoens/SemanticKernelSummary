using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelSummary.AI
{
	public class AzureOpenAIPromptExecutionSettings : IPromptExecutionSettings, IPromptExecutionSettingsTemperature, IPromptExecutionSettingsNumPedict
	{
		readonly Microsoft.SemanticKernel.Connectors.AzureOpenAI.AzureOpenAIPromptExecutionSettings _settings = new();
		public IDictionary<string, object>? ExtensionData { get => _settings.ExtensionData; set => _settings.ExtensionData = value; }
		public FunctionChoiceBehavior? FunctionChoiceBehavior { get => _settings.FunctionChoiceBehavior; set => _settings.FunctionChoiceBehavior = value; }
		public string? ServiceId { get => _settings.ServiceId; set => _settings.ServiceId = value; }
		public bool IsFrozen { get => _settings.IsFrozen; }
		public string? ModelId { get => _settings.ModelId; set => _settings.ModelId = value; }
		public float? Temperature { get => (float?)_settings.Temperature; set => _settings.Temperature = value; }
		public int? NumPredict { get => _settings.MaxTokens; set => _settings.MaxTokens = value; }

		public PromptExecutionSettings Cast()
		{
			return (PromptExecutionSettings)_settings;
		}

		public IPromptExecutionSettings Clone()
		{
			var cloned = _settings.Clone();
			return new AzureOpenAIPromptExecutionSettings()
			{
				ExtensionData = cloned.ExtensionData,
				FunctionChoiceBehavior = cloned.FunctionChoiceBehavior,
				ServiceId = cloned.ServiceId,
				ModelId = cloned.ModelId,
				Temperature = (float?)((Microsoft.SemanticKernel.Connectors.AzureOpenAI.AzureOpenAIPromptExecutionSettings)cloned).Temperature,
				NumPredict = ((Microsoft.SemanticKernel.Connectors.AzureOpenAI.AzureOpenAIPromptExecutionSettings)cloned).MaxTokens
			};
		}

		public void Freeze()
		{
			_settings.Freeze();
		}
	}
}
