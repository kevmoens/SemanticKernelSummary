using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelSummary.MVVM
{
	public class DynamicServiceProvider : IDynamicServiceProvider
	{
		private readonly ConcurrentDictionary<(Type, string), Func<object>> _factories = new();
		public DynamicServiceProvider()
		{
			
		}
		public T GetService<T>() where T : class
		{
			return GetService<T>(string.Empty);
		}

		public T GetService<T>(string key) where T : class
		{
			var registrationKey = (typeof(T), key);

			if (_factories.TryGetValue(registrationKey, out var factory))
			{
				return (T)factory();
			}

			throw new KeyNotFoundException(
				$"Service of type {typeof(T).Name} with key '{key}' was not registered.");
		}
		
		public void AddSingleton<T>(string key, T instance) where T : class
		{
			AddTransient<T>(key, () => instance);
		}


		public void AddSingleton<T>(T instance) where T : class
		{
            AddTransient<T>(string.Empty, () => instance);
		}

		public void AddTransient<T>(Func<T> factory) where T : class
		{
			AddTransient(string.Empty, factory);
		}

		public void AddTransient<T>(string key, Func<T> factory) where T : class
		{
			var registrationKey = (typeof(T), key);

			// TryAdd ensures we're not overwriting existing registrations.
			if (!_factories.TryAdd(registrationKey, () => factory()))
			{
				throw new ArgumentException(
					$"A service of type {typeof(T).Name} with key '{key}' is already registered.");
			}
		}
	}
}
