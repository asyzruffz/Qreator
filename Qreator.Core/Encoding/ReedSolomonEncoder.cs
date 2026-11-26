namespace Qreator.Core.Encoding;

internal static class ReedSolomonEncoder
{
    internal static byte[] Encode(byte[] data, int errorCorrectionLength)
    {
        int[] generator = [1];
        int root = 1;
        for (int degree = 0; degree < errorCorrectionLength; degree++)
        {
            var next = new int[generator.Length + 1];
            for (int index = 0; index < generator.Length; index++)
            {
                next[index] ^= generator[index];
                next[index + 1] ^= Multiply(generator[index], root);
            }
            generator = next;
            root = Multiply(root, 2);
        }

        var encoded = new byte[data.Length + errorCorrectionLength];
        data.CopyTo(encoded, 0);
        for (int index = 0; index < data.Length; index++)
        {
            int coefficient = encoded[index];
            for (int term = 0; term < generator.Length; term++)
                encoded[index + term] ^= (byte)Multiply(generator[term], coefficient);
        }
        return encoded[data.Length..];
    }

    private static int Multiply(int left, int right)
    {
        int result = 0;
        while (right != 0)
        {
            if ((right & 1) != 0)
                result ^= left;
            left <<= 1;
            if ((left & 256) != 0)
                left ^= 0x11d;
            right >>= 1;
        }
        return result;
    }
}
