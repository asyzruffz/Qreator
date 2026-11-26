namespace Qreator.Core.Encoding;

internal sealed class BitWriter(int byteCount)
{
    internal byte[] Data { get; } = new byte[byteCount];
    internal int Position { get; private set; }
    internal int Capacity => Data.Length * 8;

    internal void Write(int value, int bitCount)
    {
        for (int bit = bitCount - 1; bit >= 0; bit--, Position++)
            if (((value >> bit) & 1) != 0)
                Data[Position / 8] |= (byte)(0x80 >> (Position % 8));
    }
}
