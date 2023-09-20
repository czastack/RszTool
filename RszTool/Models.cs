using System.Runtime.CompilerServices;
using RszTool.Common;

namespace RszTool
{
    public class DynamicModel
    {
        protected SortedList<string, int> FieldAddress = new();

        public long Start { get; private set; }
        private FileHandler? Handler { get; set; }

        public void StartRead(FileHandler handler)
        {
            Start = handler.FTell();
            Handler = handler;
            FieldAddress.Clear();
        }
    }
}
