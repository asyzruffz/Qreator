using Qreator.Core.Models;

namespace Qreator.Core.Svg;

public sealed class SvgOptions
{
    public int Size { get; set; } = 400;
    public QrOptions? QrOptions { get; set; }
    public string BackgroundColor { get; set; } = "black";
    public string Color { get; set; } = "white";
    public string? FinderColor { get; set; }
    public string? CodewordColor { get; set; }
    public Action<SvgPaths, ModuleInfo, QR>? ModuleStyle { get; set; }
    public Action<QR>? GetQrData { get; set; }
    public Func<SvgPaths, int, QR, string?>? OnGenerated { get; set; }

    internal string EffectiveFinderColor => FinderColor ?? Color;
    internal string EffectiveCodewordColor => CodewordColor ?? Color;

    internal SvgDimensions GetDimensions(int gridSize)
    {
        if (Size <= 0)
            throw new ArgumentOutOfRangeException(nameof(Size), "Size must be positive.");

        int moduleSize = (int)Math.Floor(Size / (gridSize + 1.5));
        if (moduleSize == 0)
            throw new ArgumentOutOfRangeException(nameof(Size), "Size is too small for the QR grid.");

        int svgSize = Size + (int)Math.Ceiling((double)moduleSize * gridSize - Size) + moduleSize * 2;
        return new SvgDimensions(moduleSize, svgSize);
    }
}
