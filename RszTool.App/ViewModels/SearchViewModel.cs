using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RszTool.App.ViewModels
{
    public class InstanceSearchViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string InstanceName { get; set; } = "";
        public TextSearchOptionViewModel InstanceNameOption { get; } = new();
        public string FieldName { get; set; } = "";
        public TextSearchOptionViewModel FieldNameOption { get; } = new();
        public string FieldValue { get; set; } = "";
        public TextSearchOptionViewModel FieldValueOption { get; } = new();
    }


    public class GameobjectSearchViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public string GameObjectName { get; set; } = "";
        public TextSearchOptionViewModel GameObjectNameOption { get; } = new();
        // 按包含的组件搜索
        public InstanceSearchViewModel ComponentSearch { get; } = new();
        public bool IncludeChildren { get; set; } = false;
    }


    public class TextSearchOptionViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public bool MatchCase { get; set; }
        public bool MatchWholeWord { get; set; }
        public bool Regex { get; set; }
    }


    /// <summary>
    /// 用于过滤字符串
    /// </summary>
    public class TextMatcher
    {
        public Regex? Regex { get; private set; }
        public Predicate<string> IsMatch { get; private set; }
        public bool Enable { get; set; }

        public TextMatcher(string pattern, TextSearchOptionViewModel option)
        {
            Enable = true;
            if (string.IsNullOrWhiteSpace(pattern))
            {
                IsMatch = (text) => true;
                Enable = false;
            }
            else if (option.Regex)
            {
                Regex = new Regex(pattern, option.MatchCase ?
                    RegexOptions.None : RegexOptions.IgnoreCase);
                IsMatch = (text) => Regex.IsMatch(text);
            }
            else if (option.MatchWholeWord)
            {
                Regex = new Regex($"\\b{pattern}\\b", option.MatchCase ?
                    RegexOptions.None : RegexOptions.IgnoreCase);
                IsMatch = (text) => Regex.IsMatch(text);
            }
            else
            {
                var comparison = option.MatchCase ?
                    StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                IsMatch = (text) =>
                {
                    return text.IndexOf(pattern, comparison) > -1;
                };
            }
        }
    }


    /// <summary>
    /// 过滤实例，每次参数变更后重新生成
    /// </summary>
    public class InstanceFilter
    {
        private readonly TextMatcher instanceNameMatcher;
        private readonly TextMatcher fieldNameMatcher;
        private readonly TextMatcher fieldValueMatcher;
        private Dictionary<uint, bool>? classMatchedfieldName = null;

        public bool Enable => instanceNameMatcher.Enable || fieldNameMatcher.Enable || fieldValueMatcher.Enable;

        public InstanceFilter(InstanceSearchViewModel args)
        {
            instanceNameMatcher = new(args.InstanceName, args.InstanceNameOption);
            fieldNameMatcher = new(args.FieldName, args.FieldNameOption);
            fieldValueMatcher = new(args.FieldValue, args.FieldValueOption);
        }

        public bool IsMatch(RszInstance instance)
        {
            if (instanceNameMatcher.Enable && !instanceNameMatcher.IsMatch(instance.Name))
            {
                return false;
            }
            if (fieldNameMatcher.Enable)
            {
                classMatchedfieldName ??= new();
                bool matched = false;
                if (classMatchedfieldName.TryGetValue(instance.RszClass.typeId, out bool value))
                {
                    matched = value;
                }
                else
                {
                    foreach (var field in instance.Fields)
                    {
                        if (fieldNameMatcher.IsMatch(field.name))
                        {
                            matched = true;
                            break;
                        }
                    }
                    classMatchedfieldName[instance.RszClass.typeId] = matched;
                }
                if (!matched) return false;
            }
            if (fieldValueMatcher.Enable && instance.RSZUserData == null)
            {
                bool matched = false;
                var fields = instance.Fields;
                for (int i = 0; i < instance.Fields.Length; i++)
                {
                    RszField? field = instance.Fields[i];
                    if (field.array)
                    {
                        List<object> list = (List<object>)instance.Values[i];
                        for (int j = 0; j < list.Count; j++)
                        {
                            var valueStr = list[j].ToString();
                            if (valueStr != null && fieldValueMatcher.IsMatch(valueStr))
                            {
                                matched = true;
                                break;
                            }
                        }
                        if (matched) break;
                    }
                    else
                    {
                        var valueStr = instance.Values[i].ToString();
                        if (valueStr != null && fieldValueMatcher.IsMatch(valueStr))
                        {
                            matched = true;
                            break;
                        }
                    }
                }
                if (!matched) return false;
            }
            return true;
        }
    }


    public class ScnGameObjectFilter
    {
        private readonly TextMatcher gameObjectNameMatcher;
        private readonly InstanceFilter componentFilter;
        public bool Enable => gameObjectNameMatcher.Enable || componentFilter.Enable;

        public ScnGameObjectFilter(GameobjectSearchViewModel args)
        {
            gameObjectNameMatcher = new(args.GameObjectName, args.GameObjectNameOption);
            componentFilter = new(args.ComponentSearch);
        }

        public bool IsMatch(ScnFile.GameObjectData gameObject)
        {
            if (gameObjectNameMatcher.Enable &&
                (gameObject.Name == null || !gameObjectNameMatcher.IsMatch(gameObject.Name)))
            {
                return false;
            }
            if (componentFilter.Enable)
            {
                bool matched = false;
                foreach (var instance in gameObject.Components)
                {
                    if (componentFilter.IsMatch(instance))
                    {
                        matched = true;
                        break;
                    }
                }
                if (!matched) return false;
            }
            return true;
        }
    }
}
