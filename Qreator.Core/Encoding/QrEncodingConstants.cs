namespace Qreator.Core.Encoding;

internal static class QrEncodingConstants
{
    internal const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:";
    internal static readonly int[][] CharacterCountBits = [[10, 12, 14], [9, 11, 13], [8, 16, 16], [8, 10, 12]];
    internal static int VersionRange(int version) => version <= 9 ? 0 : version <= 26 ? 1 : 2;
}
