using Qreator.Core.Models;
using Qreator.Core.Rendering;
using System.Security;

namespace Qreator.Core;

public static class QrSvg
{
    public static string GenerateQr(string input, SvgOptions? options = null)
    {
        options ??= new SvgOptions();
        var qr = new QR(input, options.QrOptions);
        options.GetQrData?.Invoke(qr);
        return GenerateSvg(qr, options);
    }

    public static string GenerateSvg(QR qr, SvgOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(qr);
        options ??= new SvgOptions();
        if (options.Size <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Size must be positive.");
        int moduleSize = (int)Math.Floor(options.Size / (qr.GridSize + 1.5));
        if (moduleSize == 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Size is too small for the QR grid.");

        int svgSize = options.Size + (int)Math.Ceiling((double)moduleSize * qr.GridSize - options.Size) + moduleSize * 2;
        var paths = new SvgPaths();
        for (int index = 0; index < qr.Data.Length; index++)
        {
            if (qr.Data[index] == 0)
                continue;
            var module = new ModuleInfo(index, moduleSize * (index % qr.GridSize + 1), moduleSize * (index / qr.GridSize + 1), moduleSize);
            if (options.ModuleStyle is not null)
                options.ModuleStyle(paths, module, qr);
            else
                AppendDefaultModule(paths, module, qr);
        }

        string additionalContent = options.OnGenerated?.Invoke(paths, moduleSize, qr) ?? "";
        string backgroundColor = Escape(options.BackgroundColor);
        string finderColor = Escape(options.FinderColor ?? options.Color);
        string codewordColor = Escape(options.CodewordColor ?? options.Color);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"{svgSize}\" width=\"{svgSize}\" viewBox=\"0 0 {svgSize} {svgSize}\" style=\"background: {backgroundColor};\"><path id=\"finder\" fill=\"{finderColor}\" d=\"{paths.Finder}\" /><path id=\"codeword\" fill=\"{codewordColor}\" d=\"{paths.Codeword}\" />{additionalContent}</svg>";
    }

    private static void AppendDefaultModule(SvgPaths paths, ModuleInfo module, QR qr)
    {
        var path = qr.ReservedBits.TryGetValue(module.Index, out var info) && info.Type == ReservedBits.FinderPattern
            ? paths.Finder : paths.Codeword;
        path.Append($"M{module.X} {module.Y}v{module.Size}h{module.Size}v-{module.Size}z");
    }

    private static string Escape(string value) => SecurityElement.Escape(value) ?? "";
}
