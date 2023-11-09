namespace RszTool
{
    public abstract class BaseRszFile
    {
        public RszHandler RszHandler { get; set; }
        public long Start { get; set; }
        public long Size { get; protected set; }

        public FileHandler FileHandler => RszHandler.FileHandler;
        public RszParser RszParser => RszHandler.RszParser;

        public BaseRszFile(RszHandler rszHandler)
        {
            RszHandler = rszHandler;
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
