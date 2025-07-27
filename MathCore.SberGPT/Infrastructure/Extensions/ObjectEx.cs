using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class ObjectEx
{
    [return: NotNull]
    [return: NotNullIfNotNull(nameof(obj))]
    public static T NotNull<T>(this T? obj, string? Message = null, [CallerArgumentExpression(nameof(obj))] string? ParameterName = null!)
        where T : class =>
        obj ?? throw (ParameterName is null
            ? new InvalidOperationException(Message ?? "Пустая ссылка на объект")
            : new ArgumentNullException(ParameterName, Message ?? "Пустая ссылка в значении параметра"));
}
