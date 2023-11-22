using RszTool.App.ViewModels;
using System.Windows.Controls;

namespace RszTool.App.Views
{
    /// <summary>
    /// RszTestView.xaml 的交互逻辑
    /// </summary>
    public partial class RszTestView : UserControl
    {
        public RszTestView()
        {
            InitializeComponent();
            rszTestViewModel = new();
            DataContext = rszTestViewModel;
        }

        private readonly RszTestViewModel rszTestViewModel;

        private void Test(object sender, System.Windows.RoutedEventArgs e)
        {
            TestContent.Text = rszTestViewModel.InstanceList[0].Stringify();
        }
    }
}
