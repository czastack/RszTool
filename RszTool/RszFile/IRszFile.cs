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
            Start = FileHandler.FTell();
            return true;
        }

        protected void EndRead()
        {
            Size = FileHandler.FTell() - Start;
        }

        public virtual bool Write()
        {
            if (Start != -1) FileHandler.FSeek(Start);
            return true;
        }
    }
}
