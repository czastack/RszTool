namespace RszTool
{
    public abstract class BaseRszFile
    {
        public RszFileOption Option { get; set; }
        public long Start { get; set; }
        public long Size { get; protected set; }

        public FileHandler FileHandler { get; set; }
        public RszParser RszParser => Option.RszParser;

        public BaseRszFile(RszFileOption option, FileHandler fileHandler)
        {
            Option = option;
            FileHandler = fileHandler;
        }

        public bool Read()
        {
            Start = FileHandler.Tell();
            bool result = DoRead();
            Size = FileHandler.Tell() - Start;
            return result;
        }

        public bool Read(long start, bool jumpBack = true)
        {
            var handler = FileHandler;
            long pos = handler.Tell();
            if (start != -1)
            {
                handler.Seek(start);
            }
            bool result = Read();
            if (jumpBack) handler.Seek(pos);
            return result;
        }

        public bool Write()
        {
            Start = FileHandler.Tell();
            bool result = DoWrite();
            Size = FileHandler.Tell() - Start;
            return result;
        }

        public bool Write(long start, bool jumpBack = true)
        {
            var handler = FileHandler;
            long pos = handler.Tell();
            if (start != -1)
            {
                handler.Seek(start);
            }
            bool result = Write();
            if (jumpBack) handler.Seek(pos);
            return result;
        }

        public bool Rewrite(bool jumpBack = true)
        {
            return Write(Start, jumpBack);
        }

        protected abstract bool DoRead();

        protected abstract bool DoWrite();
    }
}
