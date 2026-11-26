using Qreator.Core.Models;

namespace Qreator.Core.Svg;

public static class Renderer
{
    public static string GenerateSvg(QR qr, SvgOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(qr);
        options ??= new SvgOptions();
        SvgDimensions dimensions = options.GetDimensions(qr.GridSize);
        var paths = new SvgPaths();

        for (int index = 0; index < qr.Data.Length; index++)
        {
            if (qr.Data[index] == 0)
                continue;

            int moduleSize = dimensions.ModuleSize;
            var module = new ModuleInfo(index, moduleSize * (index % qr.GridSize + 1), moduleSize * (index / qr.GridSize + 1), moduleSize);
            if (options.ModuleStyle is not null)
                options.ModuleStyle(paths, module, qr);
            else
                AppendDefaultModule(paths, module, qr);
        }

        string additionalContent = options.OnGenerated?.Invoke(paths, dimensions.ModuleSize, qr) ?? "";
        return Composer.Compose(paths, dimensions, options, additionalContent);
    }

    private static void AppendDefaultModule(SvgPaths paths, ModuleInfo module, QR qr)
    {
        var path = qr.ReservedBits.TryGetValue(module.Index, out var info) && info.Type == ReservedBits.FinderPattern
            ? paths.Finder : paths.Codeword;
        path.Append($"M{module.X} {module.Y}v{module.Size}h{module.Size}v-{module.Size}z");
    }
}
