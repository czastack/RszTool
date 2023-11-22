using RszTool.App.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RszTool.App.ViewModels
{
    public class RszTestViewModel : INotifyPropertyChanged
    {
        public RszTestViewModel()
        {
            InstanceList = InstanceTestData.GetItems().ToList();
        }

        public List<RszInstance> InstanceList { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
