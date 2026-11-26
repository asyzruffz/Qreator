using System.Text;

namespace Qreator.Core.Svg.Styles;

internal static class DotPath
{
    internal static void Append(StringBuilder path, ModuleInfo module)
    {
        int radius = module.Size / 2;
        int centerX = module.X + radius;
        int centerY = module.Y + radius;
        path.Append($"M{centerX - radius} {centerY}a {radius} {radius} 0 1 0 {radius * 2} 0a {radius} {radius} 0 1 0 -{radius * 2} 0");
    }
}
