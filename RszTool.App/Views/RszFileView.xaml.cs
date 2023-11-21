using RszTool.App.ViewModels;
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
            rszPageViewModel = new();
            DataContext = rszPageViewModel;
        }

        private readonly RszPageViewModel rszPageViewModel;

        private void Test(object sender, System.Windows.RoutedEventArgs e)
        {
            TestContent.Text = rszPageViewModel.InstanceList[0].Stringify();
        }
    }
}
