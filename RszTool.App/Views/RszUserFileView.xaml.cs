using System.Windows;
using System.Windows.Controls;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// RszUserFileView.xaml 的交互逻辑
    /// </summary>
    public partial class RszUserFileView : UserControl
    {
        public RszUserFileView()
        {
            InitializeComponent();
        }

        private void OnValueChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserFileViewModel viewModel)
            {
                viewModel.Changed = true;
            }
        }

        private void OnResourceChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is UserFileViewModel viewModel)
            {
                viewModel.ResourceChanged = true;
            }
        }
    }
}
