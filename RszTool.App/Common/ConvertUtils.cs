namespace RszTool.App.Common
{
    public static class ConvertUtils
    {
        public static object? ConvertRszValue(RszField field, object value)
        {
            var type = RszInstance.RszFieldTypeToCSharpType(field.type);
            try
            {
                return Convert.ChangeType(value, type);
            }
            catch (Exception)
            {
                MessageBoxUtils.Warning("Format error");
            }
            return null;
        }
    }
}
