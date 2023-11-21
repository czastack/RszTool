namespace RszTool
{
    public abstract class BaseRszFile : IDisposable
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

        ~BaseRszFile()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FileHandler.Dispose();
            }
        }

        public bool Read()
        {
            Start = FileHandler.Tell();
            bool result = DoRead();
            Size = FileHandler.Tell() - Start;
            // Console.WriteLine($"{this} Start: {Start}, Read size: {Size}");
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
            // Console.WriteLine($"{this} Start: {Start}, Write size: {Size}");
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

        public bool WriteTo(FileHandler handler, bool saveFile = true)
        {
            FileHandler originHandler = FileHandler;
            if (handler == originHandler)
            {
                return Write();
            }
            // save as new file
            bool result;
            try
            {
                FileHandler = handler;
                result = Write();
                if (result && saveFile)
                {
                    handler.Save();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                result = false;
            }
            FileHandler = originHandler;
            return result;
        }

        public bool WriteTo(string path)
        {
            return WriteTo(new FileHandler(path, true));
        }

        public bool Save()
        {
            if (Write())
            {
                FileHandler.Save();
                return true;
            }
            return false;
        }

        public bool SaveAs(string path)
        {
            bool result = Write();
            FileHandler.SaveAs(path);
            return result;
        }
    }
}
