using System.Net;

using MathCore.SberGPT.Models;

using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public partial class GptClient
{
    /// <summary>Получить список доступных файлов</summary>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Список файлов</returns>
    public async Task<FileDescription[]> GetFilesAsync(CancellationToken Cancel = default)
    {
        const string url = "files";

        _Log.LogInformation("Запрос состояния хранилища файлов...");
        var response = await Http.GetAsync(url, Cancel).ConfigureAwait(false);

        try
        {
            var result = await response.AsJsonAsync<GetFilesResponse>(JsonOptions, Cancel).ConfigureAwait(false);

            if (result is not { Files: { Length: var files_count } files })
                throw new InvalidOperationException("Не удалось получить список файлов");

            _Log.LogInformation("Получено файлов {FilesCount}", files_count);

            return files;
        }
        catch (InvalidOperationException ex)
        {
            var content = await response.Content.ReadAsStringAsync(Cancel).ConfigureAwait(false);
            _Log.LogError(ex, "Ошибка при получении файлов: {Message}", content);

            throw;
        }
    }

    /// <summary>Загрузить файл в хранилище</summary>
    /// <param name="FileName">Имя загружаемого файла</param>
    /// <param name="FileStream">Поток байт файла</param>
    /// <param name="Purpose">Назначение (по умолчанию должно быть general)</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Информация о загруженном файле</returns>
    public async Task<FileDescription> UploadFileAsync(
        string FileName,
        Stream FileStream,
        string Purpose = "general",
        CancellationToken Cancel = default)
    {
        const string url = "files";

        ArgumentNullException.ThrowIfNull(FileName);
        ArgumentNullException.ThrowIfNull(FileStream);

        _Log.LogInformation("Загрузка файла {FileName} в хранилище...", FileName);

        var request = new MultipartFormDataContent()
            .WithFile(FileName, FileStream)
            .WithString(Name: "purpose", Str: Purpose)
            .CheckBoundary();

        var response = await Http.PostAsync(url, request, Cancel).ConfigureAwait(false);

        try
        {
            var file_info = await response.AsJsonAsync<FileDescription>(JsonOptions, Cancel).ConfigureAwait(false);

            if (file_info is not { Name: { Length: > 0 } })
                throw new InvalidOperationException("Не удалось загрузить файл");

            _Log.LogInformation("Файл {FileName} загружен", FileName);

            return file_info;
        }
        catch (HttpRequestException e)
        {
            var error_content = await response.Content.ReadAsStringAsync(Cancel).ConfigureAwait(false);
            _Log.LogError(e, "Upload file error {FileName}, {Purpose} status: {StatusCode} {ErrorServerResponse}",
                FileName, Purpose, e.StatusCode, error_content);

            throw new InvalidOperationException($"Ошибка ответа сервера:\r\n{error_content}", e)
                .WithData(nameof(e.Message), e.Message)
                .WithData(nameof(e.StatusCode), e.StatusCode)
                .WithData("ErrorContent", error_content);
        }
    }

    /// <summary>Получить информацию о файле</summary>
    /// <param name="Id">Идентификатор файла</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Информация о файле</returns>
    public async Task<FileDescription> GetFileInfoAsync(Guid Id, CancellationToken Cancel = default)
    {
        const string url = "files";
        var url_address = $"{url}/{Id}";
        _Log.LogInformation("Запрос информации о файле {Id}", Id);

        var response = await Http.GetAsync(url_address, Cancel).ConfigureAwait(false);

        var file_info = await response.AsJsonAsync<FileDescription>(JsonOptions, Cancel).ConfigureAwait(false);

        if (file_info is not { Name.Length: > 0 })
            throw new InvalidOperationException("Не удалось получить информацию о файле");

        _Log.LogInformation("Получена информация о файле {Id}", Id);

        return file_info;
    }

    /// <summary>Скачать файл в поток</summary>
    /// <param name="FileId">Идентификатор файла</param>
    /// <param name="DataStream">Поток для записи данных</param>
    /// <param name="Cancel">Отмена операции</param>
    public async Task DownloadFileAsync(
        Guid FileId,
        Stream DataStream,
        CancellationToken Cancel = default)
    {
        if (!DataStream.CanWrite)
            throw new ArgumentException("Поток не поддерживает запись", nameof(DataStream));

        const string url = "files";
        var url_address = $"{url}/{FileId}/content";

        var request = await Http.GetAsync(url_address, Cancel).ConfigureAwait(false);

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

    /// <summary>Удалить файл из хранилища</summary>
    /// <param name="Id">Идентификатор файла</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Информация об удалении файла</returns>
    public async Task<(Guid FileId, bool Deleted)> DeleteFileAsync(Guid Id, CancellationToken Cancel = default)
    {
        const string url = "files";
        var url_address = $"{url}/{Id}/delete";

        _Log.LogInformation("Удаление файла {Id}", Id);

        var request = new HttpRequestMessage(HttpMethod.Post, url_address)
            .WithAcceptApplicationJson();

        var response = await Http.SendAsync(request, Cancel).ConfigureAwait(false);

        try
        {
            var file_info = await response.AsJsonAsync<FileDeleteInfo>(JsonOptions, Cancel).ConfigureAwait(false);

            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if (file_info is { Deleted: true })
                _Log.LogInformation("Файл {Id} удален", Id);
            else
                _Log.LogInformation("Файл {Id} не удален", Id);

            return (file_info.Id, file_info.Deleted);
        }
        catch (HttpRequestException e)
        {
            var error_content = await response.Content.ReadAsStringAsync(Cancel).ConfigureAwait(false);

            _Log.LogError(e, "Ошибка[{StatusCode}] в ходе передачи запроса на удаление файла {Id}: {Message}",
                e.StatusCode, Id, e.Message);

            throw new InvalidOperationException($"Ошибка[{e.StatusCode}] в ходе передачи запроса на удаление файла {Id}", e)
                .WithData("ErrorContent", error_content);
        }
        catch (Exception e)
        {
            var error_content = await response.Content.ReadAsStringAsync(Cancel).ConfigureAwait(false);

            _Log.LogError(e, "Ошибка в ходе передачи запроса на удаление файла {Id}: {Message}", Id, e.Message);

            throw new InvalidOperationException($"Ошибка в ходе передачи запроса на удаление файла {Id}", e)
                .WithData("ErrorContent", error_content);
        }
    }
}