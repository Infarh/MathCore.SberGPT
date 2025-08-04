using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация об удалении файла</summary>
internal readonly record struct FileDeleteInfo(
    [property: JsonPropertyName("id")] Guid Id
    , [property: JsonPropertyName("deleted")] bool Deleted
    , [property: JsonPropertyName("access_policy")] FileAccessPolicy? Access
    );