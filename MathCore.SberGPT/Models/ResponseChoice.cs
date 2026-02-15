using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Ответ модели</summary>
/// <param name="Msg">Сгенерированное сообщение</param>
/// <param name="Index">Индекс сообщения в массиве начиная с ноля</param>
/// <param name="FinishReason">
/// Причина завершения гипотезы. Возможные значения:<br/>
/// stop — модель закончила формировать гипотезу и вернула полный ответ;<br/>
/// length — достигнут лимит токенов в сообщении;<br/>
/// function_call — указывает что при запросе была вызвана встроенная функция или сгенерированы аргументы для пользовательской функции;<br/>
/// blacklist — запрос подпадает под тематические ограничения.
/// </param>
public readonly record struct ResponseChoice(
    [property: JsonPropertyName("message")] ResponseChoice.Message Msg
    , [property: JsonPropertyName("index")] int Index
    , [property: JsonPropertyName("finish_reason")] string FinishReason
    )
{
    /// <summary>Сгенерированное сообщение</summary>
    /// <param name="Role">
    /// Роль автора сообщения. Возможные значения: assistant, function_in_progress<br/>
    /// Роль function_in_progress используется при работе встроенных функций в режиме потоковой передачи токенов.
    /// </param>
    /// <param name="Content">
    /// Содержимое сообщения, например, результат генерации.<br/>
    /// В сообщениях с ролью function_in_progress содержит информацию о том, сколько времени осталось до завершения работы встроенной функции.
    /// </param>
    public readonly record struct Message(
        [property: JsonPropertyName("role")] string Role
        , [property: JsonPropertyName("content")] string Content
        , [property: JsonPropertyName("function_call")] Message.FuncInfo FunctionCall
        , [property: JsonPropertyName("functions_state_id")] Guid FunctionsStateId
        )
    {
        /// <summary>Информация о вызове функции</summary>
        /// <param name="Name">Название функции</param>
        /// <param name="Arguments">Перечень аргументов функции с их значениями</param>
        public readonly record struct FuncInfo(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("arguments")] JsonObject Arguments
            );
    }

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