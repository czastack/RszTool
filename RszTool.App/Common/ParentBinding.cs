using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace RszTool.App.Common
{
    public class ParentBinding : MarkupExtension
    {
        public ParentBinding() {}

        public ParentBinding(string path)
        {
            Path = "Data." + path;
        }

        public string Path { get; set; } = ".";

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding(Path)
            {
                Source = new StaticResourceExtension("ParentData").ProvideValue(serviceProvider),
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
