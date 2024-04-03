using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// FileExplorerTree.xaml 的交互逻辑
    /// </summary>
    public partial class FileExplorerTree : UserControl
    {
        public FileExplorerTree()
        {
            InitializeComponent();
        }

        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {

        }
    }
}
