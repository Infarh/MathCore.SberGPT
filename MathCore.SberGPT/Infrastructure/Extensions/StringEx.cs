using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace MathCore.SberGPT.Infrastructure.Extensions;

/// <summary>Статический класс-расширение для работы со строками.</summary>
internal static class StringEx
{
    /// <summary>Вычисляет SHA512-хеш строки.</summary>
    public static byte[] GetSHA512(this string str) => SHA512.HashData(MemoryMarshal.AsBytes(str.AsSpan()));
}
