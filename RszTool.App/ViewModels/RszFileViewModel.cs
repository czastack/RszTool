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

        public static ScnFile.GameObjectData? CopiedGameObject { get; private set; }

        public override void PostRead()
        {
            File.SetupGameObjects();
        }

        public RelayCommand CopyGameObject => new(OnCopyGameObject);
        public RelayCommand DeleteGameObject => new(OnDeleteGameObject);
        public RelayCommand DuplicateGameObject => new(OnDuplicateGameObject);
        public RelayCommand PasetGameObject => new(OnPasetGameObject);
        public RelayCommand PasetGameObjectToFolder => new(OnPasetGameObjectToFolder);
        public RelayCommand PasetGameObjectToParent => new(OnPasetGameObjectToParent);

        /// <summary>
        /// 复制游戏对象
        /// </summary>
        /// <param name="arg"></param>
        public static void OnCopyGameObject(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            CopiedGameObject = gameObject;
        }

        /// <summary>
        /// 删除游戏对象
        /// </summary>
        /// <param name="arg"></param>
        public void OnDeleteGameObject(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            if (gameObject.Parent != null)
            {
                gameObject.Parent.Children.Remove(gameObject);
                gameObject.Parent = null;
            }
            else if (gameObject.Folder != null)
            {
                gameObject.Folder.GameObjects.Remove(gameObject);
                gameObject.Folder = null;
            }
            else
            {
                File.GameObjectDatas?.Remove(gameObject);
            }
            File.StructChanged = true;
        }

        /// <summary>
        /// 重复游戏对象
        /// </summary>
        /// <param name="arg"></param>
        public void OnDuplicateGameObject(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            File.DuplicateGameObject(gameObject);
        }

        /// <summary>
        /// 粘贴游戏对象
        /// </summary>
        /// <param name="arg"></param>
        public void OnPasetGameObject(object arg)
        {
            if (CopiedGameObject != null)
            {
                File.ImportGameObject(CopiedGameObject);
                OnPropertyChanged(nameof(GameObjects));
            }
        }

        /// <summary>
        /// 粘贴游戏对象到文件夹
        /// </summary>
        /// <param name="arg"></param>
        public void OnPasetGameObjectToFolder(object arg)
        {
            if (CopiedGameObject != null)
            {
                var folder = (ScnFile.FolderData)arg;
                File.ImportGameObject(CopiedGameObject, folder);
            }
        }

        /// <summary>
        /// 粘贴游戏对象到父对象
        /// </summary>
        /// <param name="arg"></param>
        public void OnPasetGameObjectToParent(object arg)
        {
            if (CopiedGameObject != null)
            {
                var parent = (ScnFile.GameObjectData)arg;
                File.ImportGameObject(CopiedGameObject, parent: parent);
            }
        }
    }


    public class RszViewModel(RSZFile rsz)
    {
        public List<RszInstance> Instances => rsz.InstanceList;

        public IEnumerable<RszInstance> Objects => rsz.ObjectInstances();
    }
}
