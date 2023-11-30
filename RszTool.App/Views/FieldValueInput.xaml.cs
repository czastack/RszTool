// for VectorBindingWrapper
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RszTool.App.Views
{
    /// <summary>
    /// FieldValueInput.xaml 的交互逻辑
    /// </summary>
    public partial class FieldValueInput : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(object), typeof(FieldValueInput),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSetValue));

        public static readonly DependencyProperty ValueChangedProperty =
            DependencyProperty.Register("ValueChanged", typeof(bool), typeof(FieldValueInput),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty ValueTypeProperty =
            DependencyProperty.Register("ValueType", typeof(RszFieldType), typeof(FieldValueInput),
                new PropertyMetadata(RszFieldType.not_init, OnSetValueType));

        public static readonly DependencyProperty UpdateSourceProperty =
            DependencyProperty.Register("UpdateSource", typeof(bool), typeof(FieldValueInput),
                new PropertyMetadata(false));

        private static DataTemplate SelectTemplate(RszFieldType fieldType, FrameworkElement element)
        {
            var resource = fieldType switch
            {
                RszFieldType.Bool => element.FindResource("InputBool"),
                RszFieldType.Vec2 or RszFieldType.Float2 or RszFieldType.Point
                    or RszFieldType.Range or RszFieldType.RangeI => element.FindResource("InputVec2"),
                RszFieldType.Vec3 or RszFieldType.Float3 => element.FindResource("InputVec3"),
                RszFieldType.Vec4 or RszFieldType.Float4 or RszFieldType.Quaternion => element.FindResource("InputVec4"),
                RszFieldType.Mat4 => element.FindResource("InputMat4"),
                RszFieldType.Guid or RszFieldType.GameObjectRef or RszFieldType.Uri => element.FindResource("InputGuid"),
                RszFieldType.Color => element.FindResource("InputColor"),
                RszFieldType.OBB => element.FindResource("InputOBB"),
                RszFieldType.Sphere => element.FindResource("InputSphere"),
                RszFieldType.AABB => element.FindResource("InputAABB"),
                RszFieldType.Capsule => element.FindResource("InputCapsule"),
                RszFieldType.Area => element.FindResource("InputArea"),
                _ => element.FindResource("InputText"),
            };
            return (DataTemplate)resource;
        }

        private static void OnSetValue(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FieldValueInput control)
            {
#if !NET5_0_OR_GREATER
                if (e.NewValue is Vector2 or Vector3 or Vector4)
                {
                    control.Content = new VectorBindingWrapper(e.NewValue);
                }
                else
                {
                    control.Content = e.NewValue;
                }
#else
                control.Content = e.NewValue;
#endif
            }
        }

        private static void OnSetValueType(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FieldValueInput control)
            {
                control.ContentTemplate = SelectTemplate((RszFieldType)e.NewValue, control);
            }
        }

        public FieldValueInput()
        {
            InitializeComponent();
        }

        public RszFieldType ValueType
        {
            get { return (RszFieldType)GetValue(ValueTypeProperty); }
            set { SetValue(ValueTypeProperty, value); }
        }

        public object Value
        {
            get { return GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public bool ValueChanged
        {
            get { return (bool)GetValue(ValueChangedProperty); }
            set { SetValue(ValueChangedProperty, value); }
        }

        public bool UpdateSource
        {
            get { return (bool)GetValue(UpdateSourceProperty); }
            set { SetValue(UpdateSourceProperty, value); }
        }

        private void OnBindingSourceUpdated(object sender, DataTransferEventArgs args)
        {
            if (args.Property == TextBox.TextProperty)
            {
                ValueChanged = true;
                // if bind ValueType, force update source value
                if (UpdateSource)
                {
                    GetBindingExpression(ValueProperty).UpdateSource();
                }
            }
        }

        private void OnGuidNew(object sender, RoutedEventArgs e)
        {
            Value = Guid.NewGuid();
            ValueChanged = true;
        }
    }


    public class VectorBindingWrapper(dynamic value)
    {
        public dynamic Value { get; set; } = value;

        public float this[int index]
        {
            get => index switch {
                0 => Value.X,
                1 => Value.Y,
                2 => Value.Z,
                3 => Value.W,
                _ => throw new IndexOutOfRangeException($"Index should be between 0 and 3, but got {index}"),
            };
            set
            {
                switch (index)
                {
                    case 0: Value.X = value; break;
                    case 1: Value.Y = value; break;
                    case 2: Value.Z = value; break;
                    case 3: Value.W = value; break;
                }
            }
        }
    }
}
