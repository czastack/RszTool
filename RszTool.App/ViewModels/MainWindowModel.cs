using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
                content.DataContext = fileViewModel;
                HeaderedItemViewModel header = new(
                    fileViewModel.FileName!, content, true);
                Items.Add(header);
            }
        }

        private void DragOver(object sender, DragEventArgs e)
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

        private void Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (var file in files)
                {
                    OnDropFile(file);
                }
            }
        }
    }

    /// <summary>
    /// Helper class to create view models, particularly for tool/MDI windows.
    /// </summary>
    public class HeaderedItemViewModel(object header, object content, bool isSelected = false) : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public object Header { get; set; } = header;
        public object Content { get; set; } = content;
        public bool IsSelected { get; set; } = isSelected;
    }
}
