using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using Dragablz;
using Microsoft.Win32;
using RszTool.App.Common;
using RszTool.App.Resources;
using RszTool.App.Views;
using RszTool.Common;

namespace RszTool.App.ViewModels
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<HeaderedItemViewModel> Items { get; } = new();
        public HeaderedItemViewModel? SelectedTabItem { get; set; }
        public FileExplorerViewModel FileExplorerViewModel { get; } = new();

        private BaseRszFileViewModel? CurrentFile =>
            SelectedTabItem is FileTabItemViewModel fileTabItemViewModel ?
            fileTabItemViewModel.FileViewModel : null;

        public bool IsDarkTheme
        {
            get => ThemeManager.Instance.IsDarkTheme;
            set
            {
                SaveData.IsDarkTheme = value;
                ThemeManager.Instance.IsDarkTheme = value;
            }
        }

        public CustomInterTabClient InterTabClient { get; } = new();
        public static SaveData SaveData => App.Instance.SaveData;

        public RelayCommand OpenCommand => new(OnOpen);
        public RelayCommand SaveCommand => new(OnSave);
        public RelayCommand SaveAsCommand => new(OnSaveAs);
        public RelayCommand ReopenCommand => new(OnReopen);
        public RelayCommand AddFolderCommand => new(OnAddFolder);
        public RelayCommand CloseCommand => new(OnClose);
        public RelayCommand QuitCommand => new(OnQuit);
        public RelayCommand ClearRecentFilesHistory => new(OnClearRecentFilesHistory);
        public RelayCommand OpenRecentFile => new(OnOpenRecentFile);
        public RelayCommand OpenAbout => new(OnOpenAbout);

        public ItemActionCallback ClosingTabItemHandler => ClosingTabItemHandlerImpl;

        public MainWindowModel()
        {
            foreach (var path in SaveData.OpenedFolders)
            {
                FileExplorerViewModel.Folders.Add(new(path));
            }
            if (FileExplorerViewModel.Folders.Count == 0)
            {
                FileExplorerViewModel.Folders.Add(new(Directory.GetCurrentDirectory()));
            }
            FileExplorerViewModel.OnFileSelected += f => OpenFile(f.Path);
        }

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

            string rszJsonFile = $"rsz{SaveData.GameName}.json";
            if (!File.Exists(rszJsonFile))
            {
                MessageBoxUtils.Warning(string.Format(Texts.RszJsonNotFound, rszJsonFile));
                return;
            }

            BaseRszFileViewModel? fileViewModel = null;
            ContentControl? content = null;
            RszFileOption option = new(SaveData.GameName);
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
                bool readSuccess = false;
                for (int tryCount = 0; tryCount < 5; tryCount++)
                {
                    try
                    {
                        readSuccess = fileViewModel.Read();
                        break;
                    }
                    catch (RszRetryOpenException e)
                    {
                        fileViewModel.File.FileHandler.Seek(0);
                        MessageBoxUtils.Info(string.Format(Texts.TryReopen, e.Message));
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }
                if (!readSuccess)
                {
                    MessageBoxUtils.Error(Texts.ReadFailed);
                    return;
                }
                content.DataContext = fileViewModel;
                HeaderedItemViewModel header = new FileTabItemViewModel(fileViewModel, content);
                Items.Add(header);
                SelectedTabItem = header;

                SaveData.AddRecentFile(path);
            }
            else
            {
                MessageBoxUtils.Info(Texts.NotSupportedFormat);
            }
        }

        public void TryOpenFile(string path)
        {
            AppUtils.TryAction(() => OpenFile(path));
        }

        public void OnDropFile(string[] files)
        {
            for (int i = 0; i < files.Length; i++)
            {
                string file = files[i];
                TryOpenFile(file);
            }
        }

        /// <summary>
        /// Callback to handle tab closing.
        /// </summary>
        private void ClosingTabItemHandlerImpl(ItemActionCallbackArgs<TabablzControl> args)
        {
            if (args.DragablzItem.DataContext is not FileTabItemViewModel fileTab) return;
            if (!OnTabClose(fileTab))
            {
                args.Cancel();
            }
        }

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
            var dialog = new OpenFileDialog
            {
                Filter = OpenFileFilter
            };
            if (dialog.ShowDialog() == true)
            {
                TryOpenFile(dialog.FileName);
            }
        }

        private void OnSave(object arg)
        {
            AppUtils.TryAction(() => CurrentFile?.Save());
        }

        private void OnSaveAs(object arg)
        {
            var currentFile = CurrentFile;
            if (currentFile == null) return;
            var dialog = new SaveFileDialog
            {
                FileName = currentFile.FileName,
                Filter = OpenFileFilter
            };
            if (dialog.ShowDialog() == true)
            {
                // Open document
                string? path = dialog.FileName;
                if (path != null)
                {
                    AppUtils.TryAction(() =>
                    {
                        if(currentFile.SaveAs(path))
                        {
                            SaveData.AddRecentFile(path);
                        }
                    });
                }
            }
        }

        private void OnReopen(object arg)
        {
            AppUtils.TryAction(() => CurrentFile?.Reopen());
        }

        private static bool OnTabClose(FileTabItemViewModel fileTab)
        {
            if (fileTab.FileViewModel.Changed)
            {
                // Check changed
                var result = MessageBoxUtils.YesNoCancel(
                    $"{Texts.FileChangedPrompt}\n{fileTab.FileViewModel.FilePath}");
                if (result == MessageBoxResult.Yes)
                {
                    AppUtils.TryAction(() => fileTab.FileViewModel.Save());
                }
                else if (result == MessageBoxResult.Cancel) return false;
            }
            return true;
        }

        private void OnClose(object arg)
        {
            if (SelectedTabItem is FileTabItemViewModel fileTab && OnTabClose(fileTab))
            {
                Items.Remove(fileTab);
            }
        }

        private void OnQuit(object arg)
        {
            if (OnExit())
            {
                Application.Current.Shutdown();
            }
        }

        private void OnClearRecentFilesHistory(object arg)
        {
            SaveData.RecentFiles.Clear();
        }

        private void OnOpenRecentFile(object arg)
        {
            if (arg is string path)
            {
                TryOpenFile(path);
            }
        }

        private void OnAddFolder(object arg)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                FileExplorerViewModel.AddFolder(dialog.FolderName);
            }
        }

        public bool OnExit()
        {
            foreach (var item in Items)
            {
                if (item is FileTabItemViewModel fileTab)
                {
                    if (!OnTabClose(fileTab)) return false;
                }
            }
            return true;
        }

        private void OnOpenAbout(object arg)
        {
            var about = new AboutWindow();
            about.ShowDialog();
        }
    }


    public class FileTabItemViewModel : HeaderedItemViewModel
    {
        public FileTabItemViewModel(BaseRszFileViewModel fileViewModel, object content, bool isSelected = false)
            : base(fileViewModel.FileName!, content, isSelected)
        {
            FileViewModel = fileViewModel;
            fileViewModel.HeaderChanged += UpdateHeader;
        }

        public BaseRszFileViewModel FileViewModel { get; set; }

        public void UpdateHeader()
        {
            Header = FileViewModel.FileName + (FileViewModel.Changed ? "*" : "");
        }
    }
}
