using System.Text;

namespace Qreator.Core.Svg;

public sealed class SvgPaths
{
    public StringBuilder Finder { get; } = new();
    public StringBuilder Codeword { get; } = new();
}
