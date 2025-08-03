using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Сообщение пользователя модели</summary>
/// <param name="Content">Текст сообщения</param>
/// <param name="Role">
/// Тип рои: system, user, assistant, function<br/>
/// - system — системный промт, который задает роль модели, например, должна модель отвечать как академик или как школьник;<br/>
/// - assistant — ответ модели;<br/>
/// - user — сообщение пользователя;<br/>
/// - function — сообщение с результатом работы пользовательской функции. В сообщении с этой ролью передавайте в поле content валидный JSON-объект с результатами работы функции.
/// </param>
/// <param name="FunctionStateId">Идентификатор вызова функции</param>
/// <param name="FunctionCall">Вызов функции</param>
/// <param name="Name">Название</param>
public readonly record struct Request(
    [property: JsonPropertyName("content"), JsonPropertyOrder(1)] string Content,
    [property: JsonPropertyName("role"), JsonPropertyOrder(0)] RequestRole Role = RequestRole.user,
    [property: JsonPropertyName("functions_state_id")] Guid? FunctionStateId = null,
    [property: JsonPropertyName("function_call")] ResponseChoiceMsgFunc? FunctionCall = null,
    [property: JsonPropertyName("name")] string? Name = null
)
{
    /// <summary>Создаёт запрос пользователя с текстовым содержимым</summary>
    /// <param name="Content">Текст сообщения пользователя</param>
    /// <returns>Объект запроса с ролью пользователя</returns>
    public static Request User(string Content) => new(Content);

    /// <summary>Создаёт запрос ассистента с текстовым содержимым</summary>
    /// <param name="Content">Текст ответа ассистента</param>
    /// <returns>Объект запроса с ролью ассистента</returns>
    public static Request Assistant(string Content) => new(Content, RequestRole.assistant);

    /// <summary>Создаёт системный запрос для задания роли модели</summary>
    /// <param name="Content">Текст системного промпта</param>
    /// <returns>Объект запроса с системной ролью</returns>
    public static Request System(string Content) => new(Content, RequestRole.system);

    /// <summary>Создаёт запрос с результатом работы функции</summary>
    /// <param name="Content">JSON-объект с результатами выполнения функции</param>
    /// <returns>Объект запроса с ролью функции</returns>
    public static Request Function(string Content) => new(Content, RequestRole.function);

    /// <summary>Оператор неявного преобразования кортежа, содержащего текст запроса и роль в объект запроса</summary>
    /// <param name="request">Объект запроса</param>
    public static implicit operator Request((string Content, RequestRole Role) request) => new(request.Content, request.Role);

    /// <summary>Неявное преобразование строки в объект запроса пользователя</summary>
    /// <param name="request">Текст запроса пользователя</param>
    /// <returns>Объект запроса с ролью пользователя</returns>
    public static implicit operator Request(string request) => new(request);
}