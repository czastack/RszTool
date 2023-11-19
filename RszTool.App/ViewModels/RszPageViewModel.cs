using RszTool.App.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RszTool.App.ViewModels
{
    public class RszPageViewModel : INotifyPropertyChanged
    {
        public RszPageViewModel()
        {
            InstancesList = new RszInstancesViewModel(InstanceTestData.GetItems());
        }

        public RszInstancesViewModel InstancesList { get; }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
