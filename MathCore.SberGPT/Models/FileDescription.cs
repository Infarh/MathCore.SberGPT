using System.Text.Json.Serialization;

using MathCore.SberGPT.Infrastructure;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о файле</summary>
/// <param name="Id">Идентификатор</param>
/// <param name="Name">Имя файла</param>
/// <param name="CreatedAt">Время создания файла в формате</param>
/// <param name="Type">Тип файла</param>
/// <param name="Length">Размер файла в байтах</param>
/// <param name="Purpose">Назначение (general)</param>
/// <param name="AccessPolicy">Доступность файла (public|private)</param>
public readonly record struct FileDescription(
    [property: JsonPropertyName("id")] Guid Id
    , [property: JsonPropertyName("filename")] string Name
    , [property: JsonPropertyName("created_at"), JsonConverter(typeof(UnixDateTimeConverter))] DateTime CreatedAt
    , [property: JsonPropertyName("object")] string Type
    , [property: JsonPropertyName("bytes")] int Length
    , [property: JsonPropertyName("purpose")] string Purpose
    , [property: JsonPropertyName("access_policy")] FileAccessPolicy AccessPolicy
    );