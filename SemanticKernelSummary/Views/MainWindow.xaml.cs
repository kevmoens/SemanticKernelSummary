using SemanticKernelSummary.ViewModels;
using System.Windows;

namespace SemanticKernelSummary.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }
        public MainWindow()
        {
            InitializeComponent();
        }
    }
}
