using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using RszTool.App.Common;

namespace RszTool.App.ViewModels
{
    public abstract class BaseRszFileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public abstract BaseRszFile File { get; }
        public string? FilePath => File.FileHandler.FilePath;
        public string? FileName
        {
            get
            {
                string? path = FilePath;
                return path != null ? Path.GetFileName(path) : null;
            }
        }

        public bool Save() => File.Save();

        public bool SaveAs(string path)
        {
            bool result = File.SaveAs(path);
            if (result)
            {
                PropertyChanged?.Invoke(this, new(nameof(FilePath)));
            }
            return result;
        }

        public virtual void PostRead() {}

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    public class UserFileViewModel(UserFile file) : BaseRszFileViewModel
    {
        public override UserFile File { get; } = file;

        public RszViewModel RszViewModel => new(File.RSZ!);
    }


    public class PfbFileViewModel(PfbFile file) : BaseRszFileViewModel
    {
        public override PfbFile File { get; } = file;
        public RszViewModel RszViewModel => new(File.RSZ!);
        public IEnumerable<PfbFile.GameObjectData>? GameObjects => File.GameObjectDatas;

        public override void PostRead()
        {
            File.SetupGameObjects();
        }
    }


    public class ScnFileViewModel(ScnFile file) : BaseRszFileViewModel
    {
        public override ScnFile File { get; } = file;
        public RszViewModel RszViewModel => new(File.RSZ!);
        public ObservableCollection<ScnFile.FolderData>? Folders => File.FolderDatas;
        public ObservableCollection<ScnFile.GameObjectData>? GameObjects => File.GameObjectDatas;

        public static List<ScnFile.GameObjectData>? CopiedGameObjects { get; private set; }

        public override void PostRead()
        {
            File.SetupGameObjects();
        }

        public RelayCommand CopyGameObject => new(OnCopyGameObject);
        public RelayCommand DeleteGameObject => new(OnDeleteGameObject);
        public RelayCommand PasetGameObject => new(OnPasetGameObject);

        public void OnCopyGameObject(object arg)
        {
            CopiedGameObjects ??= new();
            var gameObject = (ScnFile.GameObjectData)arg;
            CopiedGameObjects.Remove(gameObject);
            CopiedGameObjects.Add(gameObject);
        }

        public void OnDeleteGameObject(object arg)
        {
            Console.WriteLine(arg);
        }

        public void OnPasetGameObject(object arg)
        {
            if (CopiedGameObjects != null && CopiedGameObjects.Count > 0)
            {
                var gameObject = CopiedGameObjects[^1];
                File.ImportGameObject(gameObject);
                OnPropertyChanged(nameof(GameObjects));
            }
        }
    }


    public class RszViewModel(RSZFile rsz)
    {
        public List<RszInstance> Instances => rsz.InstanceList;

        public IEnumerable<RszInstance> Objects => rsz.ObjectInstances();
    }
}
