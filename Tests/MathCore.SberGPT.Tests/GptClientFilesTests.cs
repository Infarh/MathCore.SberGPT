using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using MathCore.SberGPT.Models;

namespace MathCore.SberGPT.Tests;

[TestClass]
public class GptClientFilesTests
{
    private class TestHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> Response) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken Cancel) => await Response(request);
    }

    private static HttpClient GetTestClient(Func<HttpRequestMessage, Task<HttpResponseMessage>> Response) => new(new TestHandler(Response))
    {
        BaseAddress = new("http://localhost"),
    };

    [TestMethod]
    public async Task TestFileUpload()
    {
        //const string file_path = "path/to/test/file.txt";
        using var stream = new MemoryStream("Hello World!"u8.ToArray());

        var request_str = default(string);
        var http = GetTestClient(async request =>
        {
            var method_str = $"{request.Method} {request.RequestUri?.LocalPath ?? "/"} HTTP/{request.Version}";

            string content = null!;
            if (request.Content != null)
                content = await request.Content.ReadAsStringAsync();

            var request_str_builder = new StringBuilder();
            request_str_builder.AppendLine(method_str);
            request_str_builder.AppendLine($"Host:{request.RequestUri?.Host}");
            request_str_builder.AppendJoin(Environment.NewLine, request.Headers.Select(h => $"{h.Key}: {string.Join(", ", h.Value)}")).AppendLine();
            request_str_builder.AppendLine($"Content-Length: {Encoding.UTF8.GetByteCount(content)}");

            request_str_builder.Append(Environment.NewLine);
            request_str_builder.Append(Environment.NewLine);
            request_str_builder.AppendLine(content);

            request_str = request_str_builder.ToString();

            var file = new FileDescription(Guid.NewGuid(), "123.txt", DateTime.UtcNow, "test", 123, "general", FileAccessPolicy.Public);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(file))
            };

            return response;
        });

        const string token = "test_token";

        var request = new HttpRequestMessage(HttpMethod.Post, "https://gigachat.devices.sberbank.ru/api/v1/files");
        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.Authorization = new("Bearer", token);
        request.Headers.UserAgent.ParseAdd("MathCore.SberGPT/1.0");
        //request.Content = new MultipartFormDataContent
        //{
        //    { new StreamContent(stream), "file", "TestFile.txt" },
        //    { new StringContent("general"), "purpose" }
        //};

        request.Content = new MultipartFormDataContent
        {
            new StreamContent(stream)
            {
                Headers =
                {
                    ContentType = new("text/plain"),
                    ContentDisposition = new("form-data")
                    {
                        Name = "\"file\"",
                        FileName = "\"TestFile.txt\""
                    }
                }
            },
            new StringContent("general", Encoding.UTF8)
            {
                Headers =
                {
                    ContentType = null,
                    ContentDisposition = new("form-data")
                    {
                        Name = "\"purpose\""
                    }
                }
            }
        };

        //const string expected_response_str =
        //    """
        //    POST /api/v1/files HTTP/1.1
        //    Host: gigachat.devices.sberbank.ru
        //    User-Agent: MathCore.SberGPT/1.0
        //    Accept: */*
        //    Authorization: Bearer test_token
        //    Content-Length: 323


        //    Content-Type: multipart/form-data; boundary=------------------------dnJ8r4YHk7GTEbk4rVrsPb

        //    --------------------------dnJ8r4YHk7GTEbk4rVrsPb
        //    Content-Disposition: form-data; name="file"; filename="TestFile.txt"
        //    Content-Type: text/plain

        //    Hello World!
        //    --------------------------dnJ8r4YHk7GTEbk4rVrsPb
        //    Content-Disposition: form-data; name="purpose"

        //    general
        //    --------------------------dnJ8r4YHk7GTEbk4rVrsPb--
        //    """;

        using var response = await http.SendAsync(request);
        var response_body = await response.Content.ReadAsStringAsync();

        var file_name = "test.txt";
        ContentDispositionHeaderValue content_disposition = new("form-data")
        {
            Name = "\"file\"",
            FileName = $"\"{file_name}\""
        };

        var ss = content_disposition.ToString();

        Console.WriteLine(request_str);
        //Assert.IsNotNull(result);
    }
}
