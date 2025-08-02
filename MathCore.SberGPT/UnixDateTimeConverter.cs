using System.Text.Json;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT;

/// <summary>Конвертер для десериализации времени в формате Unix в структуру DateTime</summary>
internal class UnixDateTimeConverter : JsonConverter<DateTime>
{
    /// <summary>Преобразует значение Unix времени из JSON в <see cref="DateTime"/></summary>
    /// <param name="reader">Читатель JSON</param>
    /// <param name="type">Тип значения</param>
    /// <param name="options">Опции сериализации</param>
    /// <returns>Дата и время в формате <see cref="DateTime"/></returns>
    /// <exception cref="JsonException">Если значение не является числом</exception>
    public override DateTime Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.Number)
            throw new JsonException("Ожидалось числовое значение для Unix времени.");

        var unix_time = reader.GetInt64();
        return DateTimeOffset.FromUnixTimeSeconds(unix_time).UtcDateTime;
    }

    /// <summary>Записывает <see cref="DateTime"/> как Unix время в JSON</summary>
    /// <param name="writer">Писатель JSON</param>
    /// <param name="value">Значение даты и времени</param>
    /// <param name="options">Опции сериализации</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var unix_time = new DateTimeOffset(value).ToUnixTimeSeconds();
        writer.WriteNumberValue(unix_time);
    }
}