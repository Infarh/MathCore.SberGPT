using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Ответ модели</summary>
/// <param name="Message">Сгенерированное сообщение</param>
/// <param name="Index">Индекс сообщения в массиве начиная с ноля</param>
/// <param name="FinishReason">
/// Причина завершения гипотезы. Возможные значения:<br/>
/// stop — модель закончила формировать гипотезу и вернула полный ответ;<br/>
/// length — достигнут лимит токенов в сообщении;<br/>
/// function_call — указывает что при запросе была вызвана встроенная функция или сгенерированы аргументы для пользовательской функции;<br/>
/// blacklist — запрос подпадает под тематические ограничения.
/// </param>
public readonly record struct ResponseChoice(
    [property: JsonPropertyName("message")] ResponseChoiceMsg Message,
    [property: JsonPropertyName("index")] int Index,
    [property: JsonPropertyName("finish_reason")] string FinishReason
)
{
    /// <summary>Расшифровка информации о причине завершения запроса</summary>
    [JsonIgnore]
    public string FinishReasonInfo => FinishReason switch
    {
        "stop" => "Модель закончила формировать гипотезу и вернула полный ответ",
        "length" => "Достигнут лимит токенов в сообщении",
        "function_call" => "Указывает что при запросе была вызвана встроенная функция или сгенерированы аргументы для пользовательской функции",
        "blacklist" => "Запрос подпадает под тематические ограничения",
        _ => throw new ArgumentOutOfRangeException(nameof(FinishReason), FinishReason, "Значение должно быть одним из: stop, length, function_call, blacklist")
    };
}