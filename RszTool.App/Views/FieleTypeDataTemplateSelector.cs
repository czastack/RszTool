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
                return viewModel.Field.type switch
                {
                    RszFieldType.Vec2 or RszFieldType.Float2 or RszFieldType.Range or RszFieldType.RangeI => (DataTemplate)element.FindResource("InputVec2"),
                    RszFieldType.Vec3 or RszFieldType.Float3 => (DataTemplate)element.FindResource("InputVec3"),
                    RszFieldType.Vec4 or RszFieldType.Float4 => (DataTemplate)element.FindResource("InputVec4"),
                    RszFieldType.Mat4 => (DataTemplate)element.FindResource("InputMat4"),
                    _ => (DataTemplate)element.FindResource("InputText"),
                };
            }

            return base.SelectTemplate(item, container);
        }
    }
}
