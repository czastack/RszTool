namespace RszTool
{
    public class ResourceInfo : BaseModel
    {
        public ulong pathOffset;
        public string? Path { get; set; }

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref pathOffset);
            Path = handler.ReadWString((long)pathOffset);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.StringTableAdd(Path);
            handler.Write(pathOffset);
            return true;
        }
    }


    public class UserdataInfo : BaseModel
    {
        public uint typeId;
        public uint CRC;
        public ulong pathOffset;
        public string? Path { get; set; }

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref typeId);
            handler.Read(ref CRC);
            handler.Read(ref pathOffset);
            Path = handler.ReadWString((long)pathOffset);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.Write(ref typeId);
            handler.Write(ref CRC);
            handler.StringTableAdd(Path);
            handler.Write(ref pathOffset);
            return true;
        }
    }
}
