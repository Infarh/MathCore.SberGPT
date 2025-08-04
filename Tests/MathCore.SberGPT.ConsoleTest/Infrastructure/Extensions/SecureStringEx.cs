using System.Security;

namespace MathCore.SberGPT.ConsoleTest.Infrastructure.Extensions;

internal static class SecureStringEx
{
    public static SecureString AddStr(this SecureString s, string str)
    {
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(str);
        foreach (var c in str)
            s.AppendChar(c);
        return s;
    }
}
