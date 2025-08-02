using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

using MathCore.SberGPT.Attributes;

namespace MathCore.SberGPT.Extensions;

/// <summary>Вспомогательные методы для работы с типами.</summary>
public static class TypeEx
{
    /// <summary>Формирует JSON-схему типа в формате OpenAPI (JSON Schema)</summary>
    public static JsonNode GetJsonScheme(this Type Type, bool SetNullable = false)
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
                not null when t == typeof(string) => "string",
                not null when t == typeof(bool) => "boolean",
                not null when t == typeof(byte) ||
                              t == typeof(sbyte) ||
                              t == typeof(short) ||
                              t == typeof(ushort) ||
                              t == typeof(int) ||
                              t == typeof(uint) ||
                              t == typeof(long) ||
                              t == typeof(ulong) => "integer",
                not null when t == typeof(float) ||
                              t == typeof(double) ||
                              t == typeof(decimal) => "number",
                not null when t == typeof(DateTime) => "string",
                not null when t == typeof(Guid) => "string",
                _ => null
            };

            switch (t)
            {
                case { IsEnum: true }:
                    {
                        var enum_array = new JsonArray();
                        foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Static))
                        {
                            var name = field.GetCustomAttribute<GPTAttribute>()?.Name ??
                                       field.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ??
                                       field.Name;
                            enum_array.Add(name);
                        }

                        //foreach (var name in Enum.GetNames(t))
                        //    enum_array.Add(name);

                        var node = new JsonObject
                        {
                            ["type"] = json_type,
                            ["enum"] = enum_array
                        };

                        if (SetNullable)
                            node["nullable"] = IsNullable;

                        return node;
                    }

                case { IsArray: true }:
                    {
                        var node = new JsonObject
                        {
                            ["type"] = "array",
                            ["items"] = BuildSchema(t.GetElementType()!)
                        };

                        if (SetNullable)
                            node["nullable"] = IsNullable;

                        return node;
                    }

                case { IsGenericType: true }:
                    {
                        var generic_type = t.GetGenericTypeDefinition();
                        var type_args = t.GetGenericArguments();

                        // Словари
                        if (generic_type == typeof(Dictionary<,>) || generic_type == typeof(IDictionary<,>))
                        {
                            var node = new JsonObject
                            {
                                ["type"] = "object",
                                ["additionalProperties"] = BuildSchema(type_args[1])
                            };

                            if (SetNullable)
                                node["nullable"] = IsNullable;

                            return node;
                        }

                        // Коллекции
                        if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                        {
                            var node = new JsonObject
                            {
                                ["type"] = "array",
                                ["items"] = BuildSchema(type_args[0])
                            };

                            if (SetNullable)
                                node["nullable"] = IsNullable;

                            return node;
                        }

                        break;
                    }
            }

            // Примитивные типы
            if (json_type != null)
            {
                var node = new JsonObject { ["type"] = json_type };
                if (IsNullable && SetNullable) node["nullable"] = true;
                if (t == typeof(DateTime)) node["format"] = "date-time";
                if (t == typeof(Guid)) node["format"] = "uuid";
                return node;
            }

            // Объекты (классы/структуры)
            if (!visited_types.Add(t.NotNull())) // Предотвращение рекурсии
                return new JsonObject
                {
                    ["type"] = "object",
                    ["ref"] = t.NotNull().Name
                };

            var properties = t.NotNull().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .ToArray();

            // Исправленная логика определения обязательных свойств
            var required_props = properties
                .Where(PropertyInfoEx.IsRequiredProperty)
                .Select(p => p.GetCustomAttribute<GPTAttribute>()?.Name ?? p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name)
                .ToArray();

            var props_obj = new JsonObject();
            foreach (var p in properties)
            {
                var property_name = p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? p.Name;
                var property_scheme = BuildSchema(p.PropertyType, IsNullableType(p.PropertyType));

                if ((string?)property_scheme["type"] == "array" &&
                    p.GetCustomAttribute<ItemsDescriptionAttribute>() is { Description: { Length: > 0 } item_description } &&
                    property_scheme["items"] is { } items)
                    items["description"] = item_description;

                var property_description = p.GetCustomAttribute<DescriptionAttribute>()?.Description;
                if (property_description is { Length: > 0 })
                    property_scheme["description"] = property_description;
                props_obj[property_name] = property_scheme;
            }

            var schema_obj = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = props_obj
            };

            if (SetNullable)
                schema_obj["nullable"] = IsNullable;

            if (required_props.Length == 0) return schema_obj;

            var required_array = new JsonArray();
            foreach (var n in required_props)
                required_array.Add(n);
            schema_obj["required"] = required_array;

            return schema_obj;
        }

        var root = BuildSchema(Type);
        return root;

        static bool IsNullableType(Type t) => !t.IsValueType || Nullable.GetUnderlyingType(t) != null;
    }

    /// <summary>Получает значение по умолчанию для типа</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static object? GetDefaultValue(this Type Type) => Type.IsValueType ? Activator.CreateInstance(Type) : null;

    /// <summary>Проверяет, является ли тип generic List</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGenericList(this Type Type) => Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(List<>);

    /// <summary>Проверяет, является ли тип generic IEnumerable</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsGenericEnumerable(this Type Type) => Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

}
