using Microsoft.SemanticKernel;
using System.Collections.Generic;

namespace SemanticKernelSummary.AI
{
	public class OllamaPromptExecutionSettings : IPromptExecutionSettings, IPromptExecutionSettingsTemperature, IPromptExecutionSettingsNumPedict
	{
		readonly Microsoft.SemanticKernel.Connectors.Ollama.OllamaPromptExecutionSettings _settings = new();

		public IDictionary<string, object>? ExtensionData { get => _settings.ExtensionData; set => _settings.ExtensionData = value; }
		public FunctionChoiceBehavior? FunctionChoiceBehavior { get => _settings.FunctionChoiceBehavior; set => _settings.FunctionChoiceBehavior = value; }
		public string? ServiceId { get => _settings.ServiceId; set => _settings.ServiceId = value; }
		public bool IsFrozen { get => _settings.IsFrozen; }
		public string? ModelId { get => _settings.ModelId; set => _settings.ModelId = value; }
		public float? Temperature { get => _settings.Temperature; set => _settings.Temperature = value; }
		public int? NumPredict { get => _settings.NumPredict; set => _settings.NumPredict = value; }

		public PromptExecutionSettings Cast()
		{
			return (PromptExecutionSettings)_settings;
		}

		public IPromptExecutionSettings Clone()
		{
			var cloned= _settings.Clone();
			return new OllamaPromptExecutionSettings()
			{
				ExtensionData = cloned.ExtensionData,
				FunctionChoiceBehavior = cloned.FunctionChoiceBehavior,
				ServiceId = cloned.ServiceId,
				ModelId = cloned.ModelId,
				Temperature = ((Microsoft.SemanticKernel.Connectors.Ollama.OllamaPromptExecutionSettings)cloned).Temperature,
				NumPredict = ((Microsoft.SemanticKernel.Connectors.Ollama.OllamaPromptExecutionSettings)cloned).NumPredict
			};		
		}

		public void Freeze()
		{
			_settings.Freeze();
		}
	}
}
