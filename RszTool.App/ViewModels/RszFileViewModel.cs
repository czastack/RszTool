using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RszTool.App.ViewModels
{
    public class RszFileViewModel
    {
        
    }


    public class UserFileViewModel
    {
        public UserFileViewModel(UserFile userFile)
        {
            UserFile = userFile;
        }

        public UserFile UserFile { get; }
    }
}
