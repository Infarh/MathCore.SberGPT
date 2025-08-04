using System.ComponentModel;
using System.Net.Http.Json;

using MathCore.SberGPT.Models;

namespace MathCore.SberGPT;

public partial class GptClient
{
    #region Векторизация текста

    /// <summary>Получить векторизацию текста</summary>
    /// <param name="MessageStrings">Строки, для которых требуется вычислить вектора</param>
    /// <param name="Model">Модель</param>
    /// <param name="Cancel">Отмена операции</param>
    /// <returns>Результат векторизации текста</returns>
    public async Task<EmbeddingResponse> GetEmbeddingsAsync(
        IEnumerable<string> MessageStrings,
        EmbeddingModel Model = EmbeddingModel.Embeddings,
        CancellationToken Cancel = default)
    {
        const string url = "embeddings";

        var model_name = Model switch
        {
            EmbeddingModel.Embeddings => "Embeddings",
            EmbeddingModel.EmbeddingsGigaR => "EmbeddingsGigaR",
            _ => throw new InvalidEnumArgumentException("Неподдерживаемый тип модели", (int)Model, typeof(EmbeddingModel))
        };

        var request = new EmbeddingRequest(model_name, MessageStrings);

        var response = await Http.PostAsJsonAsync(url, request, JsonOptions, Cancel).ConfigureAwait(false);

        var result = await response.AsJsonAsync<EmbeddingResponse>(JsonOptions, Cancel).ConfigureAwait(false);

        return result;
    }

    #endregion
}
