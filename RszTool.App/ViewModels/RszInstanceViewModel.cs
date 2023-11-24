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

        public void NotifyValueChanged()
        {
            PropertyChanged?.Invoke(this, new(nameof(Value)));
        }
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
        public List<object> Values => (List<object>)instance.Values[Index];

        private IEnumerable<BaseRszFieldArrayItemViewModel> GetItems()
        {
            var values = (List<object>)instance.Values[Index];
            bool isReference = Field.IsReference;
            for (int i = 0; i < values.Count; i++)
            {
                yield return isReference ?
                    new RszFieldArrayInstanceItemViewModel(this, i) :
                    new RszFieldArrayNormalItemViewModel(this, i);
            }
        }

        public override object Value => GetItems();
    }


    /// <summary>
    /// 数组的项
    /// </summary>
    public class BaseRszFieldArrayItemViewModel(
            RszFieldArrayViewModel array, int index) : INotifyPropertyChanged
    {
        public RszFieldArrayViewModel Array { get; } = array;
        public int Index { get; } = index;

        public RszField Field { get; } = array.Field;
        public virtual string Name => $"{Index}:";
        public List<object> Values => Array.Values;

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
    public class RszFieldArrayNormalItemViewModel(RszFieldArrayViewModel array, int index) :
        BaseRszFieldArrayItemViewModel(array, index), IFieldValueViewModel
    {
    }


    /// <summary>
    /// object数组的项
    /// </summary>
    public class RszFieldArrayInstanceItemViewModel(RszFieldArrayViewModel array, int index) :
        BaseRszFieldArrayItemViewModel(array, index)
    {
        public RszInstance Instance => (RszInstance)Values[Index];
        public override string Name => $"{Index}: {Instance.Name}";
        public override object Value =>
            RszInstanceToFieldViewModels.Convert(Instance);
    }
}
