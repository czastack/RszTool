using System.Collections.ObjectModel;
using System.ComponentModel;

namespace RszTool.App.ViewModels
{
    public interface IFieldValueViewModel
    {
        RszField Field {  get; }
        string Name { get; }
        object Value { get; set; }
    }


    /// <summary>
    /// 字段
    /// </summary>
    public class BaseRszFieldViewModel(RszInstance instance, int index) : INotifyPropertyChanged
    {
        protected readonly RszInstance instance = instance;

        public int Index { get; } = index;

        public RszField Field => instance.RszClass.fields[Index];
        public virtual string Name => Field.name;
        public RszFieldType Type => Field.type;
        public string OriginalType => Field.original_type;

        public virtual object Value
        {
            get => instance.Values[Index];
            set
            {
                instance.Values[Index] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    /// <summary>
    /// 普通字段
    /// </summary>
    public class RszFieldNormalViewModel(RszInstance instance, int index) :
        BaseRszFieldViewModel(instance, index), IFieldValueViewModel
    {
    }


    /// <summary>
    /// object字段
    /// </summary>
    public class RszFieldInstanceViewModel(RszInstance instance, int index) :
        BaseRszFieldViewModel(instance, index)
    {
        public RszInstance Instance => (RszInstance)instance.Values[Index];
        public override string Name => $"{Field.name} : {Instance.Name}";
        public override object Value =>
            RszInstanceToFieldViewModels.Convert(Instance);
    }


    /// <summary>
    /// array字段
    /// </summary>
    public class RszFieldArrayViewModel(RszInstance instance, int index) :
        BaseRszFieldViewModel(instance, index)
    {
        public override string Name => $"{Field.name} : {OriginalType}";

        public override object Value
        {
            get
            {
                if (itemViewModels == null)
                {
                    var values = (List<object>)instance.Values[Index];
                    bool isReference = Field.IsReference;
                    itemViewModels = new(values.Count);
                    for (int i = 0; i < values.Count; i++)
                    {
                        itemViewModels.Add(isReference ?
                            new RszFieldArrayInstanceItemViewModel(Field, values, i) :
                            new RszFieldArrayNormalItemViewModel(Field, values, i));
                    }
                }
                return itemViewModels;
            }
        }

        private List<BaseRszFieldArrayItemViewModel>? itemViewModels;
    }


    /// <summary>
    /// 数组的项
    /// </summary>
    public class BaseRszFieldArrayItemViewModel(
            RszField field, List<object> values, int arrayIndex) : INotifyPropertyChanged
    {
        public int Index { get; } = arrayIndex;

        public RszField Field { get; } = field;
        public virtual string Name => $"{Index}:";
        protected List<object> Values { get; } = values;

        public RszFieldType Type => Field.type;
        public string OriginalType => Field.original_type;

        public virtual object Value
        {
            get => Values[Index];
            set
            {
                Values[Index] = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }


    /// <summary>
    /// 普通数组的项
    /// </summary>
    public class RszFieldArrayNormalItemViewModel(RszField field, List<object> values, int arrayIndex) :
        BaseRszFieldArrayItemViewModel(field, values, arrayIndex), IFieldValueViewModel
    {
    }


    /// <summary>
    /// object数组的项
    /// </summary>
    public class RszFieldArrayInstanceItemViewModel(RszField field, List<object> values, int arrayIndex) :
        BaseRszFieldArrayItemViewModel(field, values, arrayIndex)
    {
        public RszInstance Instance => (RszInstance)Values[Index];
        public override string Name => $"{Index}: {Instance.Name}";
        public override object Value =>
            RszInstanceToFieldViewModels.Convert(Instance);
    }
}
