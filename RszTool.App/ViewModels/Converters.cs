using System.Windows.Data;

namespace RszTool.App.ViewModels
{
    [ValueConversion(typeof(RszInstance), typeof(IEnumerable<BaseRszFieldViewModel>))]
    public class RszInstanceToFieldViewModels : IValueConverter
    {
        public static IEnumerable<BaseRszFieldViewModel> Convert(RszInstance instance)
        {
            for (int i = 0; i < instance.Values.Length; i++)
            {
                var field = instance.RszClass.fields[i];
                yield return field.array ?
                    new RszFieldArrayViewModel(instance, i) :
                    field.IsReference ? new RszFieldInstanceViewModel(instance, i) :
                                        new RszFieldNormalViewModel(instance, i);
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var instance = (RszInstance)value;
            return Convert(instance);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    [ValueConversion(typeof(ScnFile.GameObjectData), typeof(IEnumerable<BaseTreeItemViewModel>))]
    public class ScnGameObjectDataSubItemsConverter : IValueConverter
    {
        public static IEnumerable<BaseTreeItemViewModel> Convert(ScnFile.GameObjectData gameObject)
        {
            if (gameObject.Instance != null)
            {
                yield return new TreeItemDelegate(gameObject.Instance.Name,
                    () => RszInstanceToFieldViewModels.Convert(gameObject.Instance));
            }
            yield return new TreeItemViewModel("Components", gameObject.Components);
            yield return new TreeItemViewModel("Children", gameObject.Children);
            if (gameObject.Prefab != null)
            {
                yield return new ClassViewModel(gameObject.Prefab, ["Path"]);
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var gameObject = (ScnFile.GameObjectData)value;
            return Convert(gameObject);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    [ValueConversion(typeof(ScnFile.FolderData), typeof(IEnumerable<BaseTreeItemViewModel>))]
    public class ScnFolderDataSubItemsConverter : IValueConverter
    {
        public static IEnumerable<BaseTreeItemViewModel> Convert(ScnFile.FolderData folder)
        {
            if (folder.Instance != null)
            {
                yield return new TreeItemDelegate(folder.Instance.Name,
                    () => RszInstanceToFieldViewModels.Convert(folder.Instance));
            }
            yield return new TreeItemViewModel("Children", folder.Children);
            yield return new TreeItemViewModel("GameObjects", folder.GameObjects);
            if (folder.Prefabs.Count > 0)
            {
                yield return new TreeItemViewModel("Prefabs",
                    folder.Prefabs.Select(item => new ClassViewModel(item, ["Path"])));
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var folder = (ScnFile.FolderData)value;
            return Convert(folder);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }


    [ValueConversion(typeof(PfbFile.GameObjectData), typeof(IEnumerable<BaseTreeItemViewModel>))]
    public class PfbGameObjectDataSubItemsConverter : IValueConverter
    {
        public static IEnumerable<BaseTreeItemViewModel> Convert(PfbFile.GameObjectData gameObject)
        {
            if (gameObject.Instance != null)
            {
                yield return new TreeItemDelegate(gameObject.Instance.Name,
                    () => RszInstanceToFieldViewModels.Convert(gameObject.Instance));
            }
            if (gameObject.Components.Count > 0)
            {
                yield return new TreeItemViewModel("Components", gameObject.Components);
            }
            if (gameObject.Children.Count > 0)
            {
                yield return new TreeItemViewModel("Children", gameObject.Children);
            }
        }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var gameObject = (PfbFile.GameObjectData)value;
            return Convert(gameObject);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
