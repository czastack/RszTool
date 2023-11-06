using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RszTool
{
    public class RszParser
    {
        public static Dictionary<string, RszParser> instanceDict = new();

        public static RszParser GetInstance(string jsonPath)
        {
            if (!instanceDict.TryGetValue(jsonPath, out var rszParser))
            {
                rszParser = new RszParser(jsonPath);
                instanceDict[jsonPath] = rszParser;
            }
            return rszParser;
        }

        private Dictionary<uint, RszClass> classDict;

        public RszParser(string jsonPath)
        {
            classDict = new();
            using FileStream fileStream = File.OpenRead(jsonPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, RszClass>>(fileStream);
            if (dict != null)
            {
                foreach (var item in dict)
                {
                    classDict[uint.Parse(item.Key, NumberStyles.HexNumber)] = item.Value;
                }
            }
        }

        public static TypeIDs GetFieldTypeInternal(string typeName)
        {
            return Enum.Parse<TypeIDs>(typeName, true);
        }

        public string GetRSZClassName(uint classHash)
        {
            return classDict.TryGetValue(classHash, out var rszClass) ? rszClass.name : "Unknown Class!";
        }

        public int GetFieldCount(uint classHash)
        {
            return classDict.TryGetValue(classHash, out var rszClass) ? rszClass.fields.Length : -1;
        }

        public RszField? GetField(uint classHash, uint fieldIndex)
        {
            if (classDict.TryGetValue(classHash, out var rszClass))
            {
                if (fieldIndex >= 0 && fieldIndex < rszClass.fields.Length)
                {
                    return rszClass.fields[fieldIndex];
                }
            }
            return null;
        }

        public int GetFieldAlignment(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.align ?? -1;
        }

        public bool GetFieldArrayState(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.array ?? false;
        }

        public string GetFieldName(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.name ?? "not found";
        }

        public string GetFieldTypeName(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.type ?? "not found";
        }

        public string GetFieldOrgTypeName(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.original_type ?? "not found";
        }

        public int GetFieldSize(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.size ?? -1;
        }

        public TypeIDs GetFieldType(uint classHash, uint fieldIndex)
        {
            if (classDict.TryGetValue(classHash, out var rszClass))
            {
                if (fieldIndex >= 0 && fieldIndex < rszClass.fields.Length)
                {
                    return GetFieldTypeInternal(rszClass.fields[fieldIndex].type);
                }
                return TypeIDs.out_of_range;
            }
            return TypeIDs.class_not_found;
        }

        public bool IsClassNative(uint classHash)
        {
            return classDict.TryGetValue(classHash, out var rszClass) && rszClass.native;
        }

        public bool IsFieldNative(uint classHash, uint fieldIndex)
        {
            return GetField(classHash, fieldIndex)?.native ?? false;
        }
    }

    public class RszClass
    {
        [JsonConverter(typeof(HexUIntJsonConverter))]
        public uint crc { get; set; }
        public string name { get; set; } = "";
        public bool native { get; set; }
        public RszField[] fields { get; set; } = EmptyFiedls;

        public static RszField[] EmptyFiedls = Array.Empty<RszField>();
    }

    public class RszField
    {
        public string name { get; set; } = "";
        public int align { get; set; }
        public int size { get; set; }
        public bool array { get; set; }
        public bool native { get; set; }
        public string type { get; set; } = "";
        public string original_type { get; set; } = "";
    }

    public class HexUIntJsonConverter : JsonConverter<uint>
    {
        public override uint Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? text = reader.GetString();
                if (text == null) return default;
                return Convert.ToUInt32(text, 16);
            }

            return reader.GetUInt32();
        }

        public override void Write(Utf8JsonWriter writer, uint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"0x{value:x}");
        }
    }

    public class HexIntJsonConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? text = reader.GetString();
                if (text == null) return default;
                return Convert.ToInt32(text, 16);
            }

            return reader.GetInt32();
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"0x{value:x}");
        }
    }

    public class HexIntPtrJsonConverter : JsonConverter<nint>
    {
        public override nint Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? text = reader.GetString();
                if (text == null) return default;
                return (nint)Convert.ToInt64(text, 16);
            }

            return (nint)reader.GetInt64();
        }

        public override void Write(Utf8JsonWriter writer, nint value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"0x{value:x}");
        }
    }

    public class ConstObjectConverter : JsonConverter<object?>
    {
        public override object? Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetDecimal();
            }
            return null;
        }

        public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options)
        {
            if (value is string stringValue)
            {
                writer.WriteStringValue(stringValue);
            }
            else if (value is IConvertible convertible)
            {
                writer.WriteNumberValue(convertible.ToDecimal(null));
            }
        }
    }
}
