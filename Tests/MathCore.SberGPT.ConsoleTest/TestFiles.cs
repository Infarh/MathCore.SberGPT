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

        var files = await gpt.GetFilesAsync();

        if (files is [{ Id: var file_id_to_delete }, ..])
        {
            var deleted_file = await gpt.DeleteFileAsync(file_id_to_delete);
        }

        var files2 = await gpt.GetFilesAsync();

        var result = await gpt.UploadFileAsync("HelloWorld.txt", new MemoryStream([.. "Hello World!"u8]));

        var files3 = await gpt.GetFilesAsync();

    }
}
