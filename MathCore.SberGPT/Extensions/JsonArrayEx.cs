using System.Text.Json.Nodes;

namespace MathCore.SberGPT.Extensions;

/// <summary>Статический класс-расширение для JsonArray.</summary>
internal static class JsonArrayEx
{
    /// <summary>Добавляет элементы из коллекции в JsonArray.</summary>
    public static JsonArray AddRange<T>(this JsonArray array, IEnumerable<T> items)
    {
        foreach (var item in items)
            array.Add(item);
        return array;
    }
}
