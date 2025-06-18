using System;

namespace SemanticKernelSummary.MVVM
{
    public interface IDynamicServiceProvider
    {

        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <returns>An instance of the requested service.</returns>
        T GetService<T>() where T : class;
        /// <summary>
        /// Gets a service of the specified type with a specific key.
        /// </summary>
        /// <typeparam name="T">The type of service to get.</typeparam>
        /// <param name="key">The key associated with the service.</param>
        /// <returns>An instance of the requested service.</returns>
        T GetService<T>(string key) where T : class;


        void AddSingleton<T>(T instance) where T : class;
        void AddSingleton<T>(string key, T instance) where T : class;

        void AddTransient<T>(Func<T> factory) where T : class;
        void AddTransient<T>(string key, Func<T> factory) where T : class;

	}
}
