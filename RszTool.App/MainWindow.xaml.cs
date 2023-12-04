using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RszTool.App.ViewModels;

namespace RszTool.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Version version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version!;
            Title = $"{Title} v{version.Major}.{version.Minor} - By chenstack";

            Closing += OnClosing;
        }

        public void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.All;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = false;
        }

        public void OnDrop(object sender, DragEventArgs e)
        {
            if (DataContext is not MainWindowModel mainWindowModel) return;
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                mainWindowModel.OnDropFile(files);
            }
        }

        private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (DataContext is MainWindowModel mainWindowModel)
            {
                if (!mainWindowModel.OnExit()) e.Cancel = true;
            }
        }
    }
}
