using System.Runtime.CompilerServices;

using MathCore.SberGPT.Models;

using Microsoft.Extensions.AI;

using Embedding = Microsoft.Extensions.AI.Embedding<double>;
using Embeddings = Microsoft.Extensions.AI.GeneratedEmbeddings<Microsoft.Extensions.AI.Embedding<double>>;

namespace MathCore.SberGPT;

public partial class GptClient : IChatClient, IEmbeddingGenerator<string, Embedding>
{
    #region IChatClient Implementation

    //public ChatClientMetadata Metadata { get; } = new("GigaChat", new Uri("https://developers.sber.ru/"));

    /// <inheritdoc/>
    async Task<ChatResponse> IChatClient.GetResponseAsync(
        IEnumerable<ChatMessage> Messages,
        ChatOptions? options,
        CancellationToken Cancel)
    {
        var message_list = Messages.ToList();
        var messages = ConvertToRequestMessages(message_list);
        var model_name = options?.ModelId ?? "GigaChat-2";

        var system_prompt = message_list.FirstOrDefault(m => m.Role == ChatRole.System)?.Text;
        var user_request = string.Join(Environment.NewLine, message_list.Where(m => m.Role == ChatRole.User).Select(m => m.Text));

        var response = await RequestAsync(
            SystemPrompt: system_prompt,
            UserPrompt: user_request,
            ChatHistory: messages.Where(m => m.Role != RequestRole.system).SkipLast(1),
            ModelName: model_name,
            Cancel: Cancel
        ).ConfigureAwait(false);

        return ConvertToChatResponse(response, model_name);
    }

    /// <inheritdoc/>
    async IAsyncEnumerable<ChatResponseUpdate> IChatClient.GetStreamingResponseAsync(
        IEnumerable<ChatMessage> Messages,
        ChatOptions? options,
        [EnumeratorCancellation] CancellationToken Cancel)
    {
        var message_list = Messages.ToList();
        var messages = ConvertToRequestMessages(message_list);
        var model_name = options?.ModelId ?? "GigaChat-2";

        var system_prompt = message_list.FirstOrDefault(m => m.Role == ChatRole.System)?.Text;
        var user_request = string.Join(Environment.NewLine, message_list.Where(m => m.Role == ChatRole.User).Select(m => m.Text));

        var request = RequestStreamingAsync(
            SystemPrompt: system_prompt,
            UserPrompt: user_request,
            ChatHistory: messages.Where(m => m.Role != RequestRole.system).SkipLast(1),
            ModelName: model_name,
            Cancel: Cancel);

        await foreach (var streaming_response in request.ConfigureAwait(false))
            foreach (var choice in streaming_response.Choices)
                yield return ConvertToStreamingChatResponse(choice, streaming_response, model_name);

    }

    object? IChatClient.GetService(Type Service, object? Key) => Service.IsInstanceOfType(this) ? this : null;

    /// <inheritdoc/>
    void IDisposable.Dispose()
    {
        //GC.SuppressFinalize(this);
    }

    #endregion

    #region IEmbeddingGenerator Implementation

    //EmbeddingGeneratorMetadata IEmbeddingGenerator.Metadata { get; } = new("GigaChat-Embeddings");

    /// <inheritdoc/>
    async Task<Embeddings> IEmbeddingGenerator<string, Embedding>.GenerateAsync(
        IEnumerable<string> Values,
        EmbeddingGenerationOptions? options,
        CancellationToken Cancel)
    {
        var model = options?.ModelId switch
        {
            "EmbeddingsGigaR" => EmbeddingModel.EmbeddingsGigaR,
            _ => EmbeddingModel.Embeddings
        };

        var response = await GetEmbeddingsAsync(Values, model, Cancel).ConfigureAwait(false);

        var embeddings = response.Values.Select(e => new Embedding(e.Embedding));

        var usage_tokens = response.TokensUsage;
        return new(embeddings) { Usage = new() { TotalTokenCount = usage_tokens } };
    }

    /// <inheritdoc/>
    object? IEmbeddingGenerator.GetService(Type Service, object? Key) => Service.IsInstanceOfType(this) ? this : null;

    #endregion

    #region Helper Methods

    /// <summary>Конвертирует список ChatMessage в последовательность Request</summary>
    private static IEnumerable<Request> ConvertToRequestMessages(IList<ChatMessage> messages) =>
        messages.Select(msg => msg.Role.Value switch
        {
            "system" => Request.System(msg.Text),
            "user" => Request.User(msg.Text),
            "assistant" => Request.Assistant(msg.Text),
            "function" => Request.Function(msg.Text),
            _ => Request.User(msg.Text)
        });

    /// <summary>Конвертирует Response в ChatResponse</summary>
    private static ChatResponse ConvertToChatResponse(Response response, string Model) => new([
        new ChatMessage(ChatRole.Assistant, response.AssistMessage)
    ])
    {
        ModelId = Model,
        CreatedAt = response.CreateTime,
        Usage = new()
        {
            InputTokenCount = response.Usage.PromptTokens,
            OutputTokenCount = response.Usage.CompletionTokens,
            TotalTokenCount = response.Usage.TotalTokens
        }
    };

    /// <summary>Конвертирует StreamingResponse в StreamingChatResponse</summary>
    private static ChatResponseUpdate ConvertToStreamingChatResponse(
        StreamingResponse.Choice Choice,
        StreamingResponse response,
        string Model)
        => new(new(Choice.Delta.Role), Choice.Delta.Content)
        {
            ModelId = Model,
            CreatedAt = response.CreatedAt,
        };

    #endregion
}
