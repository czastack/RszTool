namespace RszTool
{
    public interface IRszFile
    {

    }


    public class BaseRszFile
    {
        public RszHandler RszHandler { get; set; }
        public long Start { get; protected set; }
        public long Size { get; protected set; }

        public FileHandler FileHandler => RszHandler.FileHandler;
        public RszParser RszParser => RszHandler.RszParser;

        public BaseRszFile(RszHandler rszHandler)
        {
            RszHandler = rszHandler;
        }


        public virtual bool Read()
        {
            Start = FileHandler.Tell();
            return true;
        }

        protected void EndRead()
        {
            Size = FileHandler.Tell() - Start;
        }

        public virtual bool Write()
        {
            if (Start != -1) FileHandler.Seek(Start);
            return true;
        }
    }
}
