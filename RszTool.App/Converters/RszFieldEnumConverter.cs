using System.Windows.Data;

namespace RszTool.App.Converters
{
    public class RszFieldEnumConverter : IValueConverter
    {
        private Type? TargetType { get; set; }

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            TargetType = value.GetType();
            if (TargetType == typeof(int))
            {
                return unchecked((ulong)(uint)(int)value);
            }
            else if (TargetType == typeof(uint))
            {
                return unchecked((ulong)(uint)value);
            }
            else if (TargetType == typeof(sbyte))
            {
                return unchecked((ulong)(byte)(sbyte)value);
            }
            else if (TargetType == typeof(byte))
            {
                return unchecked((ulong)(byte)value);
            }
            else if (TargetType == typeof(short))
            {
                return unchecked((ulong)(ushort)(short)value);
            }
            else if (TargetType == typeof(ushort))
            {
                return unchecked((ulong)(ushort)value);
            }
            else if (TargetType == typeof(long))
            {
                return unchecked((ulong)(long)value);
            }
            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ulong enumValue = (ulong)value;
            if (TargetType == typeof(int))
            {
                return unchecked((int)enumValue);
            }
            else if (TargetType == typeof(uint))
            {
                return unchecked((uint)enumValue);
            }
            else if (TargetType == typeof(sbyte))
            {
                return unchecked((sbyte)enumValue);
            }
            else if (TargetType == typeof(byte))
            {
                return unchecked((byte)enumValue);
            }
            else if (TargetType == typeof(short))
            {
                return unchecked((short)enumValue);
            }
            else if (TargetType == typeof(ushort))
            {
                return unchecked((ushort)enumValue);
            }
            else if (TargetType == typeof(long))
            {
                return unchecked((long)enumValue);
            }
            return value;
        }
    }
}
