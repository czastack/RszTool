namespace RszTool
{
    public class ResourceInfo : BaseModel
    {
        public ulong pathOffset;
        public string? resourcePath;

        public override bool Read(FileHandler handler)
        {
            if (!base.Read(handler)) return false;
            handler.Read(ref pathOffset);
            EndRead(handler);
            resourcePath = handler.ReadWString((int)pathOffset);
            return true;
        }

        public override bool Write(FileHandler handler)
        {
            if (!base.Write(handler)) return false;
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

        public override bool Read(FileHandler handler)
        {
            if (!base.Read(handler)) return false;
            handler.Read(ref typeId);
            handler.Read(ref CRC);
            handler.Read(ref pathOffset);
            EndRead(handler);
            userdataPath = handler.ReadWString((int)pathOffset);
            return true;
        }

        public override bool Write(FileHandler handler)
        {
            if (!base.Write(handler)) return false;
            handler.Write(ref typeId);
            handler.Write(ref CRC);
            handler.Write(ref pathOffset);
            return true;
        }
    }


    public static class RszExtensions
    {
        public static bool Write(this IEnumerable<IModel> list, FileHandler handler)
        {
            foreach (var item in list)
            {
                if (!item.Write(handler)) return false;
            }
            return true;
        }
    }
}
