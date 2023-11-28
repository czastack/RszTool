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
                var resource = viewModel.Field.type switch
                {
                    RszFieldType.Vec2 or RszFieldType.Float2 or RszFieldType.Range or RszFieldType.RangeI => element.FindResource("InputVec2"),
                    RszFieldType.Vec3 or RszFieldType.Float3 => element.FindResource("InputVec3"),
                    RszFieldType.Vec4 or RszFieldType.Float4 => element.FindResource("InputVec4"),
                    RszFieldType.Mat4 => element.FindResource("InputMat4"),
                    RszFieldType.Guid or RszFieldType.GameObjectRef => element.FindResource("InputGuid"),
                    RszFieldType.Color => element.FindResource("InputColor"),
                    _ => element.FindResource("InputText"),
                };
                return (DataTemplate)resource;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
