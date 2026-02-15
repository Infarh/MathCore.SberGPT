namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class AsyncEnumerableEx
{
    public static async IAsyncEnumerable<TResult> Select<T, TResult>(
        this IAsyncEnumerable<T> source,
        Func<T, TResult> selector)
    {
        await foreach (var item in source)
            yield return selector(item);
    }
}
