using System.Net;
using System.Net.Http.Json;

using MathCore.SberGPT.Models;

using Microsoft.Extensions.Logging;

namespace MathCore.SberGPT;

public partial class GptClient
{
    #region Запрос данных о количестве токенов

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строка ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat-2"</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Информация о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCount> GetTokensCountAsync(string Input, string Model = "GigaChat-2", CancellationToken Cancel = default)
    {
        var tokens = await GetTokensCountAsync([Input], Model, Cancel).ConfigureAwait(false);
        return tokens[0];
    }

    /// <summary>Получить количество токенов для указанной строки ввода</summary>
    /// <param name="Input">Строки ввода</param>
    /// <param name="Model">Тип модели из результатов вызова <see cref="GetModelsAsync"/>. По умолчанию "GigaChat-2"</param>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Массив с информацией о количестве токенов <see cref="TokensCount"/></returns>
    public async Task<TokensCountInfo> GetTokensCountAsync(IEnumerable<string> Input, string Model = "GigaChat-2", CancellationToken Cancel = default)
    {
        var input = Input.ToArray();
        const string url = "tokens/count";
        var response = await Http
            .PostAsJsonAsync<GetTokensCountRequest>(url, new(Model, input), cancellationToken: Cancel)
            .ConfigureAwait(false);

        var counts = await response.AsJsonAsync<TokensCountResponse[]>(JsonOptions, Cancel).ConfigureAwait(false);

        var result = new TokensCount[counts.Length];
        for (var i = 0; i < result.Length; i++)
        {
            var (tokens, chars) = counts[i];
            result[i] = new(input[i], tokens, chars);
        }

        return result;
    }

    #endregion

    #region Баланс токенов

    /// <summary>Получить информацию о количестве токенов</summary>
    public async Task<IReadOnlyList<(string Model, int TokensElapsed)>> GetTokensBalanceAsync(CancellationToken Cancel = default)
    {
        const string url = "balance";

        try
        {
            _Log.LogInformation("Запрос количества токенов");
            var balance = await Http.GetFromJsonAsync<BalanceInfo>(url, JsonOptions, Cancel).ConfigureAwait(false);

            _Log.LogTrace("Оставшееся количество токенов {Balance}", balance);

            return balance.Tokens.Select(token => (token.Model, token.TokensElapsed)).ToArray();
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.WriteLine(e);
            throw;
        }
        catch (HttpRequestException e) when (e.StatusCode == HttpStatusCode.Forbidden)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    #endregion
}
