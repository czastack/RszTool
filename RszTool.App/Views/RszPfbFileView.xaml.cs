using System.Windows;
using System.Windows.Controls;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// RszPfbileView.xaml 的交互逻辑
    /// </summary>
    public partial class RszPfbFileView : UserControl
    {
        public RszPfbFileView()
        {
            InitializeComponent();
        }

        private void OnValueChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is PfbFileViewModel viewModel)
            {
                viewModel.Changed = true;
            }
        }

        private void OnResourceChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is PfbFileViewModel viewModel)
            {
                viewModel.ResourceChanged = true;
            }
        }
    }
}
