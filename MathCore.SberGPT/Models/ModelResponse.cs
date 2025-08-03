using System.Text;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Ответ модели</summary>
/// <param name="Choices">Ответы модели</param>
/// <param name="CreatedUnixTime">Время формирования ответа</param>
/// <param name="Model">Тип модели</param>
/// <param name="Usage">Данные об использовании модели</param>
/// <param name="CallMethodName">Название вызываемого метода</param>
public readonly record struct ModelResponse(
    [property: JsonPropertyName("choices")] IReadOnlyList<ResponseChoice> Choices,
    [property: JsonPropertyName("created")] int CreatedUnixTime,
    [property: JsonPropertyName("model")] string Model,
    [property: JsonPropertyName("usage")] ResponseUsage Usage,
    [property: JsonPropertyName("object")] string? CallMethodName
)
{
    ///// <summary>Время формирования ответа</summary>
    //[JsonIgnore]
    //public DateTimeOffset CreateTime => DateTimeOffset.UnixEpoch.AddSeconds(CreatedUnixTime / 1000d);

    /// <summary>Время формирования ответа</summary>
    [JsonIgnore]
    public DateTimeOffset CreateTime => DateTimeOffset.FromUnixTimeMilliseconds(CreatedUnixTime);

    public IEnumerable<string> AssistMessages => Choices
        .Where(c => c.Message.Role == "assistant")
        .Select(c => c.Message.Content);

    public string AssistMessage => AssistMessages.ToSeparatedStr(Environment.NewLine);

    public override string ToString()
    {
        const int max_length = 60;

        var assist_msg = new StringBuilder();
        foreach (var msg in AssistMessages)
        {
            assist_msg.AppendLine(msg);
            if (assist_msg.Length > max_length)
                break;
        }

        assist_msg.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');

        if (assist_msg.Length > max_length)
        {
            assist_msg.Length = max_length - 3;
            assist_msg.Append("...");
        }

        return $"assist: {assist_msg} tokens: {Usage.TotalTokens} ({Usage.PrecachedPromptTokens})";
    }

    public static implicit operator string(ModelResponse response) => response.AssistMessage;
}