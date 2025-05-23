using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelSummary.MVVM
{
	public interface IFactory<T>
	{
		T Create();
		T Create(string key);
	}
}
