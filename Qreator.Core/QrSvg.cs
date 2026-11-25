using System.Text;
using System.Security;

namespace Qreator.Core;

public sealed class SvgPaths { public StringBuilder Finder { get; } = new(); public StringBuilder Codeword { get; } = new(); }
public sealed record ModuleInfo(int Index, int X, int Y, int Size);
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
public static class QrSvg
{
    public static string GenerateQr(string input, SvgOptions? options = null)
    {
        options ??= new(); var qr = new QR(input, options.QrOptions); options.GetQrData?.Invoke(qr);
        return GenerateSvg(qr, options);
    }
    public static string GenerateSvg(QR qr, SvgOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(qr); options ??= new();
        if (options.Size <= 0) throw new ArgumentOutOfRangeException(nameof(options), "Size must be positive.");
        int moduleSize = (int)Math.Floor(options.Size / (qr.GridSize + 1.5));
        if (moduleSize == 0) throw new ArgumentOutOfRangeException(nameof(options), "Size is too small for the QR grid.");
        int svgSize = options.Size + (int)Math.Ceiling((double)moduleSize * qr.GridSize - options.Size) + moduleSize * 2;
        var paths = new SvgPaths();
        for (int i = 0; i < qr.Data.Length; i++)
        {
            if (qr.Data[i] == 0) continue;
            var module = new ModuleInfo(i, moduleSize * (i % qr.GridSize + 1), moduleSize * (i / qr.GridSize + 1), moduleSize);
            if (options.ModuleStyle is not null) options.ModuleStyle(paths, module, qr);
            else
            {
                var path = qr.ReservedBits.TryGetValue(i, out var info) && info.Type == ReservedBits.FinderPattern ? paths.Finder : paths.Codeword;
                path.Append($"M{module.X} {module.Y}v{moduleSize}h{moduleSize}v-{moduleSize}z");
            }
        }
        string extra = options.OnGenerated?.Invoke(paths, moduleSize, qr) ?? "";
        static string Escape(string s) => SecurityElement.Escape(s) ?? "";
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"{svgSize}\" width=\"{svgSize}\" viewBox=\"0 0 {svgSize} {svgSize}\" style=\"background: {Escape(options.BackgroundColor)};\"><path id=\"finder\" fill=\"{Escape(options.FinderColor ?? options.Color)}\" d=\"{paths.Finder}\" /><path id=\"codeword\" fill=\"{Escape(options.CodewordColor ?? options.Color)}\" d=\"{paths.Codeword}\" />{extra}</svg>";
    }
}
