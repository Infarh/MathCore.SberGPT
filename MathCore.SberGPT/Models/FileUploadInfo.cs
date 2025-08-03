using System.Text.Json.Serialization;

namespace MathCore.SberGPT.Models;

/// <summary>Информация о загружаемом файле</summary>
public readonly record struct FileUploadInfo(
    [property: JsonPropertyName("file")] string FileName,
    [property: JsonPropertyName("purpose")] string Purpose
);