using SemanticKernelSummary.ViewModels;
using SemanticKernelSummary.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using SemanticKernelSummary.MVVM;

namespace SemanticKernelSummary
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ConfigureServices();
            MainWindow = _serviceProvider!.GetService<MainWindow>();
            MainWindow!.Show();
        }
        public void ConfigureServices()
        {
            ServiceCollection services = new();
            services.AddSingleton<MainWindow>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton(typeof(IFactory<>), typeof(ServiceProviderFactory<>));
            services.AddSingleton<IDynamicServiceProvider, DynamicServiceProvider>();
			_serviceProvider = services.BuildServiceProvider();
        }
    }
}
