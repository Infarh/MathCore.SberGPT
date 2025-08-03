using System.Net.Http.Json;
using System.Text.Json;

namespace MathCore.SberGPT;

public partial class GptClient
{
    /// <summary>Генерация запроса к API с включением заголовков идентификаторов сессии и запроса</summary>
    /// <param name="method">Метод запроса</param>
    /// <param name="url">Адрес</param>
    /// <param name="content">Содержимое запроса</param>
    /// <returns>Сформированный объект запроса</returns>
    protected virtual HttpRequestMessage GetRequestMessage(HttpMethod method, string? url, HttpContent? content)
    {
        var msg = new HttpRequestMessage(method, url)
            .WithContent(content)
            .WithSessionId(SessionId)
            .WithRequestId(RequestId);
        return msg;
    }

    /// <summary>Генерация запроса к API с включением заголовков идентификаторов сессии и запроса и содержимого в формате JSON</summary>
    /// <typeparam name="T">Тип объекта, включаемого в содержимое запроса в JSON-виде</typeparam>
    /// <param name="method">Метод запроса</param>
    /// <param name="url">Адрес</param>
    /// <param name="obj">Содержимое запроса</param>
    /// <param name="options">Параметры сериализации JSON</param>
    /// <returns>Сформированный объект запроса</returns>
    protected virtual HttpRequestMessage GetRequestMessageJson<T>(HttpMethod method, string url, T obj, JsonSerializerOptions? options = null)
    {
        var json_content = JsonContent.Create(obj, options: options);
        return GetRequestMessage(method, url, json_content);
    }
}
