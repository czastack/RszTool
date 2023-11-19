using RszTool.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RszTool.App.Views
{
    internal class FieleTypeDataTemplateSelector : DataTemplateSelector
    {
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (container is FrameworkElement element && item is IFieldValueViewModel viewModel)
            {
                switch (viewModel.Field.type)
                {
                    case RszFieldType.Vec2:
                    case RszFieldType.Float2:
                        return (DataTemplate)element.FindResource("InputVec2");
                    case RszFieldType.Vec3:
                    case RszFieldType.Float3:
                        return (DataTemplate)element.FindResource("InputVec3");
                    case RszFieldType.Vec4:
                    case RszFieldType.Float4:
                        return (DataTemplate)element.FindResource("InputVec4");
                    default:
                        return (DataTemplate)element.FindResource("InputText");
                }
            }

            return base.SelectTemplate(item, container);
        }
    }
}
