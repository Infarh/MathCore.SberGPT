using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
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

    /// <summary>Загружает токен из файла с расшифровкой</summary>
    /// <param name="FilePath">Путь к файлу</param>
    /// <param name="StoreKey">Ключ для расшифровки</param>
    /// <param name="Cancel">Токен отмены</param>
    /// <returns>Загруженный токен или значение по умолчанию</returns>
    public static async Task<AccessToken> LoadFromFileAsync(string FilePath, string? StoreKey, CancellationToken Cancel = default)
    {
        if (StoreKey is not { Length: > 0 }) return default;

        var token_store_file = new FileInfo(FilePath);
        if (!token_store_file.Exists) return default;

        var pass_bytes = StoreKey.GetSHA512();

        var key_bytes = pass_bytes[..32];
        var iv_bytes = pass_bytes[^16..];

        var aes = Aes.Create();
        aes.Key = key_bytes;
        aes.IV = iv_bytes;

        try
        {
            using var decryptor = aes.CreateDecryptor();
            await using var crypto_stream = new CryptoStream(token_store_file.OpenRead(), decryptor, CryptoStreamMode.Read);

            var token = await JsonSerializer.DeserializeAsync<AccessToken>(
                    crypto_stream,
                    Infrastructure.GptClientJsonSerializationContext.Default.Options,
                    Cancel)
                .ConfigureAwait(false);

            return token;
        }
        catch (Exception)
        {
            token_store_file.Delete();
            return default;
        }
    }

    /// <summary>Сохраняет токен в файл с шифрованием</summary>
    /// <param name="FilePath">Путь к файлу</param>
    /// <param name="StoreKey">Ключ для шифрования</param>
    /// <param name="Cancel">Токен отмены</param>
    public async Task SaveToFileAsync(string FilePath, string? StoreKey, CancellationToken Cancel = default)
    {
        if (StoreKey is not { Length: > 0 }) return;

        var token_store_file = new FileInfo(FilePath);
        token_store_file.Directory!.Create();

        var pass_bytes = StoreKey.GetSHA512();
        var aes = Aes.Create();
        aes.Key = pass_bytes[..32];
        aes.IV = pass_bytes[^16..];

        try
        {
            await using var crypto_stream = new CryptoStream(token_store_file.OpenWrite(), aes.CreateEncryptor(), CryptoStreamMode.Write);
            await JsonSerializer.SerializeAsync(crypto_stream, this, Infrastructure.GptClientJsonSerializationContext.Default.Options, Cancel).ConfigureAwait(false);
        }
        catch (Exception)
        {
            token_store_file.Delete();
            throw;
        }
    }

    /// <summary>Неявное преобразование токена доступа в AuthenticationHeaderValue.</summary>
    /// <param name="token">Токен доступа</param>
    public static implicit operator AuthenticationHeaderValue(AccessToken token) => new("Bearer", token.Token);
}