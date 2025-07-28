using Microsoft.Extensions.Configuration;

namespace MathCore.SberGPT.ConsoleTest;

internal static class TestSimple
{
    public static async Task RunAsync()
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sber:ClientId"] = "258===e8-e==8-4==8-b==e-7===bda===4e",
                ["Sber:secret"] = "MjU---MxZ---ZTk---00N---LWJ---UtN---M2J---Y5M---OmJ---QxZ---LTM---ItN---Ni0---RhL---OTc---RhM---Ywi3",
            })
            .Build();

        var gpt = new GptClient(cfg.GetSection("Sber"));

        await gpt.AddFunctionAsync(Functions.GetWeather);

        var response = await gpt.RequestAsync("Можно ли будет завтра пойти искупаться в Самаре?");

        Console.WriteLine(response);

        Console.ReadLine();
    }
}
