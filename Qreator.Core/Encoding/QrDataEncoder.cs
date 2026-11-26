using Qreator.Core.Models;

namespace Qreator.Core.Encoding;

internal static class QrDataEncoder
{
    internal static byte[] Encode(int version, ErrorCorrectionLevel errorCorrection, IReadOnlyList<Segment> segments)
    {
        int totalCodewords = QrSpecificationTables.TotalCodewords[version - 1];
        int errorCodewords = QrSpecificationTables.ErrorCorrectionCodewords[(int)errorCorrection][version - 1];
        var writer = new BitWriter(totalCodewords - errorCodewords);
        int versionRange = QrEncodingConstants.VersionRange(version);

        foreach (Segment segment in segments)
        {
            writer.Write(segment.Mode switch { Mode.Numeric => 1, Mode.AlphaNumeric => 2, _ => 4 }, 4);
            int characterCount = segment.Mode == Mode.Byte ? System.Text.Encoding.UTF8.GetByteCount(segment.Value) : segment.Value.Length;
            writer.Write(characterCount, QrEncodingConstants.CharacterCountBits[(int)segment.Mode][versionRange]);
            WriteSegment(writer, segment);
        }

        writer.Write(0, Math.Min(4, writer.Capacity - writer.Position));
        if (writer.Position % 8 != 0)
            writer.Write(0, 8 - writer.Position % 8);
        for (int paddingIndex = 0; writer.Position < writer.Capacity; paddingIndex++)
            writer.Write(paddingIndex % 2 == 0 ? 0xec : 0x11, 8);

        return InterleaveBlocks(writer.Data, totalCodewords, errorCodewords, QrSpecificationTables.ErrorCorrectionBlocks[(int)errorCorrection][version - 1]);
    }

    private static void WriteSegment(BitWriter writer, Segment segment)
    {
        if (segment.Mode == Mode.Numeric)
        {
            for (int index = 0; index < segment.Value.Length; index += 3)
            {
                int length = Math.Min(3, segment.Value.Length - index);
                writer.Write(int.Parse(segment.Value.AsSpan(index, length)), length == 3 ? 10 : length == 2 ? 7 : 4);
            }
        }
        else if (segment.Mode == Mode.AlphaNumeric)
        {
            for (int index = 0; index < segment.Value.Length; index += 2)
            {
                int first = QrEncodingConstants.Alphabet.IndexOf(segment.Value[index]);
                if (index + 1 < segment.Value.Length)
                    writer.Write(first * 45 + QrEncodingConstants.Alphabet.IndexOf(segment.Value[index + 1]), 11);
                else
                    writer.Write(first, 6);
            }
        }
        else
        {
            foreach (byte value in System.Text.Encoding.UTF8.GetBytes(segment.Value))
                writer.Write(value, 8);
        }
    }

    private static byte[] InterleaveBlocks(byte[] data, int totalCodewords, int errorCodewords, int blockCount)
    {
        int shortBlockCount = blockCount - totalCodewords % blockCount;
        int shortDataLength = data.Length / blockCount;
        int errorLength = errorCodewords / blockCount;
        int offset = 0;
        var dataBlocks = new List<byte[]>();
        var errorBlocks = new List<byte[]>();
        for (int block = 0; block < blockCount; block++)
        {
            int length = shortDataLength + (block < shortBlockCount ? 0 : 1);
            byte[] blockData = data[offset..(offset + length)];
            dataBlocks.Add(blockData);
            errorBlocks.Add(ReedSolomonEncoder.Encode(blockData, errorLength));
            offset += length;
        }

        var result = new List<byte>(totalCodewords);
        for (int index = 0; index <= shortDataLength; index++)
            foreach (byte[] block in dataBlocks)
                if (index < block.Length)
                    result.Add(block[index]);
        for (int index = 0; index < errorLength; index++)
            foreach (byte[] block in errorBlocks)
                result.Add(block[index]);
        return result.ToArray();
    }
}
