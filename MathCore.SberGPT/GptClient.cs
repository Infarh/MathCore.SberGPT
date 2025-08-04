using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace MathCore.SberGPT;

/// <summary>Клиент для запросов к Giga chat</summary>
/// <remarks>Клиент для запросов к Giga chat</remarks>
/// <param name="Http">Http-клиент для отправки запросов</param>
/// <param name="Log">Логгер</param>
public partial class GptClient(HttpClient Http, ILogger<GptClient>? Log)
{
    //internal const string BaseUrl = "http://localhost:8881";
    internal const string BaseUrl = "https://gigachat.devices.sberbank.ru/api/v1/";
    internal const string AuthUrl = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
    internal const string RequesIdHeader = "RqUID";
    internal const string ClientXIdHeader = "X-Client-ID";
    internal const string RequestXIdHeader = "X-Request-ID";
    internal const string SessionXIdHeader = "X-Session-ID";

    private readonly ILogger _Log = Log ?? NullLogger<GptClient>.Instance;

    #region Конструктор

    /// <summary>Клиент для запросов к Giga chat</summary>
    public GptClient(IConfiguration config, ILogger<GptClient>? Log = null)
        : this(
            Http: new(new SberGPTRequestHandler(config, Log)) { BaseAddress = new(BaseUrl) },
            Log: Log)
    {

    }

    /// <summary>Создаёт конфигурацию для клиента GigaChat на основе секрета и области</summary>
    /// <param name="Secret">Секретный ключ для авторизации</param>
    /// <param name="Scope">Область доступа (по умолчанию "GIGACHAT_API_PERS")</param>
    /// <returns>Объект конфигурации</returns>
    private static IConfiguration GetConfig(string Secret, string? Scope) => new ConfigurationBuilder()
        .AddInMemoryCollection([new("secret", Secret), new("scope", Scope ?? "GIGACHAT_API_PERS")]).Build();

    /// <summary>Инициализирует новый экземпляр клиента GigaChat с использованием секрета и области</summary>
    /// <param name="Secret">Секретный ключ для авторизации</param>
    /// <param name="Scope">Область доступа (по умолчанию "GIGACHAT_API_PERS")</param>
    /// <param name="Log">Логгер для вывода диагностической информации</param>
    public GptClient(string Secret, string? Scope = null, ILogger<GptClient>? Log = null)
        : this(
            Http: new(new SberGPTRequestHandler(GetConfig(Secret, Scope), Log ?? NullLogger<GptClient>.Instance)) { BaseAddress = new(BaseUrl) },
            Log: Log ?? NullLogger<GptClient>.Instance)
    {

    }

    #endregion

    /// <summary>Содержимое заголовка X-Session-Id</summary>
    public string? SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Содержимое заголовка X-Request-Id</summary>
    public string? RequestId { get; set; }
}