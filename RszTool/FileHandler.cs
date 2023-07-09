namespace RszTool
{
    public class FileHandler : IDisposable
    {
        public FileStream FileStream { get; private set; }
        public BinaryReader Reader { get; private set; }
        public BinaryWriter Writer { get; private set; }

        public FileHandler(string path)
        {
            FileStream = new FileStream(path, FileMode.Open);
            Reader = new BinaryReader(FileStream);
            Writer = new BinaryWriter(FileStream);
        }

        public void Dispose()
        {
            Reader.Dispose();
            Writer.Dispose();
            FileStream.Dispose();
        }

        public long FileSize()
        {
            return FileStream.Length;
        }

        public void FSeek(long tell)
        {
            FileStream.Position = tell;
        }
    }
}
