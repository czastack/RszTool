using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using RszTool.Common;

namespace RszTool
{
    public class RszParser
    {
        private static readonly Dictionary<string, RszParser> instanceDict = new();

        public static RszParser GetInstance(string jsonPath)
        {
            if (!instanceDict.TryGetValue(jsonPath, out var rszParser))
            {
                rszParser = new RszParser(jsonPath);
                instanceDict[jsonPath] = rszParser;
            }
            return rszParser;
        }

        private readonly Dictionary<uint, RszClass> classDict;
        private readonly Dictionary<string, RszClass> classNameDict;

        public RszParser(string jsonPath)
        {
            classDict = new();
            classNameDict = new();
            using FileStream fileStream = File.OpenRead(jsonPath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, RszClass>>(fileStream);
            if (dict != null)
            {
                foreach (var item in dict)
                {
                    classDict[uint.Parse(item.Key, NumberStyles.HexNumber)] = item.Value;
                    classNameDict[item.Value.name] = item.Value;
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

        public RszClass? GetRSZClass(uint classHash)
        {
            return classDict.TryGetValue(classHash, out var rszClass) ? rszClass : null;
        }

        public RszClass? GetRSZClass(string className)
        {
            return classNameDict.TryGetValue(className, out var rszClass) ? rszClass : null;
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
            return GetField(classHash, fieldIndex)?.type.ToString() ?? "not found";
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
                    return rszClass.fields[fieldIndex].type;
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

        public int IndexOfField(string name)
        {
            for (int i = 0; i < fields.Length; i++)
            {
                if (fields[i].name == name)
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public class RszField
    {
        public string name { get; set; } = "";
        public int align { get; set; }
        public int size { get; set; }
        public bool array { get; set; }
        public bool native { get; set; }
        [JsonConverter(typeof(EnumJsonConverter<TypeIDs>))]
        public TypeIDs type { get; set; }
        public string original_type { get; set; } = "";
    }
}
