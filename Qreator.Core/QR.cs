using System.Text;

namespace Qreator.Core;

public enum ErrorCorrectionLevel { L, M, Q, H }
public enum Mode { Numeric, AlphaNumeric, Byte, Kanji }
public enum ReservedBits { FinderPattern, AlignmentPattern, TimingPattern, FormatInfo, VersionInfo, DarkModule, Separator }
public sealed record Segment(string Value, Mode Mode);
public sealed record ReservedBitInfo(ReservedBits Type, bool Dark);
public sealed class QrOptions { public ErrorCorrectionLevel ErrorCorrection { get; set; } = ErrorCorrectionLevel.M; }

public sealed class QR
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
    private static readonly int[][] CountBits = [[10, 12, 14], [9, 11, 13], [8, 16, 16], [8, 10, 12]];
    public string InputData { get; }
    public IReadOnlyList<Segment> Segments { get; private set; } = [];
    public byte[] Data { get; }
    public int GridSize { get; }
    public int Version { get; }
    public ErrorCorrectionLevel ErrorCorrection { get; }
    public Dictionary<int, ReservedBitInfo> ReservedBits { get; } = [];
    public int MaskPattern { get; private set; }

    public QR(string inputData, QrOptions? options = null)
    {
        if (string.IsNullOrEmpty(inputData)) throw new ArgumentException("Not a valid string input", nameof(inputData));
        InputData = inputData;
        ErrorCorrection = options?.ErrorCorrection ?? ErrorCorrectionLevel.M;
        if (!Enum.IsDefined(ErrorCorrection)) throw new ArgumentOutOfRangeException(nameof(options));
        var basic = BasicSegments(inputData);
        for (int version = 1; version <= 40; version++)
        {
            var segments = Optimize(basic, version <= 9 ? 0 : version <= 26 ? 1 : 2);
            int bits = segments.Sum(s => 4 + CountBits[(int)s.Mode][version <= 9 ? 0 : version <= 26 ? 1 : 2] + BitLength(s));
            if (bits <= (Tables.Codewords[version - 1] - Tables.EcCodewords[(int)ErrorCorrection][version - 1]) * 8)
            { Version = version; Segments = segments; break; }
        }
        if (Version == 0) throw new ArgumentException("Input is too long for a QR code at this error correction level.", nameof(inputData));
        GridSize = Version * 4 + 17;
        Data = new byte[GridSize * GridSize];
        var codewords = Encode();
        DrawPatterns();
        PlaceCodewords(codewords);
        int best = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            var copy = ApplyMask(mask, (byte[])Data.Clone());
            Format(mask, copy);
            int penalty = Penalty(copy, GridSize);
            if (penalty < best) { best = penalty; MaskPattern = mask; }
        }
        ApplyMask(MaskPattern, Data);
        Format(MaskPattern, Data);
    }

    private static List<Segment> BasicSegments(string text)
    {
        var result = new List<Segment>();
        foreach (char c in text)
        {
            Mode mode = c is >= '0' and <= '9' ? Mode.Numeric : Alphabet.Contains(c) ? Mode.AlphaNumeric : Mode.Byte;
            if (result.Count > 0 && result[^1].Mode == mode) result[^1] = result[^1] with { Value = result[^1].Value + c };
            else result.Add(new(c.ToString(), mode));
        }
        return result;
    }
    private static int BitLength(Segment s) => s.Mode switch
    {
        Mode.Numeric => s.Value.Length / 3 * 10 + (s.Value.Length % 3 == 0 ? 0 : s.Value.Length % 3 * 3 + 1),
        Mode.AlphaNumeric => s.Value.Length / 2 * 11 + s.Value.Length % 2 * 6,
        _ => Encoding.UTF8.GetByteCount(s.Value) * 8
    };
    // Match the reference graph: same-mode edges count the incremental cost of the preceding basic segment.
    private static List<Segment> Optimize(List<Segment> basic, int range)
    {
        var states = new List<(Segment segment, int cost, List<Segment> path)>();
        foreach (var item in basic)
        {
            Mode[] modes = item.Mode switch { Mode.Numeric => [Mode.Numeric, Mode.AlphaNumeric, Mode.Byte], Mode.AlphaNumeric => [Mode.AlphaNumeric, Mode.Byte], _ => [Mode.Byte] };
            var next = new List<(Segment segment, int cost, List<Segment> path)>();
            foreach (var mode in modes)
            {
                var current = new Segment(item.Value, mode);
                if (states.Count == 0) { next.Add((current, BitLength(current) + 4 + CountBits[(int)mode][range], [current])); continue; }
                var best = states.Select(s => (state: s, cost: s.cost + (s.segment.Mode == mode ? BitLength(new(s.segment.Value + current.Value, mode)) - BitLength(s.segment) : BitLength(current) + 4 + CountBits[(int)mode][range]))).OrderBy(x => x.cost).First();
                var path = new List<Segment>(best.state.path);
                if (path[^1].Mode == mode) path[^1] = path[^1] with { Value = path[^1].Value + current.Value }; else path.Add(current);
                next.Add((current, best.cost, path));
            }
            states = next;
        }
        return states.OrderBy(s => s.cost).First().path;
    }
    private byte[] Encode()
    {
        int total = Tables.Codewords[Version - 1], ecTotal = Tables.EcCodewords[(int)ErrorCorrection][Version - 1];
        var data = new byte[total - ecTotal]; int position = 0;
        void Put(int value, int length) { for (int i = length - 1; i >= 0; i--, position++) if (((value >> i) & 1) != 0) data[position / 8] |= (byte)(0x80 >> (position % 8)); }
        int range = Version <= 9 ? 0 : Version <= 26 ? 1 : 2;
        foreach (var s in Segments)
        {
            Put(s.Mode switch { Mode.Numeric => 1, Mode.AlphaNumeric => 2, _ => 4 }, 4);
            Put(s.Mode == Mode.Byte ? Encoding.UTF8.GetByteCount(s.Value) : s.Value.Length, CountBits[(int)s.Mode][range]);
            if (s.Mode == Mode.Numeric)
                for (int i = 0; i < s.Value.Length; i += 3) { int n = Math.Min(3, s.Value.Length - i); Put(int.Parse(s.Value.AsSpan(i, n)), n == 3 ? 10 : n == 2 ? 7 : 4); }
            else if (s.Mode == Mode.AlphaNumeric)
                for (int i = 0; i < s.Value.Length; i += 2) { int a = Alphabet.IndexOf(s.Value[i]); if (i + 1 < s.Value.Length) Put(a * 45 + Alphabet.IndexOf(s.Value[i + 1]), 11); else Put(a, 6); }
            else foreach (byte b in Encoding.UTF8.GetBytes(s.Value)) Put(b, 8);
        }
        Put(0, Math.Min(4, data.Length * 8 - position));
        if (position % 8 != 0) Put(0, 8 - position % 8);
        for (int i = 0; position < data.Length * 8; i++) Put(i % 2 == 0 ? 0xec : 0x11, 8);
        int blocks = Tables.EcBlocks[(int)ErrorCorrection][Version - 1], shortBlocks = blocks - total % blocks;
        int shortData = data.Length / blocks, ecCount = ecTotal / blocks, offset = 0;
        var parts = new List<byte[]>(); var checks = new List<byte[]>();
        for (int b = 0; b < blocks; b++) { int len = shortData + (b < shortBlocks ? 0 : 1); var part = data[offset..(offset + len)]; parts.Add(part); checks.Add(ReedSolomon(part, ecCount)); offset += len; }
        var result = new List<byte>(total);
        for (int i = 0; i <= shortData; i++) foreach (var part in parts) if (i < part.Length) result.Add(part[i]);
        for (int i = 0; i < ecCount; i++) foreach (var check in checks) result.Add(check[i]);
        return result.ToArray();
    }
    private static byte[] ReedSolomon(byte[] data, int count)
    {
        int Mul(int a, int b) { int r = 0; while (b != 0) { if ((b & 1) != 0) r ^= a; a <<= 1; if ((a & 256) != 0) a ^= 0x11d; b >>= 1; } return r; }
        int[] gen = [1]; int root = 1;
        for (int i = 0; i < count; i++) { var next = new int[gen.Length + 1]; for (int j = 0; j < gen.Length; j++) { next[j] ^= gen[j]; next[j + 1] ^= Mul(gen[j], root); } gen = next; root = Mul(root, 2); }
        var encoded = new byte[data.Length + count]; data.CopyTo(encoded, 0);
        for (int i = 0; i < data.Length; i++) { int c = encoded[i]; for (int j = 0; j < gen.Length; j++) encoded[i + j] ^= (byte)Mul(gen[j], c); }
        return encoded[data.Length..];
    }
    private void Set(int row, int col, bool dark, ReservedBits type)
    { int i = row * GridSize + col; Data[i] = (byte)(dark ? 1 : 0); ReservedBits[i] = new(type, dark); }
    private void DrawPatterns()
    {
        void Finder(int r, int c)
        {
            for (int y = -1; y <= 7; y++) for (int x = -1; x <= 7; x++)
            {
                int row = r + y, col = c + x; if (row < 0 || col < 0 || row >= GridSize || col >= GridSize) continue;
                bool inside = x >= 0 && x <= 6 && y >= 0 && y <= 6;
                bool dark = inside && (x == 0 || x == 6 || y == 0 || y == 6 || (x >= 2 && x <= 4 && y >= 2 && y <= 4));
                Set(row, col, dark, inside ? Qreator.Core.ReservedBits.FinderPattern : Qreator.Core.ReservedBits.Separator);
            }
        }
        Finder(0, 0); Finder(0, GridSize - 7); Finder(GridSize - 7, 0);
        for (int i = 8; i < GridSize - 8; i++) { Set(6, i, i % 2 == 0, Qreator.Core.ReservedBits.TimingPattern); Set(i, 6, i % 2 == 0, Qreator.Core.ReservedBits.TimingPattern); }
        if (Version > 1)
        {
            int subdivisions = Version / 7, last = GridSize - 7, diff = Tables.AlignmentDiffs[Version - 1];
            var positions = new List<int> { 6 }; for (int i = subdivisions; i >= 1; i--) positions.Add(last - i * diff); positions.Add(last);
            foreach (int row in positions) foreach (int col in positions)
            {
                if ((row == 6 && col == 6) || (row == 6 && col == last) || (row == last && col == 6)) continue;
                for (int y = -2; y <= 2; y++) for (int x = -2; x <= 2; x++) Set(row + y, col + x, Math.Max(Math.Abs(x), Math.Abs(y)) != 1, Qreator.Core.ReservedBits.AlignmentPattern);
            }
        }
        if (Version >= 7)
        {
            int bits = Version << 12, rem = bits;
            for (int i = 17; i >= 12; i--) if ((rem & (1 << i)) != 0) rem ^= 0x1f25 << (i - 12);
            bits |= rem & 0xfff;
            for (int i = 0; i < 18; i++) { int row = i / 3, col = GridSize - 11 + i % 3; bool bit = ((bits >> i) & 1) != 0; Set(row, col, bit, Qreator.Core.ReservedBits.VersionInfo); Set(col, row, bit, Qreator.Core.ReservedBits.VersionInfo); }
        }
        // Reserve format locations before placing codewords.
        Format(0, Data);
        Set(GridSize - 8, 8, true, Qreator.Core.ReservedBits.DarkModule);
    }
    private void Format(int mask, byte[] target)
    {
        int ec = ErrorCorrection switch { ErrorCorrectionLevel.L => 1, ErrorCorrectionLevel.M => 0, ErrorCorrectionLevel.Q => 3, _ => 2 };
        int value = (ec << 3) | mask, rem = value << 10;
        for (int i = 4; i >= 0; i--) if ((rem & (1 << (i + 10))) != 0) rem ^= 0x537 << i;
        int bits = ((value << 10) | (rem & 0x3ff)) ^ 0x5412;
        void Write(int row, int col, int bit) { int index = row * GridSize + col; target[index] = (byte)bit; ReservedBits[index] = new(Qreator.Core.ReservedBits.FormatInfo, bit != 0); }
        for (int i = 0; i < 15; i++)
        {
            int bit = (bits >> i) & 1;
            Write(i < 6 ? i : i < 8 ? i + 1 : GridSize - 15 + i, 8, bit);
            Write(8, i < 8 ? GridSize - i - 1 : i < 9 ? 15 - i : 14 - i, bit);
        }
    }
    private void PlaceCodewords(byte[] codewords)
    {
        int bit = 0; bool up = true;
        for (int col = GridSize - 1; col >= 1; col -= 2)
        {
            if (col == 6) col = 5;
            for (int i = 0; i < GridSize; i++) for (int j = 0; j < 2; j++)
            {
                int row = up ? GridSize - 1 - i : i, index = row * GridSize + col - j;
                if (ReservedBits.ContainsKey(index)) continue;
                if (bit < codewords.Length * 8) Data[index] = (byte)((codewords[bit / 8] >> (7 - bit % 8)) & 1);
                bit++;
            }
            up = !up;
        }
    }
    private byte[] ApplyMask(int mask, byte[] target)
    {
        for (int r = 0; r < GridSize; r++) for (int c = 0; c < GridSize; c++)
        {
            int index = r * GridSize + c; if (ReservedBits.ContainsKey(index)) continue;
            bool flip = mask switch { 0 => (r + c) % 2 == 0, 1 => r % 2 == 0, 2 => c % 3 == 0, 3 => (r + c) % 3 == 0, 4 => (r / 2 + c / 3) % 2 == 0, 5 => r * c % 2 + r * c % 3 == 0, 6 => (r * c % 2 + r * c % 3) % 2 == 0, _ => ((r + c) % 2 + r * c % 3) % 2 == 0 };
            if (flip) target[index] ^= 1;
        }
        return target;
    }
    private static int Penalty(byte[] data, int size)
    {
        int score = 0; byte[] p1 = [1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0], p2 = [0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1];
        for (int axis = 0; axis < 2; axis++) for (int line = 0; line < size; line++)
        {
            byte At(int p) => axis == 0 ? data[line * size + p] : data[p * size + line];
            int run = 1; for (int i = 1; i < size; i++) { if (At(i) == At(i - 1)) run++; else { if (run >= 5) score += run - 2; run = 1; } }
            if (run >= 5) score += run - 2;
            for (int i = 0; i <= size - 11; i++) { bool a = true, b = true; for (int k = 0; k < 11; k++) { a &= At(i + k) == p1[k]; b &= At(i + k) == p2[k]; } if (a || b) score += 40; }
        }
        for (int r = 0; r < size - 1; r++) for (int c = 0; c < size - 1; c++) { int i = r * size + c; if (data[i] == data[i + 1] && data[i] == data[i + size] && data[i] == data[i + size + 1]) score += 3; }
        double percent = data.Sum(x => (int)x) * 100.0 / data.Length;
        score += (int)(Math.Min(Math.Abs(Math.Floor(percent / 5) * 5 - 50), Math.Abs(Math.Ceiling(percent / 5) * 5 - 50)) / 5 * 10);
        return score;
    }
}
