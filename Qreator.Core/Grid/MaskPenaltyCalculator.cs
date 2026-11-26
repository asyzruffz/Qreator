namespace Qreator.Core.Grid;

internal static class MaskPenaltyCalculator
{
    private static readonly byte[] FinderPatternBeforeLight = [1, 0, 1, 1, 1, 0, 1, 0, 0, 0, 0];
    private static readonly byte[] FinderPatternAfterLight = [0, 0, 0, 0, 1, 0, 1, 1, 1, 0, 1];

    internal static int Calculate(byte[] data, int size)
    {
        int score = 0;
        for (int axis = 0; axis < 2; axis++)
        {
            for (int line = 0; line < size; line++)
            {
                byte At(int position) => axis == 0 ? data[line * size + position] : data[position * size + line];
                int runLength = 1;
                for (int position = 1; position < size; position++)
                {
                    if (At(position) == At(position - 1))
                        runLength++;
                    else
                    {
                        if (runLength >= 5)
                            score += runLength - 2;
                        runLength = 1;
                    }
                }
                if (runLength >= 5)
                    score += runLength - 2;

                for (int position = 0; position <= size - FinderPatternBeforeLight.Length; position++)
                {
                    bool matchesBefore = true;
                    bool matchesAfter = true;
                    for (int offset = 0; offset < FinderPatternBeforeLight.Length; offset++)
                    {
                        matchesBefore &= At(position + offset) == FinderPatternBeforeLight[offset];
                        matchesAfter &= At(position + offset) == FinderPatternAfterLight[offset];
                    }
                    if (matchesBefore || matchesAfter)
                        score += 40;
                }
            }
        }

        for (int row = 0; row < size - 1; row++)
        {
            for (int column = 0; column < size - 1; column++)
            {
                int index = row * size + column;
                if (data[index] == data[index + 1] && data[index] == data[index + size] && data[index] == data[index + size + 1])
                    score += 3;
            }
        }
        double darkPercentage = data.Sum(module => (int)module) * 100.0 / data.Length;
        double lowerDeviation = Math.Abs(Math.Floor(darkPercentage / 5) * 5 - 50);
        double upperDeviation = Math.Abs(Math.Ceiling(darkPercentage / 5) * 5 - 50);
        return score + (int)(Math.Min(lowerDeviation, upperDeviation) / 5 * 10);
    }
}
