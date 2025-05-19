using System.Text.Json.Serialization;

namespace MathCore.SberGPT;

[JsonSerializable(typeof(AccessToken))]
[JsonSerializable(typeof(GptClient.ModelRequest))]
[JsonSerializable(typeof(GptClient.Request))]
[JsonSerializable(typeof(GptClient.ModelResponse))]
[JsonSerializable(typeof(GptClient.StreamingResponseMessage))]
[JsonSerializable(typeof(GptClient.FunctionInfo))]
[JsonSerializable(typeof(GptClient.FileInfo))]
[JsonSerializable(typeof(GptClient.BalanceInfo))]
[JsonSerializable(typeof(GptClient.BalanceValue))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(double))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(RequestRole))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    UseStringEnumConverter = true)]
internal partial class GptClientJsonSerializationContext : JsonSerializerContext;