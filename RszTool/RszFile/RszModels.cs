namespace RszTool
{
    public class ResourceInfo : BaseModel
    {
        public ulong pathOffset;
        public string? resourcePath;

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref pathOffset);
            resourcePath = handler.ReadWString((long)pathOffset);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.StringTableAdd(resourcePath);
            handler.Write(pathOffset);
            return true;
        }
    }


    public class UserdataInfo : BaseModel
    {
        public uint typeId;
        public uint CRC;
        public ulong pathOffset;
        public string? userdataPath;

        protected override bool DoRead(FileHandler handler)
        {
            handler.Read(ref typeId);
            handler.Read(ref CRC);
            handler.Read(ref pathOffset);
            userdataPath = handler.ReadWString((long)pathOffset);
            return true;
        }

        protected override bool DoWrite(FileHandler handler)
        {
            handler.Write(ref typeId);
            handler.Write(ref CRC);
            handler.StringTableAdd(userdataPath);
            handler.Write(ref pathOffset);
            return true;
        }
    }
}
