using System.Security;

namespace Qreator.Core.Svg;

internal static class Composer
{
    internal static string Compose(SvgPaths paths, SvgDimensions dimensions, SvgOptions options, string additionalContent)
    {
        int svgSize = dimensions.SvgSize;
        string backgroundColor = Escape(options.BackgroundColor);
        string finderColor = Escape(options.EffectiveFinderColor);
        string codewordColor = Escape(options.EffectiveCodewordColor);
        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"{svgSize}\" width=\"{svgSize}\" viewBox=\"0 0 {svgSize} {svgSize}\" style=\"background: {backgroundColor};\"><path id=\"finder\" fill=\"{finderColor}\" d=\"{paths.Finder}\" /><path id=\"codeword\" fill=\"{codewordColor}\" d=\"{paths.Codeword}\" />{additionalContent}</svg>";
    }

    private static string Escape(string value) => SecurityElement.Escape(value) ?? string.Empty;
}
