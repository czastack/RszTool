namespace RszTool.App.ViewModels
{
    public class UserFileViewModel(UserFile file) : BaseRszFileViewModel
    {
        public override BaseRszFile File => UserFile;
        public UserFile UserFile { get; } = file;

        public RszViewModel RszViewModel => new(UserFile.RSZ!);

        public override IEnumerable<object> TreeViewItems
        {
            get
            {
                yield return new TreeItemViewModel("Instances", RszViewModel.Instances);
                yield return new TreeItemViewModel("Objects", RszViewModel.Objects);
            }
        }
    }
}
