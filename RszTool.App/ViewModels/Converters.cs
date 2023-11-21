using System.Windows.Data;

namespace RszTool.App.ViewModels
{
    [ValueConversion(typeof(RszInstance), typeof(IEnumerable<BaseRszFieldViewModel>))]
    public class RszInstanceToFieldViewModels : IValueConverter
    {
        public static IEnumerable<BaseRszFieldViewModel> InstanceToFieldViewModels(RszInstance instance)
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
            return InstanceToFieldViewModels(instance);
        }

        public object? ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
