using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// SearchInstanceView.xaml 的交互逻辑
    /// </summary>
    public partial class SearchInstanceView : UserControl
    {
        public SearchInstanceView()
        {
            InitializeComponent();
        }

        private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.OriginalSource is TextBox textBox)
            {
                textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                SearchButton.Command.Execute(null);
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
