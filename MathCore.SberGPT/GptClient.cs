using System.Net.Http.Json;
using System.Text.Json.Serialization;

using static MathCore.SberGPT.GptClient.TokensCount;

namespace MathCore.SberGPT;

public class GptClient(HttpClient http)
{
    private readonly record struct ModelsInfosList([property: JsonPropertyName("data")] ModelInfo[] Models);

    private readonly record struct ModelInfo([property: JsonPropertyName("id")] string ModelId);

    /// <summary>Получить список моделей</summary>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Список моделей сервиса</returns>
    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken Cancel = default)
    {
        const string url = "api/v1/models";

        var response = await http.GetFromJsonAsync<ModelsInfosList>(url, Cancel).ConfigureAwait(false);

        var models = response.Models;
        var result = new string[models.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = models[i].ModelId;

        return result;
    }

    private readonly record struct GetTokensCountMessage(
        [property: JsonPropertyName("model")] string Model, 
        [property: JsonPropertyName("input")] string[] Input);

    private readonly record struct TokensCountResponse(
        [property: JsonPropertyName("tokens")] int Tokens,
        [property: JsonPropertyName("characters")] int Characters);

    /// <summary>Информация о количестве токенов для указанной строки ввода</summary>
    /// <param name="Input">Строка ввода</param>
    /// <param name="Tokens">Количество токенов</param>
    /// <param name="Characters">Количество символов</param>
    public readonly record struct TokensCount(string Input, int Tokens, int Characters);

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строка ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat"</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Информация о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCount> GetTokensCountAsync(string Input, string Model = "GigaChat", CancellationToken Cancel = default)
    {
        var tokens = await GetTokensCountAsync([Input], Model, Cancel).ConfigureAwait(false);
        return tokens[0];
    }

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строки ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat"</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Массив с информацией о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<IReadOnlyList<TokensCount>> GetTokensCountAsync(IEnumerable<string> Input, string Model = "GigaChat", CancellationToken Cancel = default)
    {
        var input = Input.ToArray();
        const string url = "api/v1/tokens/count";
        var response = await http.PostAsJsonAsync<GetTokensCountMessage>(url, new(Model, input), cancellationToken: Cancel)
            .ConfigureAwait(false);

        var counts = await response
            .EnsureSuccessStatusCode()
            .Content
            .ReadFromJsonAsync<TokensCountResponse[]>(cancellationToken: Cancel)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException();

        var result = new TokensCount[counts.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var (tokens, chars) = counts[i];
            result[i] = new(input[i], tokens, chars);
        }

        return result;
    }
}
