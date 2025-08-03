using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Сообщение запроса к модели</summary>
/// <param name="Requests">Массив элементов запроса</param>
/// <param name="Model">Тип модели</param>
/// <param name="FunctionCall">
/// Определяет режим вызова функций: none, auto<br/>
/// - <b>none</b> - запрет на вызов любых функций<br/>
/// - <b>auto</b> - вызов внутренних функций и, если указано поле functions, то вызов пользовательских функций<br/>
/// - объект {"name": "имя_функции"}<br/>
/// </param>
/// <param name="FunctionSchemes">Список схем функций, доступных модели для вызова</param>
/// <param name="Streaming">Использовать потоковую передачу (по умолчанию false)</param>
/// <param name="UpdateInterval">Интервал в секундах отправки результатов при потоковой передаче</param>
/// <param name="Temperature">Величина температуры. Должна быть больше 0. Чем выше значение, тем более случайным будет ответ.</param>
/// <param name="TemperatureAlternative">Альтернативное значение температуры. Значение от 0 до 1. Определяет процент используемых токенов запроса.</param>
/// <param name="MaxTokensCount">Максимальное количество токенов, которые будут использованы для создания ответов</param>
/// <param name="RepetitionPenalty">Количество повторений слов. По умолчанию 1.0. При значении больше 1 модель будет стараться не повторять слова.</param>
internal readonly record struct ModelRequest(
    [property: JsonPropertyName("messages")] Request[] Requests,
    [property: JsonPropertyName("model")] string Model = "GigaChat-2",
    [property: JsonPropertyName("function_call")] string? FunctionCall = "auto",
    [property: JsonPropertyName("functions")] IEnumerable<JsonNode>? FunctionSchemes = null,
    [property: JsonPropertyName("temperature")] double? Temperature = null,
    [property: JsonPropertyName("top_p")] double? TemperatureAlternative = null,
    [property: JsonPropertyName("stream")] bool? Streaming = null,
    [property: JsonPropertyName("max_tokens")] int? MaxTokensCount = null,
    [property: JsonPropertyName("repetition_penalty")] double? RepetitionPenalty = null,
    [property: JsonPropertyName("update_interval")] int? UpdateInterval = null
);