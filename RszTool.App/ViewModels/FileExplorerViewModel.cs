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
            children ??= new();
            children.Clear();
            foreach (var item in Directory.GetFiles(Path))
            {
                if (Directory.Exists(item))
                {
                    children.Add(new DirectoryItem(item));
                }
                else
                {
                    children.Add(new FileItem(item));
                }
            }
        }
    }
}
