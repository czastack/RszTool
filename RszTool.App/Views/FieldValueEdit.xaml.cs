using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using RszTool.App.ViewModels;

namespace RszTool.App.Views
{
    /// <summary>
    /// FieldValueEdit.xaml 的交互逻辑
    /// </summary>
    public partial class FieldValueEdit : UserControl
    {
        public static readonly DependencyProperty ValueChangedProperty =
            DependencyProperty.Register("ValueChanged", typeof(bool), typeof(FieldValueEdit),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public static readonly DependencyProperty EnumDictProperty =
            DependencyProperty.Register("EnumDict", typeof(EnumDict), typeof(FieldValueEdit),
                new PropertyMetadata(null));

        public FieldValueEdit()
        {
            InitializeComponent();
        }

        public bool ValueChanged
        {
            get => (bool)GetValue(ValueChangedProperty);
            set => SetValue(ValueChangedProperty, value);
        }

        public EnumDict? EnumDict
        {
            get => GetValue(EnumDictProperty) as EnumDict;
            set => SetValue(EnumDictProperty, value);
        }
    }
}
