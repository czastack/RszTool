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
            handler.AddStringToWrite(resourcePath);
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
            handler.AddStringToWrite(userdataPath);
            handler.Write(ref pathOffset);
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
