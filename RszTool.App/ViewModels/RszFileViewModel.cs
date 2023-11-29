using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
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
        public bool Changed { get; set; }
        public RelayCommand CopyInstance => new(OnCopyInstance);
        public RelayCommand CopyArrayItem => new(OnCopyArrayItem);
        public RelayCommand RemoveArrayItem => new(OnRemoveArrayItem);
        public RelayCommand DuplicateArrayItem => new(OnDuplicateArrayItem);
        public RelayCommand DuplicateArrayItemMulti => new(OnDuplicateArrayItemMulti);
        public RelayCommand PasteArrayItemAfter => new(OnPasteArrayItemAfter);

        public static RszInstance? CopiedInstance { get; private set; }

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

        private static void OnCopyInstance(object arg)
        {
            if (arg is RszInstance instance)
            {
                CopiedInstance = instance;
            }
        }

        private static void OnCopyArrayItem(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                CopiedInstance = item.Instance;
            }
        }

        private void OnRemoveArrayItem(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                rsz.ArrayRemoveItem(item.Values, item.Instance);
                item.Array.NotifyValueChanged();
            }
        }

        private void OnDuplicateArrayItem(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                rsz.ArrayInsertItem(item.Values, item.Instance, isDuplicate: true);
                item.Array.NotifyValueChanged();
            }
        }

        private void OnDuplicateArrayItemMulti(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                Views.InputDialog dialog = new()
                {
                    Message = "请输入重复次数",
                    Owner = Application.Current.MainWindow,
                };
                // 显示对话框，并等待用户输入
                if (dialog.ShowDialog() == true)
                {
                    string userInput = dialog.InputText;
                    int count = int.Parse(userInput);
                    for (int i = 0; i < count; i++)
                    {
                        rsz.ArrayInsertItem(item.Values, item.Instance, isDuplicate: true);
                    }
                }
                item.Array.NotifyValueChanged();
            }
        }

        /// <summary>
        /// 在后面粘贴
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteArrayItemAfter(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item && File.GetRSZ() is RSZFile rsz &&
                CopiedInstance != null)
            {
                if (CopiedInstance.RszClass != item.Instance.RszClass)
                {
                    var error = new InvalidOperationException($"CopiedInstance is {CopiedInstance.RszClass.name}, missmatch {item.Instance.RszClass.name}");
                    App.ShowUnhandledException(error, "OnPasteArrayItemAfter");
                }
                rsz.ArrayInsertItem(item.Values, CopiedInstance, item.Values.IndexOf(item.Instance) + 1);
                item.Array.NotifyValueChanged();
            }
        }
    }


    public class UserFileViewModel(UserFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => UserFile;
        public UserFile UserFile { get; } = file;

        public RszViewModel RszViewModel => new(UserFile.RSZ!);
    }


    public class PfbFileViewModel(PfbFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => PfbFile;
        public PfbFile PfbFile { get; } = file;
        public RszViewModel RszViewModel => new(PfbFile.RSZ!);
        public IEnumerable<PfbFile.GameObjectData>? GameObjects => PfbFile.GameObjectDatas;

        public override void PostRead()
        {
            PfbFile.SetupGameObjects();
        }
    }


    public class ScnFileViewModel(ScnFile file) : BaseRszFileViewModel
    {
        public ScnFile ScnFile { get; } = file;
        public override BaseRszFile File => ScnFile;
        public RszViewModel RszViewModel => new(ScnFile.RSZ!);
        public ObservableCollection<ScnFile.FolderData>? Folders => ScnFile.FolderDatas;
        public ObservableCollection<ScnFile.GameObjectData>? GameObjects => ScnFile.GameObjectDatas;

        public static ScnFile.GameObjectData? CopiedGameObject { get; private set; }

        public override void PostRead()
        {
            ScnFile.SetupGameObjects();
        }

        public RelayCommand CopyGameObject => new(OnCopyGameObject);
        public RelayCommand RemoveGameObject => new(OnRemoveGameObject);
        public RelayCommand DuplicateGameObject => new(OnDuplicateGameObject);
        public RelayCommand PasetGameObject => new(OnPasetGameObject);
        public RelayCommand PasetGameObjectToFolder => new(OnPasetGameObjectToFolder);
        public RelayCommand PasetGameObjectToParent => new(OnPasetGameObjectToParent);

        /// <summary>
        /// 复制游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private static void OnCopyGameObject(object arg)
        {
            CopiedGameObject = (ScnFile.GameObjectData)arg;
        }

        /// <summary>
        /// 删除游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnRemoveGameObject(object arg)
        {
            ScnFile.RemoveGameObject((ScnFile.GameObjectData)arg);
        }

        /// <summary>
        /// 重复游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnDuplicateGameObject(object arg)
        {
            ScnFile.DuplicateGameObject((ScnFile.GameObjectData)arg);
        }

        /// <summary>
        /// 粘贴游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasetGameObject(object arg)
        {
            if (CopiedGameObject != null)
            {
                ScnFile.ImportGameObject(CopiedGameObject);
                OnPropertyChanged(nameof(GameObjects));
            }
        }

        /// <summary>
        /// 粘贴游戏对象到文件夹
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasetGameObjectToFolder(object arg)
        {
            if (CopiedGameObject != null)
            {
                var folder = (ScnFile.FolderData)arg;
                ScnFile.ImportGameObject(CopiedGameObject, folder);
            }
        }

        /// <summary>
        /// 粘贴游戏对象到父对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasetGameObjectToParent(object arg)
        {
            if (CopiedGameObject != null)
            {
                var parent = (ScnFile.GameObjectData)arg;
                ScnFile.ImportGameObject(CopiedGameObject, parent: parent);
            }
        }
    }


    public class RszViewModel(RSZFile rsz)
    {
        public List<RszInstance> Instances => rsz.InstanceList;

        public IEnumerable<RszInstance> Objects => rsz.ObjectList;
    }
}
