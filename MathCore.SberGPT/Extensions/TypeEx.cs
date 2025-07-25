using System.Reflection;
using System.Text.Json.Nodes;

namespace MathCore.SberGPT.Extensions;

public static class TypeEx
{
    /// <summary>Формирует JSON-схему типа в формате OpenAPI (JSON Schema)</summary>
    public static JsonNode GetJsonScheme(this Type Type)
    {
        var visited_types = new HashSet<Type>(); // Для предотвращения циклических ссылок

        // Рекурсивная функция построения схемы
        JsonNode BuildSchema(Type t, bool IsNullable = false)
        {
            // Обработка Nullable типов
            if (Nullable.GetUnderlyingType(t) is { } underlying_type)
                return BuildSchema(underlying_type, true); // Если тип поддерживает null, то строим схему для него в режиме null-совместимости

            // Определение JSON-типа для примитивных типов
            var json_type = t switch
            {
                { IsEnum: true } => "string",
                Type _ when t == typeof(string) => "string",
                Type _ when t == typeof(bool) => "boolean",
                Type _ when t == typeof(byte) ||
                            t == typeof(sbyte) ||
                            t == typeof(short) ||
                            t == typeof(ushort) ||
                            t == typeof(int) ||
                            t == typeof(uint) ||
                            t == typeof(long) ||
                            t == typeof(ulong) => "integer",
                Type _ when t == typeof(float) ||
                            t == typeof(double) ||
                            t == typeof(decimal) => "number",
                Type _ when t == typeof(DateTime) => "string",
                Type _ when t == typeof(Guid) => "string",
                _ => null
            };

            // Обработка перечислений
            if (t.IsEnum)
            {
                var enum_array = new JsonArray();
                foreach (var name in Enum.GetNames(t))
                    enum_array.Add(name);

                var node = new JsonObject
                {
                    ["type"] = json_type,
                    ["nullable"] = IsNullable,
                    ["enum"] = enum_array
                };
                return node;
            }

            // Обработка массивов
            if (t.IsArray)
            {
                var node = new JsonObject
                {
                    ["type"] = "array",
                    ["nullable"] = IsNullable,
                    ["items"] = BuildSchema(t.GetElementType()!)
                };
                return node;
            }

            // Обработка обобщённых типов
            if (t.IsGenericType)
            {
                var generic_type = t.GetGenericTypeDefinition();
                var type_args = t.GetGenericArguments();

                // Словари
                if (generic_type == typeof(Dictionary<,>) || generic_type == typeof(IDictionary<,>))
                {
                    var node = new JsonObject
                    {
                        ["type"] = "object",
                        ["nullable"] = IsNullable,
                        ["additionalProperties"] = BuildSchema(type_args[1])
                    };
                    return node;
                }

                // Коллекции
                if (t.GetInterfaces().Any(i => i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                {
                    var node = new JsonObject
                    {
                        ["type"] = "array",
                        ["nullable"] = IsNullable,
                        ["items"] = BuildSchema(type_args[0])
                    };
                    return node;
                }
            }

            // Примитивные типы
            if (json_type != null)
            {
                var node = new JsonObject { ["type"] = json_type };
                if (IsNullable) node["nullable"] = true;
                if (t == typeof(DateTime)) node["format"] = "date-time";
                if (t == typeof(Guid)) node["format"] = "uuid";
                return node;
            }

            // Объекты (классы/структуры)
            if (visited_types.Contains(t))
                return new JsonObject
                {
                    ["type"] = "object",
                    ["ref"] = t.Name // Предотвращение рекурсии
                };

            visited_types.Add(t);

            var properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            var required_props = properties
                .Where(p => !IsNullableType(p.PropertyType))
                .Select(p => p.Name)
                .ToArray();

            var props_obj = new JsonObject();
            foreach (var p in properties)
                props_obj[p.Name] = BuildSchema(p.PropertyType, IsNullableType(p.PropertyType));

            var schema_obj = new JsonObject
            {
                ["type"] = "object",
                ["nullable"] = IsNullable,
                ["properties"] = props_obj
            };

            if (required_props.Length > 0)
            {
                var required_array = new JsonArray();
                foreach (var n in required_props) required_array.Add(n);
                schema_obj["required"] = required_array;
            }

            return schema_obj;
        }

        // Проверка, является ли тип nullable
        static bool IsNullableType(Type t) =>
            !t.IsValueType || Nullable.GetUnderlyingType(t) != null;

        var root = BuildSchema(Type);
        return root;
    }
}
