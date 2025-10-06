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
            Title = $"{Title} v{version.Major}.{version.Minor}.{version.Build} - By chenstack";

            Closing += OnClosing;
        }

        ///隐藏/显示侧边栏
        private void SideColumn_Hide(object sender, RoutedEventArgs e)
        {
            if (col1.Width != new GridLength(0))
            {
                col1.Width = new GridLength(0);
            }
            else { 
                col1.Width =new GridLength(200);
            }
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

        private void Tag_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowModel mainWindowModel &&
                mainWindowModel.SelectedTabItem != null)
            {
                IInputElement focusedControl = FocusManager.GetFocusedElement(this);
                if (focusedControl is TreeViewItem treeViewItem &&
                    e.Source is MenuItem menuItem && menuItem.Header is Ellipse ellipse)
                {
                    treeViewItem.Foreground = ellipse.Fill;
                }
            }
        }
    }
}
