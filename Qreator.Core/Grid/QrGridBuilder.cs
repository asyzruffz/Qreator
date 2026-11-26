using Qreator.Core.Encoding;
using Qreator.Core.Models;

namespace Qreator.Core.Grid;

internal sealed class QrGridBuilder(QR qr)
{
    private int Size => qr.GridSize;

    internal void DrawPatterns()
    {
        DrawFinder(0, 0);
        DrawFinder(0, Size - 7);
        DrawFinder(Size - 7, 0);
        for (int position = 8; position < Size - 8; position++)
        {
            SetModule(6, position, position % 2 == 0, ReservedBits.TimingPattern);
            SetModule(position, 6, position % 2 == 0, ReservedBits.TimingPattern);
        }
        DrawAlignmentPatterns();
        DrawVersionInformation();
        WriteFormatInformation(0, qr.Data);
        SetModule(Size - 8, 8, true, ReservedBits.DarkModule);
    }

    private void DrawFinder(int top, int left)
    {
        for (int y = -1; y <= 7; y++)
        {
            for (int x = -1; x <= 7; x++)
            {
                int row = top + y;
                int column = left + x;
                if (row < 0 || column < 0 || row >= Size || column >= Size)
                    continue;
                bool inside = x >= 0 && x <= 6 && y >= 0 && y <= 6;
                bool dark = inside && (x == 0 || x == 6 || y == 0 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4));
                SetModule(row, column, dark, inside ? ReservedBits.FinderPattern : ReservedBits.Separator);
            }
        }
    }

    private void DrawAlignmentPatterns()
    {
        if (qr.Version == 1)
            return;
        int subdivisions = qr.Version / 7;
        int lastPosition = Size - 7;
        int spacing = QrSpecificationTables.AlignmentPatternSpacing[qr.Version - 1];
        var positions = new List<int> { 6 };
        for (int index = subdivisions; index >= 1; index--)
            positions.Add(lastPosition - index * spacing);
        positions.Add(lastPosition);

        foreach (int row in positions)
        {
            foreach (int column in positions)
            {
                if ((row == 6 && column == 6) || (row == 6 && column == lastPosition) || (row == lastPosition && column == 6))
                    continue;
                for (int y = -2; y <= 2; y++)
                    for (int x = -2; x <= 2; x++)
                        SetModule(row + y, column + x, Math.Max(Math.Abs(x), Math.Abs(y)) != 1, ReservedBits.AlignmentPattern);
            }
        }
    }

    private void DrawVersionInformation()
    {
        if (qr.Version < 7)
            return;
        int bits = qr.Version << 12;
        int remainder = bits;
        for (int index = 17; index >= 12; index--)
            if ((remainder & (1 << index)) != 0)
                remainder ^= 0x1f25 << (index - 12);
        bits |= remainder & 0xfff;
        for (int index = 0; index < 18; index++)
        {
            int row = index / 3;
            int column = Size - 11 + index % 3;
            bool dark = ((bits >> index) & 1) != 0;
            SetModule(row, column, dark, ReservedBits.VersionInfo);
            SetModule(column, row, dark, ReservedBits.VersionInfo);
        }
    }

    private void SetModule(int row, int column, bool dark, ReservedBits type)
    {
        int index = row * Size + column;
        qr.Data[index] = (byte)(dark ? 1 : 0);
        qr.ReservedBits[index] = new ReservedBitInfo(type, dark);
    }

    internal void WriteFormatInformation(int mask, byte[] target)
    {
        int correctionBits = qr.ErrorCorrection switch
        {
            ErrorCorrectionLevel.L => 1,
            ErrorCorrectionLevel.M => 0,
            ErrorCorrectionLevel.Q => 3,
            _ => 2
        };
        int value = (correctionBits << 3) | mask;
        int remainder = value << 10;
        for (int index = 4; index >= 0; index--)
            if ((remainder & (1 << (index + 10))) != 0)
                remainder ^= 0x537 << index;
        int bits = ((value << 10) | (remainder & 0x3ff)) ^ 0x5412;

        void Write(int row, int column, int bit)
        {
            int index = row * Size + column;
            target[index] = (byte)bit;
            qr.ReservedBits[index] = new ReservedBitInfo(ReservedBits.FormatInfo, bit != 0);
        }
        for (int index = 0; index < 15; index++)
        {
            int bit = (bits >> index) & 1;
            Write(index < 6 ? index : index < 8 ? index + 1 : Size - 15 + index, 8, bit);
            Write(8, index < 8 ? Size - index - 1 : index < 9 ? 15 - index : 14 - index, bit);
        }
    }

    internal void PlaceCodewords(byte[] codewords)
    {
        int bitIndex = 0;
        bool upwards = true;
        for (int column = Size - 1; column >= 1; column -= 2)
        {
            if (column == 6)
                column = 5;
            for (int offset = 0; offset < Size; offset++)
            {
                for (int pair = 0; pair < 2; pair++)
                {
                    int row = upwards ? Size - 1 - offset : offset;
                    int gridIndex = row * Size + column - pair;
                    if (qr.ReservedBits.ContainsKey(gridIndex))
                        continue;
                    if (bitIndex < codewords.Length * 8)
                        qr.Data[gridIndex] = (byte)((codewords[bitIndex / 8] >> (7 - bitIndex % 8)) & 1);
                    bitIndex++;
                }
            }
            upwards = !upwards;
        }
    }

    internal byte[] ApplyMask(int mask, byte[] target)
    {
        for (int row = 0; row < Size; row++)
        {
            for (int column = 0; column < Size; column++)
            {
                int index = row * Size + column;
                if (qr.ReservedBits.ContainsKey(index))
                    continue;
                bool flip = mask switch
                {
                    0 => (row + column) % 2 == 0,
                    1 => row % 2 == 0,
                    2 => column % 3 == 0,
                    3 => (row + column) % 3 == 0,
                    4 => (row / 2 + column / 3) % 2 == 0,
                    5 => row * column % 2 + row * column % 3 == 0,
                    6 => (row * column % 2 + row * column % 3) % 2 == 0,
                    _ => ((row + column) % 2 + row * column % 3) % 2 == 0
                };
                if (flip)
                    target[index] ^= 1;
            }
        }
        return target;
    }
}
