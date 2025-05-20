using System.Net.Http.Json;
using System.Text.Json;

namespace MathCore.SberGPT;

public partial class GptClient
{
    protected virtual HttpRequestMessage GetRequestMessage(HttpMethod method, string? url, HttpContent? content)
    {
        var msg = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        if (SessionId is { Length: > 0 } session_id)
            msg.Headers.Add(SessionXIdHeader, session_id);

        if (RequestId is { Length: > 0 } request_id)
            msg.Headers.Add(RequestXIdHeader, request_id);

        return msg;
    }

    protected virtual HttpRequestMessage GetRequestMessageJson<T>(HttpMethod method, string url, T obj, JsonSerializerOptions? options = null)
    {
        var json_content = JsonContent.Create(obj, options: options);
        return GetRequestMessage(method, url, json_content);
    }
}
