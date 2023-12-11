using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace RszTool.App.Common
{
    public static class ObjectModelUtils
    {
        public static void SyncObservableCollection<T>(
            ObservableCollection<T> list,
            Func<object, T> converter,
            NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
            case NotifyCollectionChangedAction.Add:
                for (int i = 0; i < e.NewItems!.Count; i++)
                {
                    list.Insert(e.NewStartingIndex + i, converter(e.NewItems[i]!));
                }
                break;

            case NotifyCollectionChangedAction.Move:
                if (e.OldItems!.Count == 1)
                {
                    list.Move(e.OldStartingIndex, e.NewStartingIndex);
                }
                else
                {
                    T[] items = list.Skip(e.OldStartingIndex).Take(e.OldItems.Count).ToArray();
                    for (int i = 0; i < e.OldItems.Count; i++)
                    {
                        list.RemoveAt(e.OldStartingIndex);
                    }

                    for (int i = 0; i < items.Length; i++)
                    {
                        list.Insert(e.NewStartingIndex + i, items[i]);
                    }
                }
                break;

            case NotifyCollectionChangedAction.Remove:
                for (int i = 0; i < e.OldItems!.Count; i++)
                    list.RemoveAt(e.OldStartingIndex);
                break;

            case NotifyCollectionChangedAction.Replace:
                // remove
                for (int i = 0; i < e.OldItems!.Count; i++)
                {
                    list.RemoveAt(e.OldStartingIndex);
                }
                // add
                goto case NotifyCollectionChangedAction.Add;

            case NotifyCollectionChangedAction.Reset:
                list.Clear();
                for (int i = 0; i < e.NewItems!.Count; i++)
                    list.Add(converter(e.NewItems[i]!));
                break;
            default:
                break;
            }
        }
    }
}
