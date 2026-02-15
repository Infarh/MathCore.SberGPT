using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using MathCore.SberGPT.Extensions;
using MathCore.SberGPT.Models;

using Microsoft.Extensions.Logging;
// ReSharper disable MemberCanBePrivate.Global

namespace MathCore.SberGPT;

/// <summary>Клиент для работы с Gpt и функциями</summary>
public partial class GptClient
{
    /// <summary>Опции сериализации для валидации функций</summary>
    private static readonly JsonSerializerOptions __ValidateFunctionSerializationOptions = new()
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.BasicLatin, UnicodeRanges.Cyrillic),
        WriteIndented = false,
    };

    /// <summary>Словарь зарегистрированных функций</summary>
    private readonly Dictionary<string, FunctionInfo> _Functions = [];

    /// <summary>Выполняет валидацию делегата-функции</summary>
    /// <param name="Function">Делегат функции</param>
    /// <param name="Cancel">Токен отмены</param>
    /// <returns>Результат валидации</returns>
    /// <exception cref="InvalidOperationException">Ошибка получения имени функции или данных от сервера</exception>
    public async Task<ValidationFunctionResult> ValidateFunctionAsync(Delegate Function, CancellationToken Cancel = default)
    {
        var function_info = Function.GetJsonScheme();

        var function_name = function_info["name"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Не удалось получить имя функции из схемы");

        var str_content = JsonSerializer.Serialize(function_info, __ValidateFunctionSerializationOptions);
        var request = new HttpRequestMessage(HttpMethod.Post, "functions/validate").WithJson(str_content);

        var response = await Http.SendAsync(request, Cancel);
        try
        {
            var result = await response.AsJsonAsync<ValidationFunctionResult>(JsonOptions, Cancel).ConfigureAwait(false);

            if (result.IsCorrect)
                _Log.LogInformation("Функция {function} успешно прошла валидацию: {result}", function_name, result);
            else
                _Log.LogInformation("Функция {function} не прошла валидацию: {result}", function_name, result);

            return result;
        }
        catch (Exception error)
        {
            var response_message = await response.Content.ReadAsStringAsync(Cancel).ConfigureAwait(false);
            _Log.LogError(error, "Ошибка {responseMessage} получения данных от сервера в процессе выполнения валидации функции {function}",
                response_message,
                function_info);

            throw new InvalidOperationException("Ошибка получения данных от сервера в процессе валидации функции", error)
                .WithData(nameof(Function), function_info);
        }
    }

    /// <summary>Добавляет функцию в клиент после успешной валидации</summary>
    /// <param name="Function">Делегат функции</param>
    /// <param name="Cancel">Токен отмены</param>
    /// <returns>Информация о добавленной функции</returns>
    /// <exception cref="InvalidOperationException">Функция не прошла валидацию</exception>
    public async Task<FunctionInfo> AddFunctionAsync(Delegate Function, CancellationToken Cancel = default)
    {
        var validation_result = await ValidateFunctionAsync(Function, Cancel).ConfigureAwait(false);
        if (validation_result.HasErrors)
            throw new InvalidOperationException($"Функция не прошла валидацию: {validation_result.Message}")
                .WithData(nameof(Function), Function.GetJsonScheme())
                .WithData("ValidationResult", validation_result);

        var function_info = Function.GetJsonScheme();

        var function_name = function_info["name"]?.GetValue<string>()
            ?? throw new InvalidOperationException("Не удалось получить имя функции из схемы");
        var function_description = function_info["description"]?.GetValue<string>();

        var function_args_map = Function.GetArgsMap();

        var info = new FunctionInfo(function_name, function_description, validation_result, Function, function_info, function_args_map);
        _Functions[function_name] = info;

        _Log.LogInformation("Функция {function}:\"{description}\" добавлена", function_name, function_description);

        return info;
    }
}
