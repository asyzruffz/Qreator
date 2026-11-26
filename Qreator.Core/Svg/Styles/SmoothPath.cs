using System.Globalization;
using System.Text;

namespace Qreator.Core.Svg.Styles;

internal static class SmoothPath
{
    internal static void Append(StringBuilder path, ModuleInfo module, QR qr)
    {
        ModuleNeighbors neighbors = ModuleNeighbors.FromGrid(qr, module.Index);
        double radius = module.Size * 0.5;
        string distance = Format(radius);
        int x = module.X;
        int y = module.Y;
        int size = module.Size;

        if (!neighbors.Left && neighbors.TopLeft)
            path.Append($"M{x} {y} v{distance}q0 -{distance} -{distance} -{distance}z");
        if (!neighbors.Left && neighbors.BottomLeft)
            path.Append($"M{x} {y + size} v-{distance}q0 {distance} -{distance} {distance}z");
        if (!neighbors.Right && neighbors.TopRight)
            path.Append($"M{x + size} {y} v{distance}q0 -{distance} {distance} -{distance}z");
        if (!neighbors.Right && neighbors.BottomRight)
            path.Append($"M{x + size} {y + size} h{distance}q-{distance} 0 -{distance} -{distance}z");

        bool topLeft = !neighbors.TopLeft && !neighbors.Top && !neighbors.Left;
        bool topRight = !neighbors.TopRight && !neighbors.Top && !neighbors.Right;
        bool bottomLeft = !neighbors.BottomLeft && !neighbors.Bottom && !neighbors.Left;
        bool bottomRight = !neighbors.BottomRight && !neighbors.Bottom && !neighbors.Right;
        if (!topLeft && !topRight && !bottomLeft && !bottomRight)
        {
            path.Append($"M{x} {y}v{size}h{size}v-{size}z");
            return;
        }

        path.Append($"M{Format(topLeft ? x + radius : x)} {y}");
        if (topLeft)
            path.Append($"q-{distance} 0 -{distance} {distance}");
        if (bottomLeft)
            path.Append($"v{Format(topLeft ? size - 2 * radius : size - radius)} q0 {distance} {distance} {distance}");
        else
            path.Append($"v{Format(topLeft ? size - radius : size)}");
        if (bottomRight)
            path.Append($"h{Format(bottomLeft ? size - 2 * radius : size - radius)} q{distance} 0 {distance} -{distance}");
        else
            path.Append($"h{Format(bottomLeft ? size - radius : size)}");
        if (topRight)
            path.Append($"v-{Format(bottomRight ? size - 2 * radius : size - radius)} q0 -{distance} -{distance} -{distance}");
        else
            path.Append($"v-{Format(bottomRight ? size - radius : size)}");
        path.Append('z');
    }

    private static string Format(double value) => value.ToString(CultureInfo.InvariantCulture);
}
