using Qreator.Core.Models;

namespace Qreator.Core.Encoding;

internal static class QrVersionSelector
{
    internal static (int Version, IReadOnlyList<Segment> Segments) Select(string inputData, ErrorCorrectionLevel errorCorrection)
    {
        List<Segment> basicSegments = QrSegmenter.BasicSegments(inputData);
        List<Segment>? optimizedSegments = null;
        int previousRange = -1;
        for (int version = 1; version <= 40; version++)
        {
            int range = QrEncodingConstants.VersionRange(version);
            if (range != previousRange)
            {
                optimizedSegments = QrSegmenter.Optimize(basicSegments, range);
                previousRange = range;
            }

            int bitCount = optimizedSegments!.Sum(segment =>
                4 + QrEncodingConstants.CharacterCountBits[(int)segment.Mode][range] + QrSegmenter.BitLength(segment));
            int capacity = (QrSpecificationTables.TotalCodewords[version - 1]
                - QrSpecificationTables.ErrorCorrectionCodewords[(int)errorCorrection][version - 1]) * 8;
            if (bitCount <= capacity)
                return (version, optimizedSegments!);
        }
        throw new ArgumentException("Input is too long for a QR code at this error correction level.", nameof(inputData));
    }
}

