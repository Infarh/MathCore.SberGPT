using System.Text.Json.Serialization;

using MathCore.SberGPT.Models;

namespace MathCore.SberGPT.Infrastructure;

/// <summary>Контекст сериализации для GptClient с поддержкой source generation</summary>
[JsonSerializable(typeof(AccessToken))]
[JsonSerializable(typeof(ModelRequest))]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Response))]
[JsonSerializable(typeof(StreamingResponse))]
[JsonSerializable(typeof(EmbeddingRequest))]
[JsonSerializable(typeof(EmbeddingResponse))]
[JsonSerializable(typeof(ValidationFunctionResult))]
[JsonSerializable(typeof(GetFilesResponse))]
[JsonSerializable(typeof(FileDeleteInfo))]
[JsonSerializable(typeof(BalanceInfo))]
[JsonSerializable(typeof(BalanceInfo.Value))]
[JsonSerializable(typeof(CheckTextToAIGenerationRequest))]
[JsonSerializable(typeof(CheckTextToAIGenerationResponse))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(RequestRole))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
internal partial class GptClientJsonSerializationContext : JsonSerializerContext;