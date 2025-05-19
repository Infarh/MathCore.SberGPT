using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MathCore.SberGPT.Infrastructure.Extensions;

internal static class StringEx
{
    public static byte[] GetSHA512(this string str) => SHA512.HashData(MemoryMarshal.AsBytes(str.AsSpan()));
}
