namespace SemanticKernelSummary.MVVM
{
	public interface IFactory<T>
	{
		T Create();
		T Create(string key);
	}
}
