using System.Windows;
using System.Windows.Controls;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// RszScnFileView.xaml 的交互逻辑
    /// </summary>
    public partial class RszScnFileView : UserControl
    {
        public RszScnFileView()
        {
            InitializeComponent();
        }

        private void OnValueChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ScnFileViewModel viewModel)
            {
                viewModel.Changed = true;
            }
        }

        private void OnResourceChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ScnFileViewModel viewModel)
            {
                viewModel.ResourceChanged = true;
                Console.WriteLine("OnResourceChanged");
            }
        }
    }
}
