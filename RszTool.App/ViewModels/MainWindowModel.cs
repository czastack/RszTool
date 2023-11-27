using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Dragablz;
using Microsoft.Win32;
using RszTool.App.Common;
using RszTool.App.Views;

namespace RszTool.App.ViewModels
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<HeaderedItemViewModel> Items { get; } = new();
        public GameName GameName { get; set; } = GameName.re4;
        public HeaderedItemViewModel? SelectedTabItem { get; set; }

        private BaseRszFileViewModel? CurrentFile =>
            SelectedTabItem is FileTabItemViewModel fileTabItemViewModel ?
            fileTabItemViewModel.FileViewModel : null;

        public CustomInterTabClient InterTabClient { get; } = new();

        /// <summary>
        /// 打开文件
        /// </summary>
        /// <param name="path"></param>
        public void OpenFile(string path)
        {
            // check file opened
            foreach (var item in Items)
            {
                if (item is FileTabItemViewModel fileTab)
                {
                    if (fileTab.FileViewModel.FilePath == path)
                    {
                        SelectedTabItem = item;
                        return;
                    }
                }
            }

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
                HeaderedItemViewModel header = new FileTabItemViewModel(fileViewModel, content);
                Items.Add(header);
                SelectedTabItem = header;
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
                if (Debugger.IsAttached)
                {
                    OpenFile(file);
                }
                else try
                {
                    OpenFile(file);
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



        public RelayCommand OpenCommand => new(OnOpen);
        public RelayCommand SaveCommand => new(OnSave);
        public RelayCommand CloseCommand => new(OnClose);
        public RelayCommand QuitCommand => new(OnQuit);

        private static readonly (string, string)[] SupportedFile = {
            ("User file", "*.user.*"),
            ("Scene file", "*.scn.*"),
            ("Prefab file", "*.pfb.*"),
        };

        private static readonly string OpenFileFilter =
            $"All file|{string.Join(";", SupportedFile.Select(item => item.Item2))}|" +
            string.Join("|", SupportedFile.Select(item => $"{item.Item1}|{item.Item2}"));

        private void OnOpen(object arg)
        {
            // Configure open file dialog box
            var dialog = new OpenFileDialog
            {
                FileName = "Document", // Default file name
                DefaultExt = ".txt", // Default file extension
                // Filter files by extension
                Filter = OpenFileFilter
            };

            // Show open file dialog box
            bool? result = dialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dialog.FileName;
                OpenFile(filename);
            }
        }

        private void OnSave(object arg)
        {
            if (Debugger.IsAttached)
            {
                CurrentFile?.Save();
            }
            else try
            {
                CurrentFile?.Save();
            }
            catch (Exception e)
            {
                App.ShowUnhandledException(e, "OnSave");
            }
        }

        private void OnClose(object arg)
        {
            if (SelectedTabItem == null || CurrentFile == null) return;
            if (CurrentFile.Changed)
            {
                // Check changed
            }
            Items.Remove(SelectedTabItem);
        }

        private void OnQuit(object arg)
        {
            foreach (var item in Items)
            {
                if (item is FileTabItemViewModel fileTab)
                {
                    if (fileTab.FileViewModel.Changed)
                    {
                        // Check changed
                    }
                }
            }
            Application.Current.Shutdown();
        }
    }


    public class FileTabItemViewModel : HeaderedItemViewModel
    {
        public FileTabItemViewModel(BaseRszFileViewModel fileViewModel, object content, bool isSelected = false)
            : base(fileViewModel.FileName!, content, isSelected)
        {
            FileViewModel = fileViewModel;
        }

        public BaseRszFileViewModel FileViewModel { get; set; }
    }
}
