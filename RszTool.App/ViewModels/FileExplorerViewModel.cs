using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using RszTool.App.Common;

namespace RszTool.App.ViewModels
{
    public class FileExplorerViewModel
    {
        public ObservableCollection<RootDirectoryItem> Folders { get; } = new();
        public event Action<FileItem>? OnFileSelected;

        public RelayCommand RemoveRootDirectory => new(OnRemoveRootDirectory);

        public void Refresh()
        {
            foreach (var folder in Folders)
            {
                folder.Refresh();
            }
        }

        public void AddFolder(string path)
        {
            if (!Folders.Any(item => item.Name == path))
            {
                Folders.Add(new(path));
                App.Instance.SaveData.OpenedFolders.Add(path);
            }
        }

        public void SelectFile(FileItem fileItem)
        {
            OnFileSelected?.Invoke(fileItem);
        }

        private void OnRemoveRootDirectory(object arg)
        {
            if (arg is RootDirectoryItem rootDirectory)
            {
                Folders.Remove(rootDirectory);
                App.Instance.SaveData.OpenedFolders.Remove(rootDirectory.Path);
            }
        }
    }


    public class BaseFileItem : INotifyPropertyChanged
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public bool IsExpanded { get; set; }

        protected BaseFileItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    public class FileItem(string path) : BaseFileItem(path)
    {
    }


    public class DirectoryItem(string path) : BaseFileItem(path)
    {
        private static readonly IComparer<BaseFileItem> comparer =
            Comparer<BaseFileItem>.Create((x, y) => x.Name.CompareTo(y.Name));
        private static readonly HashSet<string> Filter = new() { ".pfb", ".scn", ".user" };

        private ObservableCollection<BaseFileItem>? children;

        public ObservableCollection<BaseFileItem> Children
        {
            get
            {
                if (children == null)
                {
                    children = new();
                    Refresh();
                }
                return children;
            }
        }

        public void Refresh()
        {
            if (children == null) return;
            // Console.WriteLine($"Refresh {Path}");
            List<BaseFileItem> items = new();
            Dictionary<string, BaseFileItem> childDict = new();
            foreach (BaseFileItem item in children)
            {
                childDict[item.Path] = item;
            }
            children.Clear();
            int directoryCount = 0;
            foreach (var childPath in Directory.EnumerateDirectories(Path))
            {
                if (childDict.TryGetValue(childPath, out var fileItem) && fileItem is DirectoryItem directoryItem)
                {
                    items.Add(fileItem);
                    directoryItem.Refresh();
                }
                else
                {
                    items.Add(new DirectoryItem(childPath));
                }
                directoryCount++;
            }
            foreach (var childPath in Directory.EnumerateFiles(Path))
            {
                RszUtils.GetFileExtension(childPath, out string extension, out _);
                if (!Filter.Contains(extension)) continue;
                if (childDict.TryGetValue(childPath, out var fileItem) && fileItem is FileItem)
                {
                    items.Add(fileItem);
                }
                else
                {
                    items.Add(new FileItem(childPath));
                }
            }
            items.Sort(0, directoryCount, comparer);
            items.Sort(directoryCount, items.Count - directoryCount, comparer);
            if (directoryCount == 1)
            {
                items[0].IsExpanded = true;
                ((DirectoryItem)items[0]).Refresh();
            }
            foreach (var item in items)
            {
                if (item is DirectoryItem directoryItem)
                {
                    directoryItem.Refresh();
                }
                Children.Add(item);
            }
        }
    }


    public class RootDirectoryItem(string path) : DirectoryItem(path)
    {
    }
}
