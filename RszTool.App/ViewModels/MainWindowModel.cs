using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Dragablz;
using RszTool.App.Views;

namespace RszTool.App.ViewModels
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<HeaderedItemViewModel> Items { get; } = new();
        public GameName GameName { get; set; } = GameName.re4;

        /// <summary>
        /// 文件拖放
        /// </summary>
        /// <param name="path"></param>
        public void OnDropFile(string path)
        {
            BaseRszFileViewModel? fileViewModel = null;
            ContentControl? content = null;
            RszFileOption option = new(GameName);
            switch (RszUtils.GetFileType(path))
            {
                case FileType.user:
                    fileViewModel = new UserFileViewModel(new(option, new(path)));
                    content = new RszUserFileView();
                    break;
                case FileType.pfb:
                    fileViewModel = new PfbFileViewModel(new(option, new(path)));
                    content = new RszPfbFileView();
                    break;
                case FileType.scn:
                    fileViewModel = new ScnFileViewModel(new(option, new(path)));
                    content = new RszScnFileView();
                    break;
            }
            if (fileViewModel != null && content != null)
            {
                if (!fileViewModel.File.Read())
                {
                    return;
                }
                fileViewModel.PostRead();
                content.DataContext = fileViewModel;
                HeaderedItemViewModel header = new(
                    fileViewModel.FileName!, content);
                Items.Add(header);
            }
            else
            {
                MessageBox.Show("不支持的文件类型", "提示");
            }
        }

        public void OnDropFile(string[] files)
        {
            foreach (var file in files)
            {
                try
                {
                    OnDropFile(file);
                }
                catch (Exception e)
                {
                    App.ShowUnhandledException(e, "OnDropFile");
                }
            }
        }

        public ItemActionCallback ClosingTabItemHandler
        {
            get { return ClosingTabItemHandlerImpl; }
        }

        /// <summary>
        /// Callback to handle tab closing.
        /// </summary>
        private static void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {
            //in here you can dispose stuff or cancel the close

            //here's your view model:
            var viewModel = args.DragablzItem.DataContext as HeaderedItemViewModel;

            //here's how you can cancel stuff:
            //args.Cancel(); 
        }
    }
}
