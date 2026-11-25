using Qreator.Core;

const string usage = "Usage: qreator <text> [-o <file.svg>] [--size <pixels>] [--error-correction L|M|Q|H] [--color <color>] [--background <color>] [--finder-color <color>] [--codeword-color <color>]";
try
{
    if (args.Length == 0 || args.Contains("--help") || args.Contains("-h")) { Console.WriteLine(usage); return args.Length == 0 ? 1 : 0; }
    string? input = null, output = null; var options = new SvgOptions();
    for (int i = 0; i < args.Length; i++)
    {
        string Value() => ++i < args.Length ? args[i] : throw new ArgumentException($"Missing value for {args[i - 1]}.");
        switch (args[i])
        {
            case "-o": case "--output": output = Value(); break;
            case "--size": if (!int.TryParse(Value(), out int size) || size <= 0) throw new ArgumentException("Size must be a positive integer."); options.Size = size; break;
            case "--error-correction": if (!Enum.TryParse<ErrorCorrectionLevel>(Value(), true, out var level) || !Enum.IsDefined(level)) throw new ArgumentException("Error correction must be L, M, Q, or H."); options.QrOptions = new() { ErrorCorrection = level }; break;
            case "--color": options.Color = Value(); break;
            case "--background": options.BackgroundColor = Value(); break;
            case "--finder-color": options.FinderColor = Value(); break;
            case "--codeword-color": options.CodewordColor = Value(); break;
            default: if (args[i].StartsWith('-')) throw new ArgumentException($"Unknown option: {args[i]}"); if (input is not null) throw new ArgumentException("Provide a single text argument."); input = args[i]; break;
        }
    }
    if (input is null) throw new ArgumentException("Text is required.");
    string svg = QrSvg.GenerateQr(input, options);
    if (output is null) Console.WriteLine(svg); else File.WriteAllText(output, svg);
    return 0;
}
catch (Exception e) when (e is ArgumentException or IOException or UnauthorizedAccessException)
{
    Console.Error.WriteLine(e.Message); Console.Error.WriteLine(usage); return 1;
}
