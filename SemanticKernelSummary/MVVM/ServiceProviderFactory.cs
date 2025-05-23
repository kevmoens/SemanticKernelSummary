using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelSummary.MVVM
{
	public class ServiceProviderFactory<T>(IServiceProvider serviceProvider, IDynamicServiceProvider dynamicServiceProvider) : IFactory<T> where T : class
	{
		private readonly IServiceProvider _serviceProvider = serviceProvider;
		private readonly IDynamicServiceProvider _dynamicServiceProvider = dynamicServiceProvider;

		public T Create()
		{
			T? instance = _serviceProvider.GetService<T>();
			instance  ??= _dynamicServiceProvider.GetService<T>();
			return instance;
		}
		public T Create(string key)
		{
			T? instance = _serviceProvider.GetKeyedService<T>(key);
			instance ??= _dynamicServiceProvider.GetService<T>(key);
			return instance;
		}
	}
}
