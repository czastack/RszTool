using System.Windows;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// UpdateContextIDWindow.xaml 的交互逻辑
    /// </summary>
    public partial class UpdateContextIDWindow : Window
    {
        public UpdateContextIDWindow()
        {
            InitializeComponent();
        }

        public IEnumerable<GameObjectContextID> TreeViewItems { get; set; } = Array.Empty<GameObjectContextID>();
    }
}
