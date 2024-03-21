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
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is BaseRszFileViewModel viewModel)
            {
                viewModel.SelectedSearchResultChanged += OnSelectedSearchResultChanged;
            }
        }

        private void OnSelectedSearchResultChanged(object? obj)
        {
            if (obj != null)
            {
                var item = TreeViewUtils.GetTreeViewItem(TreeView, obj);
                if (item != null)
                {
                    item.IsSelected = true;
                }
            }
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
