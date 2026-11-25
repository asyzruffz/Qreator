# Qreator

Generate QR code grids and SVGs with .NET 10.

```powershell
dotnet run --project Qreator.Cli -- "https://example.com" -o qr.svg
```

Use `--help` for size, error correction, and color options. Without `-o`, the SVG is written to standard output. The reference defaults are a black background and white modules; use `--background white --color black` for the usual dark-on-light appearance.

The core library exposes `QR` for the generated grid and metadata, and `QrSvg.GenerateQr` / `QrSvg.GenerateSvg` for SVG rendering. `SvgOptions` supports separate finder and codeword colors and callbacks for custom module paths and generated content. Numeric, alphanumeric, and UTF-8 byte encoding are supported; as in the reference, Kanji mode is not selected for encoding.
