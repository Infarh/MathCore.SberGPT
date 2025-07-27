using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace MathCore.SberGPT;

/// <summary>Токен доступа</summary>
/// <param name="Token">Токен</param>
/// <param name="ExpiredTime">Время истечения срока действия</param>
public readonly record struct AccessToken(string Token, DateTimeOffset ExpiredTime)
{
    /// <summary>Признак истечения срока действия токена</summary>
    [JsonIgnore] public bool Expired => (ExpiredTime - DateTimeOffset.Now).TotalMilliseconds < 500;

    /// <summary>Оставшееся время</summary>
    [JsonIgnore] public TimeSpan DurationTime => ExpiredTime - DateTimeOffset.Now;

    /// <summary>Неявное преобразование токена доступа в AuthenticationHeaderValue.</summary>
    /// <param name="token">Токен доступа</param>
    public static implicit operator AuthenticationHeaderValue(AccessToken token) => new("Bearer", token.Token);
}