using Qreator.Core;

namespace Qreator.Cli;

internal static class QrCommand
{
    private const string Usage = "Usage: qreator <text> [-o <file.svg>] [--size <pixels>] [--error-correction L|M|Q|H] [--color <color>] [--background <color>] [--finder-color <color>] [--codeword-color <color>]";

    internal static int Run(string[] arguments, TextWriter output, TextWriter error)
    {
        try
        {
            if (arguments.Length == 0 || arguments.Contains("--help") || arguments.Contains("-h"))
            {
                output.WriteLine(Usage);
                return arguments.Length == 0 ? 1 : 0;
            }

            CommandOptions command = CommandOptions.Parse(arguments);
            string svg = QrGenerator.GenerateQr(command.Input, command.SvgOptions);

            if (command.OutputPath is null)
                output.WriteLine(svg);
            else
                File.WriteAllText(command.OutputPath, svg);
            return 0;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            error.WriteLine(exception.Message);
            error.WriteLine(Usage);
            return 1;
        }
    }
}
