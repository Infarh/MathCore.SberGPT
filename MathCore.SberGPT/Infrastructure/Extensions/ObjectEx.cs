using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace MathCore.SberGPT.Infrastructure.Extensions;

/// <summary>Статический класс-расширение для проверки объектов на null.</summary>
internal static class ObjectEx
{
    /// <summary>Проверяет объект на null и выбрасывает исключение, если объект равен null.</summary>
    /// <typeparam name="T">Тип проверяемого объекта.</typeparam>
    /// <param name="obj">Проверяемый объект.</param>
    /// <param name="Message">Сообщение исключения (опционально).</param>
    /// <param name="ParameterName">Имя параметра (определяется автоматически).</param>
    /// <returns>Ссылка на объект, если он не равен null.</returns>
    /// <exception cref="InvalidOperationException">Если имя параметра не указано и объект равен null.</exception>
    /// <exception cref="ArgumentNullException">Если имя параметра указано и объект равен null.</exception>
    [return: NotNull]
    [return: NotNullIfNotNull(nameof(obj))]
    public static T NotNull<T>(this T? obj, string? Message = null, [CallerArgumentExpression(nameof(obj))] string? ParameterName = null!)
        where T : class =>
        obj ?? throw (ParameterName is null
            ? new InvalidOperationException(Message ?? "Пустая ссылка на объект")
            : new ArgumentNullException(ParameterName, Message ?? "Пустая ссылка в значении параметра"));
}
