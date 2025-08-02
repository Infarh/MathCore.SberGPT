using Microsoft.Extensions.Configuration;

namespace MathCore.SberGPT.ConsoleTest;

internal static class TestFiles
{
    public static Task RunAsync() => TestUploadAsync();

    private static GptClient GetClient()
    {
        var cfg = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", true)
            .AddUserSecrets(typeof(Program).Assembly)
            .AddCommandLine(Environment.GetCommandLineArgs())
            .Build();

        return new(cfg.GetSection("Sber"));
    }

    private static async Task TestUploadAsync()
    {
        var gpt = GetClient();

        var result = await gpt.UploadFileAsync("HelloWorld.txt", new MemoryStream([.. "Hello World!"u8]));
    }
}
