using System.Net.Http.Headers;

namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class HttpRequestMessageEx
{
    public static HttpRequestMessage WithSessionId(this HttpRequestMessage request, string? SessionId)
    {
        if (SessionId is { Length: > 0 })
            request.Headers.Add(GptClient.SessionXIdHeader, SessionId);
        return request;
    }

    public static HttpRequestMessage WithRequestId(this HttpRequestMessage request, string? RequestId)
    {
        if (RequestId is { Length: > 0 })
            request.Headers.Add(GptClient.RequestXIdHeader, RequestId);
        return request;
    }

    public static HttpRequestMessage WithContent(this HttpRequestMessage request, HttpContent? Content)
    {
        if (Content is not null)
            request.Content = Content;
        return request;
    }

    private const string __ApplicationJson = "application/json";

    public static HttpRequestMessage WithJson(this HttpRequestMessage request, string json)
    {
        var content = new StringContent(json, null, __ApplicationJson)
        {
            Headers = { ContentType = new(__ApplicationJson) }
        };

        request.Content = content;
        return request;
    }

    public static HttpRequestMessage WithAcceptApplicationJson(this HttpRequestMessage request)
    {
        request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse(__ApplicationJson));
        return request;
    }
}
