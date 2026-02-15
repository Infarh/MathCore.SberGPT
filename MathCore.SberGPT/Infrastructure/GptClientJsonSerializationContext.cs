using System.Text.Json.Serialization;

using MathCore.SberGPT.Models;

namespace MathCore.SberGPT.Infrastructure;

/// <summary>Контекст сериализации для GptClient с поддержкой source generation</summary>
[JsonSerializable(typeof(AccessToken))]
[JsonSerializable(typeof(ModelRequest))]
[JsonSerializable(typeof(Request))]
[JsonSerializable(typeof(Response))]
[JsonSerializable(typeof(ResponseChoice))]
[JsonSerializable(typeof(ResponseChoice.Message))]
[JsonSerializable(typeof(ResponseChoice.Message.FuncInfo))]
[JsonSerializable(typeof(ResponseUsage))]
[JsonSerializable(typeof(StreamingResponse))]
[JsonSerializable(typeof(StreamingResponse.Choice))]
[JsonSerializable(typeof(StreamingResponse.Choice.ChoiceDelta))]
[JsonSerializable(typeof(EmbeddingRequest))]
[JsonSerializable(typeof(EmbeddingResponse))]
[JsonSerializable(typeof(EmbeddingResponse.Value), TypeInfoPropertyName = "EmbeddingResponseValue")]
[JsonSerializable(typeof(EmbeddingResponse.Value.UsageInfo))]
[JsonSerializable(typeof(IReadOnlyList<EmbeddingResponse.Value>), TypeInfoPropertyName = "IReadOnlyListEmbeddingResponseValue")]
[JsonSerializable(typeof(ValidationFunctionResult))]
[JsonSerializable(typeof(GetFilesResponse))]
[JsonSerializable(typeof(FileDeleteInfo))]
[JsonSerializable(typeof(BalanceInfo))]
[JsonSerializable(typeof(BalanceInfo.Value), TypeInfoPropertyName = "BalanceInfoValue")]
[JsonSerializable(typeof(IReadOnlyList<BalanceInfo.Value>), TypeInfoPropertyName = "IReadOnlyListBalanceInfoValue")]
[JsonSerializable(typeof(CheckTextToAIGenerationRequest))]
[JsonSerializable(typeof(CheckTextToAIGenerationResponse))]
[JsonSerializable(typeof(TokensCount), TypeInfoPropertyName = "TokensCountValue")]
[JsonSerializable(typeof(IReadOnlyList<TokensCount>), TypeInfoPropertyName = "IReadOnlyListTokensCount")]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(RequestRole))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
internal partial class GptClientJsonSerializationContext : JsonSerializerContext;