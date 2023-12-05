using RszTool.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RszTool.App.Views
{
    /// <summary>
    /// RszFileView.xaml 的交互逻辑
    /// </summary>
    public partial class RszFileView : UserControl
    {
        public RszFileView()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DataContext is BaseRszFileViewModel viewModel)
            {
                viewModel.SelectedItem = e.NewValue;
            }
        }
    }
}
