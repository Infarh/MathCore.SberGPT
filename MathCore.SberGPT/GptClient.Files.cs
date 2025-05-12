using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public partial class GptClient
{
    /// <summary>Информация о файле</summary>
    /// <param name="Id">Идентификатор</param>
    /// <param name="Name">Имя файла</param>
    /// <param name="Type">Тип файла</param>
    /// <param name="Length">Размер файла в байтах</param>
    /// <param name="Purpose">Назначение (general)</param>
    /// <param name="AccessPolicy">Доступность файла (public|private)</param>
    public readonly record struct FileInfo(
        [property: JsonPropertyName("id")] Guid Id
        , [property: JsonPropertyName("filename")] string Name
        , [property: JsonPropertyName("object")] string Type
        , [property: JsonPropertyName("bytes"), JsonConverter(typeof(UnixDateTimeConverter))] string Length
        , [property: JsonPropertyName("purpose")] string Purpose
        , [property: JsonPropertyName("access_policy")] AccessPolicy AccessPolicy
    );

    public readonly record struct FileUploadInfo(
        [property: JsonPropertyName("file")] string FileName,
        [property: JsonPropertyName("purpose")] string Purpose
    );

    public readonly record struct FileDeleteInfo(
        [property: JsonPropertyName("id")] Guid Id
        , [property: JsonPropertyName("deleted")] bool Deleted
        , [property: JsonPropertyName("access_policy")] AccessPolicy AccessPolicy
    );

    /// <summary>Режим доступа</summary>
    public enum AccessPolicy { Private, Public }

    /// <summary>Получить список доступных файлов</summary>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Список файлов</returns>
    public async Task<FileInfo[]> GetFilesAsync(CancellationToken Cancel = default)
    {
        const string url = "files";

        _Log.LogInformation("Запрос состояния хранилища файлов...");
        var response = await _Http.GetAsync(url, Cancel).ConfigureAwait(false);

        var files = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<FileInfo[]>(__DefaultOptions, Cancel)
            .ConfigureAwait(false);

        if (files is null)
            throw new InvalidOperationException("Не удалось получить список файлов");

        _Log.LogInformation("Получено файлов {FilesCount}", files.Length);

        return files;
    }

    /// <summary>Загрузить файл в хранилище</summary>
    /// <param name="FileName">Имя загружаемого файла</param>
    /// <param name="FileStream">Поток байт файла</param>
    /// <param name="Purpose">Назначение (по умолчанию должно быть general)</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Информация о загруженном файле</returns>
    public async Task<FileInfo> UploadFileAsync(
        string FileName,
        Stream FileStream,
        string Purpose = "general",
        CancellationToken Cancel = default)
    {
        const string url = "files";
        _Log.LogInformation("Загрузка файла {FileName} в хранилище...", FileName);

        var content = new MultipartFormDataContent()
        {
            {new StreamContent(FileStream), "file", FileName},
            {new StringContent(Purpose), "purpose"},
        };

        var response = await _Http.PostAsJsonAsync(url, content, Cancel).ConfigureAwait(false);

        var file_info = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<FileInfo>(__DefaultOptions, Cancel)
            .ConfigureAwait(false);

        if (file_info is not { Name: { Length: > 0 } })
            throw new InvalidOperationException("Не удалось загрузить файл");

        _Log.LogInformation("Файл {FileName} загружен", FileName);

        return file_info;
    }

    public async Task<FileInfo> GetFileInfoAsync(Guid Id, CancellationToken Cancel = default)
    {
        const string url = "files";
        var url_address = $"{url}/{Id}";
        _Log.LogInformation("Запрос информации о файле {Id}", Id);

        var response = await _Http.GetAsync(url_address, Cancel).ConfigureAwait(false);

        var file_info = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<FileInfo>(__DefaultOptions, Cancel)
            .ConfigureAwait(false);

        if (file_info is not { Name: { Length: > 0 } })
            throw new InvalidOperationException("Не удалось получить информацию о файле");

        _Log.LogInformation("Получена информация о файле {Id}", Id);

        return file_info;
    }

    public async Task DownloadFileAsync(
        Guid FileId,
        Stream DataStream,
        CancellationToken Cancel = default)
    {
        if (!DataStream.CanWrite)
            throw new ArgumentException("Поток не поддерживает запись", nameof(DataStream));

        const string url = "files";
        var url_address = $"{url}/{FileId}/content";

        var request = await _Http.GetAsync(url_address, Cancel).ConfigureAwait(false);

        switch (request.StatusCode)
        {
            case HttpStatusCode.OK: // 200
                break;

            case HttpStatusCode.BadRequest: // 400
                throw new ArgumentException($"Ошибка при загрузке файла {FileId}: {request.ReasonPhrase}", nameof(FileId));

            case HttpStatusCode.Unauthorized: // 401
                throw new UnauthorizedAccessException($"Ошибка при загрузке файла {FileId}: {request.ReasonPhrase}");

            case HttpStatusCode.NotFound: // 404
                throw new FileNotFoundException($"Файл {FileId} не найден", request.ReasonPhrase);

            default:
                throw new HttpRequestException($"Ошибка при загрузке файла {FileId}: {request.ReasonPhrase}");
        }

        _Log.LogInformation("Загрузка файла {Id} в поток", FileId);

        await using var response = await request.Content.ReadAsStreamAsync(Cancel).ConfigureAwait(false);
        await response.CopyToAsync(DataStream, Cancel).ConfigureAwait(false);
    }

    public async Task<FileDeleteInfo> DeleteFileAsync(Guid Id, CancellationToken Cancel = default)
    {
        const string url = "files";
        var url_address = $"{url}/{Id}";

        _Log.LogInformation("Удаление файла {Id}", Id);

        var response = await _Http.DeleteAsync(url_address, Cancel).ConfigureAwait(false);

        var file_info = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<FileDeleteInfo>(__DefaultOptions, Cancel)
            .ConfigureAwait(false);

        // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
        if (file_info is { Deleted: true })
            _Log.LogInformation("Файл {Id} удален", Id);
        else
            _Log.LogInformation("Файл {Id} не удален", Id);


        return file_info;
    }
}