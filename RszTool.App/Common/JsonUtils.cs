using System.IO;
using System.Text.Json;

namespace RszTool.App.Common
{
    public static class JsonUtils
    {
        public static readonly JsonSerializerOptions deserializeOptions = new()
        {
            // Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 中文字不編碼
        };

        public static T? LoadJson<T>(string path)
        {
            using FileStream fileStream = File.OpenRead(path);
            return JsonSerializer.Deserialize<T>(fileStream);
        }

        public static object? LoadJson(string path, Type type)
        {
            using FileStream fileStream = File.OpenRead(path);
            return JsonSerializer.Deserialize(fileStream, type);
        }

        public static void DumpJson<T>(string path, T data, JsonSerializerOptions? options = null)
        {
            using FileStream fileStream = File.Create(path);
            options ??= deserializeOptions;
            JsonSerializer.Serialize(fileStream, data, typeof(T), options);
        }

        public static Dictionary<string, object?>? JsonElementToDict(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object)
            {
                return null;
            }
            Dictionary<string, object?> result = new();
            foreach (var item in element.EnumerateObject())
            {
                result[item.Name] = JsonElementToObject(item.Value);
            }
            return result;
        }

        public static object? JsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.Number => element.GetDecimal(),
                JsonValueKind.False => false,
                JsonValueKind.True => true,
                JsonValueKind.Undefined => null,
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Object => JsonElementToDict(element),
                JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToArray(),
                _ => null,
            };
        }
    }
}
