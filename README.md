# Qreator

Generate QR code grids in SVGs with .NET 10.

```powershell
Qreator "https://example.com" -o qr.svg
```

or

```powershell
dotnet run --project Qreator.Cli -- "https://example.com" -o qr.svg
```

Use `--help` for size, error correction, style, and color options. Use `--style dot` or `--style smooth` to change module shapes (the default is `square`). Without `-o`, the SVG is written to standard output. The reference defaults are a black background and white modules; use `--background white --color black` for the usual dark-on-light appearance.

The core library exposes `QR` for the generated grid and metadata, and `QrGenerator.GenerateQr` / `Svg.Renderer.GenerateSvg` for SVG rendering. `SvgOptions` supports separate finder and codeword colors and callbacks for custom module paths and generated content. Numeric, alphanumeric, and UTF-8 byte encoding are supported; as in the reference, Kanji mode is not selected for encoding.

Built-in SVG styles are also available through `SvgOptions.ModuleStyle`: assign `ModuleStyles.Dot` or `ModuleStyles.Smooth` from `Qreator.Core.Svg.Styles`. Custom callbacks remain supported.
