using System.Windows.Data;
using System.Windows.Markup;

namespace RszTool.App.Resources
{
    public class TextExtension(string key) : MarkupExtension
    {
        public string Key { get; set; } = key;

        public override object? ProvideValue(IServiceProvider serviceProvider)
        {
            return TextsManager.Instance[Key];
        }
    }


    public class TextBindingExtension(string key) : MarkupExtension
    {
        public string Key { get; set; } = key;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = TextsManager.Instance,
                Mode = BindingMode.OneWay
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
