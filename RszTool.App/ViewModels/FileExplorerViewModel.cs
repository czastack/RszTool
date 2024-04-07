using System.Collections.ObjectModel;
using System.IO;

namespace RszTool.App.ViewModels
{
    public class FileExplorerViewModel
    {
        public ObservableCollection<DirectoryItem> Folders { get; } = new();

        public void Refresh()
        {
            foreach (var folder in Folders)
            {
                folder.Refresh();
            }
        }
    }


    public class BaseFileItem
    {
        public string Path { get; set; }
        public string Name { get; set; }

        protected BaseFileItem(string path)
        {
            Path = path;
            Name = System.IO.Path.GetFileName(path);
        }
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
            Console.WriteLine($"Refresh {Path}");
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
}
