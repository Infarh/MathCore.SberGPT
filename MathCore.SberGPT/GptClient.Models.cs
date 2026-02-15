using System.Net.Http.Json;

using MathCore.SberGPT.Models;

namespace MathCore.SberGPT;

public partial class GptClient
{
    #region Информация о типах моделей

    /// <summary>Получить список моделей</summary>
    /// <param name="Cancel">Токен отмены асинхронной операции</param>
    /// <returns>Список моделей сервиса</returns>
    public async Task<IReadOnlyList<string>> GetModelsAsync(CancellationToken Cancel = default)
    {
        const string url = "models";

        var response = await Http
            .GetFromJsonAsync<ModelsInfosListResponse>(url, Cancel)
            .ConfigureAwait(false);

        var models = response.Models;
        var result = new string[models.Length];
        for (var i = 0; i < result.Length; i++)
            result[i] = models[i].ModelId;

        return result;
    }

    #endregion
}
