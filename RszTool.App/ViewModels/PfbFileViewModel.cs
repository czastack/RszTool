using System.Collections.ObjectModel;
using System.Windows;
using RszTool.App.Common;
using RszTool.App.Resources;


namespace RszTool.App.ViewModels
{
    public class PfbFileViewModel(PfbFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => PfbFile;
        public PfbFile PfbFile { get; } = file;
        public RszViewModel RszViewModel => new(PfbFile.RSZ!);
        public IEnumerable<PfbFile.GameObjectData>? GameObjects => PfbFile.GameObjectDatas;
        public GameObjectSearchViewModel GameObjectSearchViewModel { get; } = new() { IncludeChildren = true };
        public ObservableCollection<PfbFile.GameObjectData>? SearchGameObjectList { get; set; }

        public bool ResourceChanged
        {
            get => PfbFile.ResourceChanged;
            set => PfbFile.ResourceChanged = value;
        }

        public RelayCommand CopyGameObject => new(OnCopyGameObject);
        public RelayCommand RemoveGameObject => new(OnRemoveGameObject);
        public RelayCommand DuplicateGameObject => new(OnDuplicateGameObject);
        public RelayCommand PasteGameObject => new(OnPasteGameObject);
        public RelayCommand PasteGameobjectAsChild => new(OnPasteGameobjectAsChild);
        public RelayCommand SearchGameObjects => new(OnSearchGameObjects);
        public RelayCommand AddComponent => new(OnAddComponent);
        public RelayCommand PasteInstanceAsComponent => new(OnPasteInstanceAsComponent);
        public RelayCommand RemoveComponent => new(OnRemoveComponent);

        public override void PostRead()
        {
            PfbFile.SetupGameObjects();
        }

        public override IEnumerable<object> TreeViewItems
        {
            get
            {
                yield return new TreeItemViewModel("Resources", PfbFile.ResourceInfoList);
                yield return new GameObjectsHeader("GameObjects", GameObjects);
            }
        }

        /// <summary>
        /// 复制游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private static void OnCopyGameObject(object arg)
        {
            GameObjectCopyHelper.CopyGameObject((PfbFile.GameObjectData)arg);
        }

        /// <summary>
        /// 删除游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnRemoveGameObject(object arg)
        {
            PfbFile.RemoveGameObject((PfbFile.GameObjectData)arg);
            Changed = true;
        }

        /// <summary>
        /// 重复游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnDuplicateGameObject(object arg)
        {
            PfbFile.DuplicateGameObject((PfbFile.GameObjectData)arg);
            Changed = true;
        }

        /// <summary>
        /// 粘贴游戏对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameObject(object arg)
        {
            var gameObject = GameObjectCopyHelper.GetCopiedPfbGameObject();
            if (gameObject != null)
            {
                PfbFile.ImportGameObject(gameObject);
                OnPropertyChanged(nameof(GameObjects));
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴游戏对象到父对象
        /// </summary>
        /// <param name="arg"></param>
        private void OnPasteGameobjectAsChild(object arg)
        {
            var gameObject = GameObjectCopyHelper.GetCopiedPfbGameObject();
            if (gameObject != null)
            {
                var parent = (PfbFile.GameObjectData)arg;
                PfbFile.ImportGameObject(gameObject, parent: parent);
                Changed = true;
            }
        }

        private void OnSearchGameObjects(object arg)
        {
            SearchGameObjectList ??= new();
            SearchGameObjectList.Clear();
            GameObjectFilter filter = new(GameObjectSearchViewModel);
            if (!filter.Enable) return;
            if (PfbFile.GameObjectDatas == null) return;
            foreach (var gameObject in PfbFile.IterAllGameObjects(GameObjectSearchViewModel.IncludeChildren))
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
            var gameObject = (PfbFile.GameObjectData)arg;
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
                PfbFile.AddComponent(gameObject, lastInputClassName);
                Changed = true;
            });
        }

        private void OnPasteInstanceAsComponent(object arg)
        {
            var gameObject = (PfbFile.GameObjectData)arg;
            if (CopiedInstance != null && File.GetRSZ() is RSZFile rsz)
            {
                RszInstance component = rsz.CloneInstance(CopiedInstance);
                PfbFile.AddComponent(gameObject, component);
                Changed = true;
            }
        }

        private void OnRemoveComponent(object arg)
        {
            var item = (GameObejctComponentViewModel)arg;
            var gameObject = item.GameObject;
            PfbFile.RemoveComponent(gameObject, item.Instance);
            Changed = true;
        }
    }
}
