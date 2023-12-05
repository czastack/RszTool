using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RszTool.App.ViewModels
{
    public class SearchViewModel
    {
    }


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
}
