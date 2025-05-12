using System.Net.Http.Headers;

namespace MathCore.SberGPT;

/// <summary>Токен доступа</summary>
/// <param name="Token">Токен</param>
/// <param name="ExpiredTime">Время истечения срока действия</param>
public readonly record struct AccessToken(string Token, DateTimeOffset ExpiredTime)
{
    /// <summary>Признак истечения срока действия токена</summary>
    public bool Expired => (ExpiredTime - DateTimeOffset.Now).TotalMilliseconds < 500;

    public static implicit operator AuthenticationHeaderValue(AccessToken token) => new("Bearer", token.Token);
}