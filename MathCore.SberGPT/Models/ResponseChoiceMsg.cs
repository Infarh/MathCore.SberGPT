using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Сгенерированное сообщение</summary>
/// <param name="Role">
/// Роль автора сообщения. Возможные значения: assistant, function_in_progress<br/>
/// Роль function_in_progress используется при работе встроенных функций в режиме потоковой передачи токенов.
/// </param>
/// <param name="Content">
/// Содержимое сообщения, например, результат генерации.<br/>
/// В сообщениях с ролью function_in_progress содержит информацию о том, сколько времени осталось до завершения работы встроенной функции.
/// </param>
public readonly record struct ResponseChoiceMsg(
    [property: JsonPropertyName("role")] string Role,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("function_call")] ResponseChoiceMsgFunc FunctionCall,
    [property: JsonPropertyName("functions_state_id")] Guid FunctionsStateId
);