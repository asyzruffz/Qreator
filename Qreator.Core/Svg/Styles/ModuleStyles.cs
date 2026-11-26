using Qreator.Core.Models;
using System.Text;

namespace Qreator.Core.Svg.Styles;

public static class ModuleStyles
{
    public static void Dot(SvgPaths paths, ModuleInfo module, QR qr)
    {
        DotPath.Append(SelectPath(paths, module, qr), module);
    }

    public static void Smooth(SvgPaths paths, ModuleInfo module, QR qr)
    {
        SmoothPath.Append(SelectPath(paths, module, qr), module, qr);
    }

    private static StringBuilder SelectPath(SvgPaths paths, ModuleInfo module, QR qr) =>
        qr.ReservedBits.TryGetValue(module.Index, out var info) && info.Type == ReservedBits.FinderPattern
            ? paths.Finder : paths.Codeword;
}
