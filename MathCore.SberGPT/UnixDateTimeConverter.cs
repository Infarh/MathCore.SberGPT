using System.Text.Json;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT;

/// <summary>Конвертер для десериализации времени в формате Unix в структуру DateTime</summary>
internal class UnixDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Ожидалось числовое значение для Unix времени.");

        var unix_time = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(unix_time).UtcDateTime;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unix_time = new DateTimeOffset(value).ToUnixTimeSeconds();
        writer.WriteNumberValue(unix_time);
    }
}