namespace RszTool.App.Common
{
    public static class ConvertUtils
    {
        public static object? ConvertRszValue(RszField field, object oldValue, object value)
        {
            if (field.type == RszFieldType.Data)
            {
                if (((byte[])oldValue).Length != ((byte[])value).Length)
                {
                    MessageBoxUtils.Warning("Length changed");
                    return null;
                }
            }
            if (oldValue.GetType() == value.GetType())
            {
                return value;
            }
            var type = RszInstance.RszFieldTypeToCSharpType(field.type);
            try
            {
                return Convert.ChangeType(value, type);
            }
            catch (Exception e)
            {
                MessageBoxUtils.Warning($"Format error: {e.Message}");
            }
            return null;
        }

#if NET5_0_OR_GREATER
        public static byte[] FromHexString(string hex)
        {
            if (hex.Contains(' '))
            {
                hex = hex.Replace(" ", "");
            }
            return Convert.FromHexString(hex);
        }

        public static string ToHexString(byte[] bytes, string sep = "")
        {
            if (string.IsNullOrEmpty(sep))
            {
                return Convert.ToHexString(bytes);
            }
            return BitConverter.ToString(bytes).Replace("-", sep);
        }

        public static string ToHexString(byte[] bytes, int offset, int length, string sep = "")
        {
            if (string.IsNullOrEmpty(sep))
            {
                return Convert.ToHexString(bytes, offset, length);
            }
            return BitConverter.ToString(bytes, offset, length).Replace("-", sep);
        }
#else
        public static int GetHexVal(char hex)
        {
            if (hex == ' ')
            {
                return 0;
            }
            int val = hex;
            return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }

        /// <summary>
        /// hex转byte[]
        /// </summary>
        public static byte[] FromHexString(string hex)
        {
            hex = hex.Replace(" ", "");
            if ((hex.Length % 2) != 0)
            {
                hex += " ";
            }
            byte[] bytes = new byte[hex.Length >> 1];
            for (int i = 0; i < bytes.Length; ++i)
            {
                bytes[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }
            return bytes;
        }

        /// <summary>
        /// byte[]转hex
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="sep"></param>
        /// <returns></returns>
        public static string ToHexString(byte[] bytes, string sep = "")
        {
            return BitConverter.ToString(bytes).Replace("-", sep);
        }

        public static string ToHexString(byte[] bytes, int offset, int length, string sep = "")
        {
            return BitConverter.ToString(bytes, offset, length).Replace("-", sep);
        }
#endif
    }
}
