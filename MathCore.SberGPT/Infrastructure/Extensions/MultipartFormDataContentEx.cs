namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class MultipartFormDataContentEx
{
    public static MultipartFormDataContent WithFile(this MultipartFormDataContent content, string FileName, Stream FileStream)
    {
        var stream_content = new StreamContent(FileStream)
        {
            Headers =
            {
                ContentType = new("text/plain"),
                ContentDisposition = new("form-data") { Name = "\"file\"", FileName = $"\"{FileName}\"" }
            }
        };

        content.Add(stream_content);

        return content;
    }

    public static MultipartFormDataContent WithString(this MultipartFormDataContent content, string Name, string Str)
    {
        var stream_content = new StringContent(Str)
        {
            Headers =
            {
                ContentType = null,
                ContentDisposition = new("form-data") { Name = $"\"{Name.Trim('"')}\"" }
            }
        };

        content.Add(stream_content);

        return content;
    }

    public static MultipartFormDataContent CheckBoundary(this MultipartFormDataContent content)
    {
        var content_type_parameter = content.Headers.ContentType!.Parameters.First();
        var old_boundary = content_type_parameter.Value!;
        content_type_parameter.Value = old_boundary.Trim('"');

        return content;
    }

}
