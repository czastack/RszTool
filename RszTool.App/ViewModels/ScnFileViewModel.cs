using System.Collections.ObjectModel;
using System.Windows;
using RszTool.App.Common;
using RszTool.App.Resources;

namespace RszTool.App.ViewModels
{
    public class ScnFileViewModel(ScnFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => ScnFile;
        public ScnFile ScnFile { get; } = file;
        public RszViewModel RszViewModel => new(ScnFile.RSZ!);
        public ObservableCollection<ScnFile.FolderData>? Folders => ScnFile.FolderDatas;
        public ObservableCollection<ScnFile.GameObjectData>? GameObjects => ScnFile.GameObjectDatas;
        public GameObjectSearchViewModel GameObjectSearchViewModel { get; } = new();
        public ObservableCollection<ScnFile.GameObjectData>? SearchGameObjectList { get; set; }

        public bool ResourceChanged
        {
            get => ScnFile.ResourceChanged;
            set => ScnFile.ResourceChanged = value;
        }

        public override void PostRead()
        {
            ScnFile.SetupGameObjects();
        }

        public override IEnumerable<object> TreeViewItems
        {
            get
            {
                yield return new TreeItemViewModel("Resources", ScnFile.ResourceInfoList);
                yield return new FoldersHeader("Folders", Folders);
                yield return new GameObjectsHeader("GameObjects", GameObjects);
            }
        }

        public RelayCommand CopyGameObject => new(OnCopyGameObject);
        public RelayCommand RemoveFolder => new(OnRemoveFolder);
        public RelayCommand RemoveGameObject => new(OnRemoveGameObject);
        public RelayCommand DuplicateGameObject => new(OnDuplicateGameObject);
        public RelayCommand PasteGameObject => new(OnPasteGameObject);
        public RelayCommand PasteGameObjectToFolder => new(OnPasteGameObjectToFolder);
        public RelayCommand AddFolder => new(OnAddFolder);
        public RelayCommand PasteGameobjectAsChild => new(OnPasteGameobjectAsChild);
        public RelayCommand SearchGameObjects => new(OnSearchGameObjects);
        public RelayCommand AddComponent => new(OnAddComponent);
        public RelayCommand PasteInstanceAsComponent => new(OnPasteInstanceAsComponent);
        public RelayCommand RemoveComponent => new(OnRemoveComponent);

        /// <summary>
        /// Update re4 chainsaw.ContextID
        /// </summary>
        /// <param name="gameObject"></param>
        private void UpdateContextID(IGameObjectData gameObject)
        {
            GameObjectCopyHelper.UpdateContextID(ScnFile.Option, gameObject);
        }

        /// <summary>
        /// 复制游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private static void OnCopyGameObject(object arg)
        {
            GameObjectCopyHelper.CopyGameObject((ScnFile.GameObjectData)arg);
        }

        /// <summary>
        /// 删除目录
        /// </summary>
        /// <param name="arg"></param>
        private void OnRemoveFolder(object arg)
        {
            ScnFile.RemoveFolder((ScnFile.FolderData)arg);
            Changed = true;
        }

        /// <summary>
        /// 删除游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnRemoveGameObject(object arg)
        {
            ScnFile.RemoveGameObject((ScnFile.GameObjectData)arg);
            Changed = true;
        }

        /// <summary>
        /// 重复游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnDuplicateGameObject(object arg)
        {
            var newGameObject = ScnFile.DuplicateGameObject((ScnFile.GameObjectData)arg);
            UpdateContextID(newGameObject);
            Changed = true;
        }

        /// <summary>
        /// 粘贴游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameObject(object arg)
        {
            var gameObject = GameObjectCopyHelper.GetCopiedScnGameObject();
            if (gameObject != null)
            {
                var newGameObject = ScnFile.ImportGameObject(gameObject);
                UpdateContextID(newGameObject);
                OnPropertyChanged(nameof(GameObjects));
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴游戏对象到文件夹
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameObjectToFolder(object arg)
        {
            var gameObject = GameObjectCopyHelper.GetCopiedScnGameObject();
            if (gameObject != null)
            {
                var folder = (ScnFile.FolderData)arg;
                var newGameObject = ScnFile.ImportGameObject(gameObject, folder);
                UpdateContextID(newGameObject);
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴游戏对象到父对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameobjectAsChild(object arg)
        {
            var gameObject = GameObjectCopyHelper.GetCopiedScnGameObject();
            if (gameObject != null)
            {
                var parent = (ScnFile.GameObjectData)arg;
                var newGameObject = ScnFile.ImportGameObject(gameObject, parent: parent);
                UpdateContextID(newGameObject);
                Changed = true;
            }
        }

        /// <summary>
        /// 添加文件夹
        /// </summary>
        /// <param name="arg"></param>
        private void OnAddFolder(object arg)
        {
            Views.InputDialog dialog = new()
            {
                Title = Texts.AddFolder,
                Message = Texts.InputFolderName,
                Owner = Application.Current.MainWindow,
            };
            if (dialog.ShowDialog() != true) return;
            if (string.IsNullOrWhiteSpace(dialog.InputText))
            {
                MessageBoxUtils.Error("FolderName is empty");
                return;
            }
            lastInputClassName = dialog.InputText;
            ScnFile.AddFolder(dialog.InputText, arg as ScnFile.FolderData);
            Changed = true;
        }

        private void OnSearchGameObjects(object arg)
        {
            SearchGameObjectList ??= new();
            SearchGameObjectList.Clear();
            GameObjectFilter filter = new(GameObjectSearchViewModel);
            if (!filter.Enable) return;
            if (ScnFile.GameObjectDatas == null) return;
            foreach (var gameObject in ScnFile.IterAllGameObjects(GameObjectSearchViewModel.IncludeChildren))
            {
                if (filter.IsMatch(gameObject))
                {
                    SearchGameObjectList.Add(gameObject);
                }
            }
        }

        private string lastInputClassName = "";
        private void OnAddComponent(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            Views.InputDialog dialog = new()
            {
                Title = Texts.NewItem,
                Message = Texts.InputClassName,
                InputText = lastInputClassName,
                Owner = Application.Current.MainWindow,
            };
            if (dialog.ShowDialog() != true) return;
            lastInputClassName = dialog.InputText;
            if (string.IsNullOrWhiteSpace(lastInputClassName))
            {
                MessageBoxUtils.Error("ClassName is empty");
                return;
            }
            AppUtils.TryActionSimple(() =>
            {
                ScnFile.AddComponent(gameObject, lastInputClassName);
                Changed = true;
            });
        }

        private void OnPasteInstanceAsComponent(object arg)
        {
            var gameObject = (ScnFile.GameObjectData)arg;
            if (CopiedInstance != null && File.GetRSZ() is RSZFile rsz)
            {
                RszInstance component = rsz.CloneInstance(CopiedInstance);
                ScnFile.AddComponent(gameObject, component);
                Changed = true;
            }
        }

        private void OnRemoveComponent(object arg)
        {
            var item = (GameObejctComponentViewModel)arg;
            if (ScnFile.RemoveComponent(item.GameObject, item.Instance))
            {
                Changed = true;
            }
        }
    }


    public class RszViewModel(RSZFile rsz)
    {
        public List<RszInstance> Instances => rsz.InstanceList;

        public List<RszInstance> Objects => rsz.ObjectList;
    }
}
