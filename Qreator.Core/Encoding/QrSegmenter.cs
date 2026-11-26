using Qreator.Core.Models;

namespace Qreator.Core.Encoding;

internal static class QrSegmenter
{
    private sealed record SegmentState(Segment Previous, int Cost, List<Segment> Path);

    internal static List<Segment> BasicSegments(string text)
    {
        var segments = new List<Segment>();
        foreach (char character in text)
        {
            Mode mode = character is >= '0' and <= '9'
                ? Mode.Numeric
                : QrEncodingConstants.Alphabet.Contains(character) ? Mode.AlphaNumeric : Mode.Byte;
            if (segments.Count > 0 && segments[^1].Mode == mode)
                segments[^1] = segments[^1] with { Value = segments[^1].Value + character };
            else
                segments.Add(new Segment(character.ToString(), mode));
        }
        return segments;
    }

    internal static int BitLength(Segment segment) => segment.Mode switch
    {
        Mode.Numeric => segment.Value.Length / 3 * 10 + (segment.Value.Length % 3 == 0 ? 0 : segment.Value.Length % 3 * 3 + 1),
        Mode.AlphaNumeric => segment.Value.Length / 2 * 11 + segment.Value.Length % 2 * 6,
        _ => Encoding.UTF8.GetByteCount(segment.Value) * 8
    };

    internal static List<Segment> Optimize(List<Segment> basicSegments, int versionRange)
    {
        var states = new List<SegmentState>();
        foreach (Segment basicSegment in basicSegments)
        {
            Mode[] compatibleModes = basicSegment.Mode switch
            {
                Mode.Numeric => [Mode.Numeric, Mode.AlphaNumeric, Mode.Byte],
                Mode.AlphaNumeric => [Mode.AlphaNumeric, Mode.Byte],
                _ => [Mode.Byte]
            };
            var nextStates = new List<SegmentState>();
            foreach (Mode mode in compatibleModes)
            {
                var current = new Segment(basicSegment.Value, mode);
                int headerCost = 4 + QrEncodingConstants.CharacterCountBits[(int)mode][versionRange];
                if (states.Count == 0)
                {
                    nextStates.Add(new SegmentState(current, BitLength(current) + headerCost, [current]));
                    continue;
                }

                // Preserve the reference graph's incremental cost for the preceding basic segment.
                var best = states.Select(state => new
                {
                    State = state,
                    Cost = state.Cost + (state.Previous.Mode == mode
                        ? BitLength(new Segment(state.Previous.Value + current.Value, mode)) - BitLength(state.Previous)
                        : BitLength(current) + headerCost)
                }).OrderBy(candidate => candidate.Cost).First();
                var path = new List<Segment>(best.State.Path);
                if (path[^1].Mode == mode)
                    path[^1] = path[^1] with { Value = path[^1].Value + current.Value };
                else
                    path.Add(current);
                nextStates.Add(new SegmentState(current, best.Cost, path));
            }
            states = nextStates;
        }
        return states.OrderBy(state => state.Cost).First().Path;
    }
}
