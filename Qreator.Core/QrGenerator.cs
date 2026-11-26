using Qreator.Core.Svg;

namespace Qreator.Core;

public static class QrGenerator
{
    public static string GenerateQr(string input, SvgOptions? options = null)
    {
        options ??= new SvgOptions();
        var qr = new QR(input, options.QrOptions);
        options.GetQrData?.Invoke(qr);
        return Renderer.GenerateSvg(qr, options);
    }
}
