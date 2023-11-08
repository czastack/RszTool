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
            resourcePath = handler.ReadWString((long)pathOffset);
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
            userdataPath = handler.ReadWString((long)pathOffset);
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


    /// <summary>
    /// 待写入的字符串
    /// </summary>
    /// <param name="OffsetStart">字符串偏移的地址</param>
    /// <param name="Text">待写入文本</param>
    public record StringToWrite(long OffsetStart, string Text);
}
