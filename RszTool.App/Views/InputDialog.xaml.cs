using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RszTool.App.Views
{
    /// <summary>
    /// InputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog : Window
    {
        public string Message { get; set; } = "";
        public string InputText { get; set; } = "";

        public InputDialog()
        {
            InitializeComponent();
        }

        private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && e.OriginalSource is TextBox textBox)
            {
                textBox.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                DialogResult = true;
            }
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
