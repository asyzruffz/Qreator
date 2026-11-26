namespace Qreator.Core.Svg.Styles;

internal readonly record struct ModuleNeighbors(
    bool Left, bool Right, bool Top, bool Bottom,
    bool TopLeft, bool TopRight, bool BottomLeft, bool BottomRight)
{
    internal static ModuleNeighbors FromGrid(QR qr, int index)
    {
        int row = index / qr.GridSize;
        int column = index % qr.GridSize;
        bool IsDark(int rowOffset, int columnOffset)
        {
            int neighborRow = row + rowOffset;
            int neighborColumn = column + columnOffset;
            return neighborRow >= 0 && neighborRow < qr.GridSize
                && neighborColumn >= 0 && neighborColumn < qr.GridSize
                && qr.Data[neighborRow * qr.GridSize + neighborColumn] != 0;
        }

        return new ModuleNeighbors(
            IsDark(0, -1), IsDark(0, 1), IsDark(-1, 0), IsDark(1, 0),
            IsDark(-1, -1), IsDark(-1, 1), IsDark(1, -1), IsDark(1, 1));
    }
}
