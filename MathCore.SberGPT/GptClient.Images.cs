using System.Net.Http.Json;
using System.Text.RegularExpressions;

using MathCore.SberGPT.Models;

namespace MathCore.SberGPT;

public partial class GptClient
{
    #region Генерация изображения

    private static readonly Regex __GuidRegex = GetGuidRegex();

    [GeneratedRegex("[0-9a-f]{8}(?:-[0-9a-f]{4}){3}-[0-9a-f]{12}")]
    private static partial Regex GetGuidRegex();

    public async Task<Guid> GenerateImageAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        const string request_url = "chat/completions";

        ModelRequest message = new(Requests.ToArray(), Model, FunctionCall: "auto");

        var response = await Http.PostAsJsonAsync(request_url, message, JsonOptions, Cancel).ConfigureAwait(false);

        var response_message = await response.AsJsonAsync<Response>(JsonOptions, Cancel).ConfigureAwait(false);

        var content_str = response_message.Choices.First(c => c.Msg.Role == "assistant").Msg.Content;
        var guid = Guid.Parse(__GuidRegex.Match(content_str).ValueSpan);

        return guid;
    }

    private async ValueTask<Stream> GetImageDownloadStreamAsync(Guid id, CancellationToken Cancel)
    {
        var response = await Http.GetAsync($"files/{id}/content", Cancel).ConfigureAwait(false);
        return await response.AsStream(Cancel).ConfigureAwait(false);
    }

    public async Task<byte[]> DownloadImageById(Guid id, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);

        var result = new MemoryStream(new byte[stream.Length]);
        await stream.CopyToAsync(result, Cancel).ConfigureAwait(false);

        return result.ToArray();
    }

    public async Task DownloadImageById(Guid id, Func<Stream, Task> ProcessStream, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);
        await ProcessStream(stream).ConfigureAwait(false);
    }

    public async Task DownloadImageById(Guid id, Func<Stream, CancellationToken, Task> ProcessStream, CancellationToken Cancel = default)
    {
        await using var stream = await GetImageDownloadStreamAsync(id, Cancel);
        await ProcessStream(stream, Cancel).ConfigureAwait(false);
    }

    public async Task<byte[]> GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        return result;
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<byte[], Task> ProcessData,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        await ProcessData(result).ConfigureAwait(false);
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<byte[], CancellationToken, Task> ProcessData,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        var result = await DownloadImageById(guid, Cancel).ConfigureAwait(false);
        await ProcessData(result, Cancel).ConfigureAwait(false);
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<Stream, Task> ProcessStream,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        await DownloadImageById(guid, ProcessStream, Cancel).ConfigureAwait(false);
    }

    public async Task GenerateAndDownloadImageAsync(
        IEnumerable<Request> Requests,
        Func<Stream, CancellationToken, Task> ProcessStream,
        string Model = "GigaChat-2",
        CancellationToken Cancel = default)
    {
        var guid = await GenerateImageAsync(Requests, Model, Cancel).ConfigureAwait(false);
        await DownloadImageById(guid, ProcessStream, Cancel).ConfigureAwait(false);
    }

    #endregion
}
