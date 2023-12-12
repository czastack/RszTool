using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using RszTool.App.Common;
using RszTool.App.Resources;


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
        private bool changed;
        public bool Changed
        {
            get => changed;
            set
            {
                changed = value;
                HeaderChanged?.Invoke();
            }
        }
        public object? SelectedItem { get; set; }
        public InstanceSearchViewModel InstanceSearchViewModel { get; } = new();
        public ObservableCollection<RszInstance>? SearchInstanceList { get; set; }

        public static RelayCommand CopyInstance => new(OnCopyInstance);
        public RelayCommand PasteInstance => new(OnPasteInstance);
        public static RelayCommand CopyNormalField => new(OnCopyNormalField);
        public RelayCommand PasteNormalField => new(OnPasteNormalField);
        public static RelayCommand ArrayItemCopy => new(OnArrayItemCopy);
        public RelayCommand ArrayItemRemove => new(OnArrayItemRemove);
        public RelayCommand ArrayItemDuplicate => new(OnArrayItemDuplicate);
        public RelayCommand ArrayItemDuplicateMulti => new(OnArrayItemDuplicateMulti);
        public RelayCommand ArrayItemPasteAfter => new(OnArrayItemPasteAfter);
        public RelayCommand ArrayItemPasteToSelf => new(OnArrayItemPasteToSelf);
        public RelayCommand ArrayItemNew => new(OnArrayItemNew);
        public RelayCommand ArrayItemPaste => new(OnArrayItemPaste);
        public RelayCommand SearchInstances => new(OnSearchInstances);

        /// <summary>
        /// 标题改变(SaveAs或者Changed)
        /// </summary>
        public event Action? HeaderChanged;

        /// <summary>
        /// Item改变了，但Item本身不支持Notify，Items用了Convert的情况，通知TreeView更新
        /// </summary>
        // public event Action<object>? TreeViewItemChanged;

        public static RszInstance? CopiedInstance { get; private set; }
        public static RszField? CopiedNormalField { get; private set; }
        public static object? CopiedNormalValue { get; private set; }

        public virtual void PostRead() {}

        public bool Read()
        {
            if (File.Read())
            {
                PostRead();
                Changed = false;
                return true;
            }
            return false;
        }

        public bool Save()
        {
            bool result = File.Save();
            if (result)
            {
                Changed = false;
            }
            return result;
        }

        public bool SaveAs(string path)
        {
            bool result = File.SaveAs(path);
            if (result)
            {
                Changed = false;
                HeaderChanged?.Invoke();
            }
            return result;
        }

        public bool Reopen()
        {
            // TODO check Changed
            File.FileHandler.Reopen();
            bool result = Read();
            OnPropertyChanged(nameof(TreeViewItems));
            return result;
        }

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
            else if (arg is RszFieldInstanceViewModel fieldInstanceViewModel)
            {
                CopiedInstance = fieldInstanceViewModel.Instance;
            }
            else if (arg is GameObejctComponentViewModel componentViewModel)
            {
                CopiedInstance = componentViewModel.Instance;
            }
        }

        private void OnPasteInstance(object arg)
        {
            if (arg is RszInstance instance)
            {
                if (DoPasteInstance(instance))
                {
                    Changed = true;
                }
            }
            else if (arg is RszFieldInstanceViewModel fieldInstanceViewModel)
            {
                if (DoPasteInstance(fieldInstanceViewModel.Instance))
                {
                    fieldInstanceViewModel.NotifyItemsChanged();
                    Changed = true;
                }
            }
            else if (arg is GameObejctComponentViewModel componentViewModel)
            {
                if (DoPasteInstance(componentViewModel.Instance))
                {
                    componentViewModel.NotifyItemsChanged();
                    Changed = true;
                }
            }
        }

        private bool DoPasteInstance(RszInstance instance)
        {
            if (CopiedInstance == null) return false;
            if (CopiedInstance.RszClass != instance.RszClass)
            {
                MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedInstance.RszClass.name, instance.RszClass.name));
                return false;
            }
            if (File.GetRSZ() is not RSZFile rsz) return false;
            return rsz.InstanceCopyValues(instance, CopiedInstance);
        }

        private static void OnCopyNormalField(object arg)
        {
            if (arg is RszFieldNormalViewModel item)
            {
                CopiedNormalField = item.Field;
                CopiedNormalValue = item.Value;
            }
        }

        private void OnPasteNormalField(object arg)
        {
            if (arg is RszFieldNormalViewModel item && CopiedNormalField != null && CopiedNormalValue != null)
            {
                if (CopiedNormalField.type != item.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedNormalField.type, item.Field.type));
                    return;
                }
                item.Value = CopiedNormalValue;
                Changed = true;
            }
        }

        private static void OnArrayItemCopy(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                CopiedInstance = item.Instance;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                CopiedNormalField = normalItem.Field;
                CopiedNormalValue = normalItem.Value;
            }
        }

        /// <summary>
        /// 移除数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemRemove(object arg)
        {
            if (arg is BaseRszFieldArrayItemViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                rsz.ArrayRemoveItem(item.Values, item.Value);
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 重复数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemDuplicate(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                rsz.ArrayInsertInstance(item.Values, item.Instance, item.Index + 1);
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                rsz.ArrayInsertItem(normalItem.Values, normalItem.Value, normalItem.Index + 1);
                normalItem.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 重复多次数组项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemDuplicateMulti(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            Views.InputDialog dialog = new()
            {
                Title = Texts.Duplicate,
                Message = Texts.InputDulicateCount,
                InputText = "1",
                Owner = Application.Current.MainWindow,
            };
            // 显示对话框，并等待用户输入
            if (dialog.ShowDialog() != true) return;
            string userInput = dialog.InputText;
            int count = int.Parse(userInput);
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                for (int i = 0; i < count; i++)
                {
                    rsz.ArrayInsertInstance(item.Values, item.Instance, item.Index + 1);
                }
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem)
            {
                for (int i = 0; i < count; i++)
                {
                    rsz.ArrayInsertItem(normalItem.Values, normalItem.Value, normalItem.Index + 1);
                }
                normalItem.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 在数组项后面粘贴
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemPasteAfter(object arg)
        {
            if (File.GetRSZ() is not RSZFile rsz) return;
            if (arg is RszFieldArrayInstanceItemViewModel item && CopiedInstance != null)
            {
                if (CopiedInstance.RszClass != item.Instance.RszClass)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedInstance.RszClass.name, item.Instance.RszClass.name));
                    return;
                }
                rsz.ArrayInsertInstance(item.Values, CopiedInstance, item.Index + 1);
                item.Array.NotifyItemsChanged();
                Changed = true;
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem
                && CopiedNormalField != null && CopiedNormalValue != null)
            {
                if (CopiedNormalField.type != normalItem.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedNormalField.type, normalItem.Field.type));
                    return;
                }
                rsz.ArrayInsertItem(normalItem.Values, CopiedNormalValue, normalItem.Index + 1);
                normalItem.Array.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴到自身
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemPasteToSelf(object arg)
        {
            if (arg is RszFieldArrayInstanceItemViewModel item)
            {
                if (DoPasteInstance(item.Instance))
                {
                    item.NotifyItemsChanged();
                    Changed = true;
                }
            }
            else if (arg is RszFieldArrayNormalItemViewModel normalItem
                && CopiedNormalField != null && CopiedNormalValue != null)
            {
                if (CopiedNormalField.type != normalItem.Field.type)
                {
                    MessageBoxUtils.Error(string.Format(Texts.RszClassMismatch, CopiedNormalField.type, normalItem.Field.type));
                    return;
                }
                normalItem.Value = CopiedNormalValue;
                Changed = true;
            }
        }

        /// <summary>
        /// 数组新建项
        /// </summary>
        /// <param name="arg"></param>
        private void OnArrayItemNew(object arg)
        {
            if (arg is RszFieldArrayViewModel item && File.GetRSZ() is RSZFile rsz)
            {
                string? className = null;
                if (item.Field.IsReference)
                {
                    className = RszInstance.GetElementType(item.Field.original_type);
                    Views.InputDialog dialog = new()
                    {
                        Title = Texts.NewItem,
                        Message = Texts.InputClassName,
                        InputText = className,
                        Owner = Application.Current.MainWindow,
                    };
                    if (dialog.ShowDialog() != true) return;
                    className = dialog.InputText;
                }
                var newItem = RszInstance.CreateArrayItem(rsz.RszParser, item.Field, className);
                if (newItem == null) return;
                if (newItem is RszInstance instance)
                {
                    rsz.ArrayInsertInstance(item.Values, instance, clone: false);
                }
                else
                {
                    rsz.ArrayInsertItem(item.Values, newItem);
                }
                item.NotifyItemsChanged();
                Changed = true;
            }
        }

        /// <summary>
        /// 粘贴到数组
        /// </summary>
        private void OnArrayItemPaste(object arg)
        {
            if (arg is RszFieldArrayViewModel item && CopiedInstance != null &&
                File.GetRSZ() is RSZFile rsz)
            {
                string className = RszInstance.GetElementType(item.Field.original_type);
                if (className != CopiedInstance.RszClass.name &&
                    !MessageBoxUtils.Confirm(string.Format(
                        Texts.RszClassMismatchConfirm, CopiedInstance.RszClass.name, className)))
                {
                    return;
                }
                rsz.ArrayInsertInstance(item.Values, CopiedInstance);
                item.NotifyItemsChanged();
                Changed = true;
            }
        }

        private void OnSearchInstances(object arg)
        {
            SearchInstanceList ??= new();
            SearchInstanceList.Clear();
            if (File.GetRSZ() is not RSZFile rsz) return;
            InstanceFilter filter = new(InstanceSearchViewModel);
            if (!filter.Enable) return;
            foreach (var instance in rsz.InstanceList)
            {
                if (filter.IsMatch(instance))
                {
                    SearchInstanceList.Add(instance);
                }
            }
        }

        public abstract IEnumerable<object> TreeViewItems { get; }
    }


    public class GameObejctComponentViewModel(IGameObjectData gameObject, RszInstance instance) : BaseViewModel
    {
        public IGameObjectData GameObject { get; } = gameObject;
        public RszInstance Instance { get; } = instance;

        public string Name => Instance.Name;
        public IEnumerable<object> Items => Converters.RszInstanceFieldsConverter.GetItems(Instance);

        public void NotifyItemsChanged()
        {
            OnPropertyChanged(nameof(Items));
        }

        public override string ToString()
        {
            return Instance.Name;
        }

        public static ObservableCollection<GameObejctComponentViewModel> MakeList(IGameObjectData gameObject)
        {
            ObservableCollection<GameObejctComponentViewModel> list = new();
            foreach (var item in gameObject.Components)
            {
                list.Add(new(gameObject, item));
            }
            gameObject.Components.CollectionChanged += (_, e) =>
            {
                ObjectModelUtils.SyncObservableCollection(list,
                    obj => new GameObejctComponentViewModel(gameObject, (RszInstance)obj), e);
            };
            return list;
        }
    }
}
