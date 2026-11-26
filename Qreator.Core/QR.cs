using Qreator.Core.Encoding;
using Qreator.Core.Grid;
using Qreator.Core.Models;

namespace Qreator.Core;

public sealed class QR
{
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
        if (string.IsNullOrEmpty(inputData))
            throw new ArgumentException("Not a valid string input", nameof(inputData));

        InputData = inputData;
        ErrorCorrection = options?.ErrorCorrection ?? ErrorCorrectionLevel.M;
        if (!Enum.IsDefined(ErrorCorrection))
            throw new ArgumentOutOfRangeException(nameof(options));

        (Version, Segments) = QrVersionSelector.Select(inputData, ErrorCorrection);

        GridSize = Version * 4 + 17;
        Data = new byte[GridSize * GridSize];
        byte[] codewords = QrDataEncoder.Encode(Version, ErrorCorrection, Segments);
        var gridBuilder = new QrGridBuilder(this);
        gridBuilder.DrawPatterns();
        gridBuilder.PlaceCodewords(codewords);

        int lowestPenalty = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            byte[] candidate = gridBuilder.ApplyMask(mask, (byte[])Data.Clone());
            gridBuilder.WriteFormatInformation(mask, candidate);
            int penalty = MaskPenaltyCalculator.Calculate(candidate, GridSize);
            if (penalty < lowestPenalty)
            {
                lowestPenalty = penalty;
                MaskPattern = mask;
            }
        }

        gridBuilder.ApplyMask(MaskPattern, Data);
        gridBuilder.WriteFormatInformation(MaskPattern, Data);
    }
}

