using System.Reflection;

namespace RszTool.App.ViewModels
{
    public class ClassViewModel(object instance, string[] properties) : BaseTreeItemViewModel(instance.GetType().Name)
    {
        public static List<ClassPropertyViewModel> MakePropertyViewModels(object instance, string[] properties)
        {
            Type type = instance.GetType();
            List<ClassPropertyViewModel> propertyViewModels = [];
            foreach (var name in properties)
            {
                PropertyInfo? property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (property != null)
                {
                    propertyViewModels.Add(new ClassPropertyViewModel(instance, property));
                }
            }
            return propertyViewModels;
        }

        public object Instance { get; set; } = instance;
        public string[] Properties { get; set; } = properties;

        private List<ClassPropertyViewModel>? propertyViewModels;
        public List<ClassPropertyViewModel> PropertyViewModels =>
            propertyViewModels ??= MakePropertyViewModels(Instance, Properties);

        override public IEnumerable<object>? Items => PropertyViewModels;
    }


    public class ClassPropertyViewModel(object instance, PropertyInfo property)
    {
        public object Instance { get; set; } = instance;
        public PropertyInfo Property { get; set; } = property;

        public string Name => Property.Name;
        public string Type => Property.PropertyType.ToString();
        public object? Value
        {
            get => Property.GetValue(Instance);
            set => Property.SetValue(Instance, value);
        }
    }
}
