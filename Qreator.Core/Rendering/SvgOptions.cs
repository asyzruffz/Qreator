using Qreator.Core.Models;

namespace Qreator.Core.Rendering;

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
}
