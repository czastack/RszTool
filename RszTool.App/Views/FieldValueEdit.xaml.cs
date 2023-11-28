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

        public FieldValueEdit()
        {
            InitializeComponent();
        }

        public bool ValueChanged
        {
            get { return (bool)GetValue(ValueChangedProperty); }
            set { SetValue(ValueChangedProperty, value); }
        }

        private void OnBindingSourceUpdated(object sender, DataTransferEventArgs args)
        {
            if (args.Property == TextBox.TextProperty)
            {
                ValueChanged = true;
            }
        }

        private void OnGuidNew(object sender, RoutedEventArgs e)
        {
            ((IFieldValueViewModel)DataContext).Value = Guid.NewGuid();
        }
    }
}
