using System.ComponentModel;
using System.IO;

namespace RszTool.App.ViewModels
{
    public abstract class BaseRszFileViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public abstract BaseRszFile File { get; }
        public string? FilePath => File.FileHandler.FilePath;
        public string? FileName
        {
            get
            {
                string? path = FilePath;
                return path != null ? Path.GetFileName(path) : null;
            }
        }

        public bool SaveAs(string path)
        {
            bool result = File.SaveAs(path);
            if (result)
            {
                PropertyChanged?.Invoke(this, new(nameof(FilePath)));
            }
            return result;
        }
    }


    public class UserFileViewModel(UserFile userFile) : BaseRszFileViewModel
    {
        public override UserFile File { get; } = userFile;
    }


    public class PfbFileViewModel(PfbFile pfbFile) : BaseRszFileViewModel
    {
        public override PfbFile File { get; } = pfbFile;
    }


    public class ScnFileViewModel(ScnFile scnFile) : BaseRszFileViewModel
    {
        public override  ScnFile File { get; } = scnFile;
    }
}
