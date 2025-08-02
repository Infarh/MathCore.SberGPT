namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class ExceptionEx
{
    public static TException WithData<TException>(this TException exception, string key, object? value)
        where TException : Exception
    {
        if (exception.Data.Contains(key))
            exception.Data[key] = value;
        else
            exception.Data.Add(key, value);
        return exception;
    }
}
