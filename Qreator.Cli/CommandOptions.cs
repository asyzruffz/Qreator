using Qreator.Core.Models;
using Qreator.Core.Svg;
using Qreator.Core.Svg.Styles;

namespace Qreator.Cli;

internal sealed record CommandOptions(string Input, string? OutputPath, SvgOptions SvgOptions)
{
    internal static CommandOptions Parse(string[] arguments)
    {
        string? input = null;
        string? outputPath = null;
        var options = new SvgOptions();

        for (int index = 0; index < arguments.Length; index++)
        {
            string ReadValue() => ++index < arguments.Length
                ? arguments[index]
                : throw new ArgumentException($"Missing value for {arguments[index - 1]}.");

            switch (arguments[index])
            {
                case "-o":
                case "--output":
                    outputPath = ReadValue();
                    break;
                case "--size":
                    if (!int.TryParse(ReadValue(), out int size) || size <= 0)
                        throw new ArgumentException("Size must be a positive integer.");
                    options.Size = size;
                    break;
                case "--error-correction":
                    if (!Enum.TryParse<ErrorCorrectionLevel>(ReadValue(), true, out var level) || !Enum.IsDefined(level))
                        throw new ArgumentException("Error correction must be L, M, Q, or H.");
                    options.QrOptions = new QrOptions { ErrorCorrection = level };
                    break;
                case "--style":
                    options.ModuleStyle = ReadValue().ToLowerInvariant() switch
                    {
                        "square" => null,
                        "dot" => ModuleStyles.Dot,
                        "smooth" => ModuleStyles.Smooth,
                        _ => throw new ArgumentException("Style must be square, dot, or smooth.")
                    };
                    break;
                case "--color":
                    options.Color = ReadValue();
                    break;
                case "--background":
                    options.BackgroundColor = ReadValue();
                    break;
                case "--finder-color":
                    options.FinderColor = ReadValue();
                    break;
                case "--codeword-color":
                    options.CodewordColor = ReadValue();
                    break;
                default:
                    if (arguments[index].StartsWith('-'))
                        throw new ArgumentException($"Unknown option: {arguments[index]}");
                    if (input is not null)
                        throw new ArgumentException("Provide a single text argument.");
                    input = arguments[index];
                    break;
            }
        }

        return new CommandOptions(input ?? throw new ArgumentException("Text is required."), outputPath, options);
    }
}
