using System.Text.Json.Nodes;

namespace MathCore.SberGPT.Extensions;

internal static class JsonArrayEx
{
    public static JsonArray AddRange<T>(this JsonArray array, IEnumerable<T> items)
    {
        foreach (var item in items)
            array.Add(item);
        return array;
    }
}
