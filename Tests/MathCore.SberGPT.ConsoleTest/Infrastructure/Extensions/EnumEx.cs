namespace MathCore.SberGPT.ConsoleTest.Infrastructure.Extensions;

internal static class EnumEx
{
    public static string ToSeparatedString<T>(this IEnumerable<T> items, string Separator = ", ") => string.Join(Separator, items);
}
