using System.Runtime.CompilerServices;

namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class ListOfRequestsEx
{
    public static void AddAssistant(this List<GptClient.Request> requests, string AssistMessage) => requests.Add(GptClient.Request.Assistant(AssistMessage));

    public static void AddUser(this List<GptClient.Request> requests, string UserPrompt) => requests.Add(GptClient.Request.User(UserPrompt));

    public static void AddSystem(this List<GptClient.Request> requests, string? SystemPrompt)
    {
        if (string.IsNullOrWhiteSpace(SystemPrompt))
            return;
        requests.Add(GptClient.Request.System(SystemPrompt));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void AddHistory(this List<GptClient.Request> requests, IEnumerable<GptClient.Request>? History)
    {
        if (History is null) return;
        requests.AddRange(History);
    }
}
