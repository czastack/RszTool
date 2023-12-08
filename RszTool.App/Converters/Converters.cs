using System.Windows.Data;
using RszTool.App.Common;
using RszTool.App.ViewModels;

namespace RszTool.App.Converters
{
    [ValueConversion(typeof(RszInstance), typeof(IEnumerable<object>))]
    public class RszInstanceFieldsConverter : IValueConverter
    {
        public static IEnumerable<object> Convert(RszInstance instance)
        {
            if (instance.RSZUserData is RSZUserDataInfo userDataInfo)
            {
                yield return new ClassViewModel("UserDataInfo", userDataInfo, ["Path"]);
            }
            else
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


    [ValueConversion(typeof(ScnFile.GameObjectData), typeof(IEnumerable<object>))]
    public class ScnGameObjectDataSubItemsConverter : IValueConverter
    {
        public static IEnumerable<object> Convert(ScnFile.GameObjectData gameObject)
        {
            if (gameObject.Instance != null)
            {
                yield return gameObject.Instance;
            }
            yield return new ClassViewModel("GameObjectInfo", gameObject, ["Guid"]);
            if (gameObject.Prefab != null)
            {
                yield return new ClassViewModel(gameObject.Prefab, ["Path"]);
            }
            yield return new TreeItemViewModel("Components", gameObject.Components);
            yield return new TreeItemViewModel("Children", gameObject.Children);
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


    [ValueConversion(typeof(ScnFile.FolderData), typeof(IEnumerable<object>))]
    public class ScnFolderDataSubItemsConverter : IValueConverter
    {
        public static IEnumerable<object> Convert(ScnFile.FolderData folder)
        {
            if (folder.Instance != null)
            {
                yield return folder.Instance;
            }
            yield return new TreeItemViewModel("Children", folder.Children);
            yield return new TreeItemViewModel("GameObjects", folder.GameObjects);
            yield return new TreeItemViewModel("Prefabs", folder.Prefabs);
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


    [ValueConversion(typeof(PfbFile.GameObjectData), typeof(IEnumerable<object>))]
    public class PfbGameObjectDataSubItemsConverter : IValueConverter
    {
        public static IEnumerable<object> Convert(PfbFile.GameObjectData gameObject)
        {
            if (gameObject.Instance != null)
            {
                yield return gameObject.Instance;
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


    [ValueConversion(typeof(via.Color), typeof(string))]
    public class ColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((via.Color)value).Hex();
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return via.Color.Parse((string)value);
        }
    }


    [ValueConversion(typeof(Guid), typeof(string))]
    public class GuidConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((Guid)value).ToString();
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Guid.Parse((string)value);
        }
    }


    public class BytesConverter : IValueConverter
    {
        private int byteCount;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is byte[] bytes)
            {
                byteCount = bytes.Length;
                return ConvertUtils.ToHexString(bytes, " ");
            }
            else
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string str)
            {
                return ConvertUtils.FromHexString(str);
            }
            else
            {
                return Array.Empty<byte>();
            }
        }
    }
}
