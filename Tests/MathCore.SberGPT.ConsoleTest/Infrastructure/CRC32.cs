namespace MathCore.SberGPT.ConsoleTest.Infrastructure;

public class Crc32
{
    public const uint ZipPoly = 0xEDB88320;

    private readonly uint[] _CRCTable;
    private readonly uint _Init;
    private readonly bool _RefIn;
    private readonly bool _RefOut;
    private readonly uint _XorOut;

    /// <summary>Конструктор класса Crc32</summary>
    /// <param name="Poly">Полином для расчёта CRC32.</param>
    /// <param name="Init">Начальное значение CRC.</param>
    /// <param name="RefIn">Флаг отражения входных данных.</param>
    /// <param name="RefOut">Флаг отражения выходного значения.</param>
    /// <param name="XorOut">Значение для финального XOR.</param>
    public Crc32(
        uint Poly = ZipPoly,
        uint Init = 0xFFFFFFFF,
        bool RefIn = true,
        bool RefOut = true,
        uint XorOut = 0xFFFFFFFF)
    {
        _Init = Init;
        _RefIn = RefIn;
        _RefOut = RefOut;
        _XorOut = XorOut;
        _CRCTable = InitializeTable(Poly, RefIn);

        return;
        static uint[] InitializeTable(uint Poly, bool RefIn)
        {
            var table = new uint[256];

            if (RefIn)
                for (var i = 0; i < 256; i++)
                {
                    ref var crc = ref table[i];
                    crc = ReflectByte((byte)i);

                    for (var j = 0; j < 8; j++)
                    {
                        crc >>= 1;
                        if ((crc & 0x00000001) != 0)
                            crc ^= Poly;
                    }
                }
            else
                for (var i = 0; i < 256; i++)
                {
                    ref var crc = ref table[i];
                    crc = (uint)(i << 24);

                    for (var j = 0; j < 8; j++)
                    {
                        crc <<= 1;
                        if ((crc & 0x80000000) != 0)
                            crc ^= Poly;
                    }
                }

            return table;
        }
    }

    /// <summary>Вычисляет CRC32 для потока данных</summary>
    /// <param name="Stream">Входной поток данных.</param>
    /// <param name="PreviousCrc">Предыдущее значение CRC для последовательного расчёта.</param>
    /// <returns>Вычисленное значение CRC32.</returns>
    public uint Compute(Stream Stream, uint PreviousCrc = 0)
    {
        ArgumentNullException.ThrowIfNull(Stream);
        var crc = _Init ^ PreviousCrc;
        const int buffer_length = 4096;
        var buffer_array = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer_length);
        var buffer = buffer_array.AsSpan(0, buffer_length);

        try
        {
            if (_RefIn)
                while (Stream.Read(buffer) is > 0 and var read)
                    for (var i = 0; i < read; i++)
                        crc = (crc >> 8) ^ _CRCTable[(crc & 0xFF) ^ ReflectByte(buffer_array[i])];
            else
                while (Stream.Read(buffer) is > 0 and var read)
                    for (var i = 0; i < read; i++)
                        crc = (crc << 8) ^ _CRCTable[(crc >> 24) ^ buffer_array[i]];
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer_array);
        }

        var result = _RefOut ? ReflectCrc(crc) : crc;
        return result ^ _XorOut;
    }

    /// <summary>Вычисляет CRC32 для потока данных</summary>
    /// <param name="Stream">Входной поток данных.</param>
    /// <param name="PreviousCrc">Предыдущее значение CRC для последовательного расчёта.</param>
    /// <param name="Cancel">Отмена асинхронной операции</param>
    /// <returns>Вычисленное значение CRC32.</returns>
    public async ValueTask<uint> ComputeAsync(Stream Stream, uint PreviousCrc = 0, CancellationToken Cancel = default)
    {
        ArgumentNullException.ThrowIfNull(Stream);

        var crc = _Init ^ PreviousCrc;
        const int buffer_length = 4096;
        var buffer_array = System.Buffers.ArrayPool<byte>.Shared.Rent(buffer_length);
        var buffer = buffer_array.AsMemory(0, buffer_length);

        try
        {
            if (_RefIn)
                while (await Stream.ReadAsync(buffer, Cancel).ConfigureAwait(false) is > 0 and var read)
                {
                    var buffer_span = buffer.Span;
                    for (var i = 0; i < read; i++)
                        crc = (crc >> 8) ^ _CRCTable[(crc & 0xFF) ^ ReflectByte(buffer_span[i])];
                }
            else
                while (await Stream.ReadAsync(buffer, Cancel).ConfigureAwait(false) is > 0 and var read)
                {
                    var buffer_span = buffer.Span;
                    for (var i = 0; i < read; i++)
                        crc = (crc << 8) ^ _CRCTable[(crc >> 24) ^ buffer_span[i]];
                }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer_array);
        }

        var result = _RefOut ? ReflectCrc(crc) : crc;
        return result ^ _XorOut;
    }

    /// <summary>Вычисляет CRC32 для диапазона байт</summary>
    /// <param name="Data">Входной диапазон байт.</param>
    /// <param name="PreviousCrc">Предыдущее значение CRC для последовательного расчёта.</param>
    /// <returns>Вычисленное значение CRC32.</returns>
    public uint Compute(ReadOnlySpan<byte> Data, uint PreviousCrc = 0)
    {
        var crc = _Init ^ PreviousCrc;
        if (_RefIn)
            foreach (var b in Data)
            {
                var ref_byte = ReflectByte(b);
                crc = (crc >> 8) ^ _CRCTable[(crc & 0xFF) ^ ref_byte];
            }
        else
            foreach (var b in Data)
                crc = (crc << 8) ^ _CRCTable[(crc >> 24) ^ b];

        var result = _RefOut ? ReflectCrc(crc) : crc;
        return result ^ _XorOut;
    }

    private static uint ReflectCrc(uint Value)
    {
        Value = ((Value >> 16) | (Value << 16));
        Value = ((Value & 0xFF00FF00) >> 8) | ((Value & 0x00FF00FF) << 8);
        Value = ((Value & 0xF0F0F0F0) >> 4) | ((Value & 0x0F0F0F0F) << 4);
        Value = ((Value & 0xCCCCCCCC) >> 2) | ((Value & 0x33333333) << 2);
        Value = ((Value & 0xAAAAAAAA) >> 1) | ((Value & 0x55555555) << 1);
        return Value;
    }

    private static byte ReflectByte(byte Value)
    {
        Value = (byte)(((Value & 0xF0) >> 4) | ((Value & 0x0F) << 4));
        Value = (byte)(((Value & 0xCC) >> 2) | ((Value & 0x33) << 2));
        Value = (byte)(((Value & 0xAA) >> 1) | ((Value & 0x55) << 1));
        return Value;
    }
}