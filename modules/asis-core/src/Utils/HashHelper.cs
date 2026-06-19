using System.Security.Cryptography;

namespace ASIS.Core.Utils;

public static class HashHelper
{
    public static string ComputeSHA256(string file)
    {
        using var sha = SHA256.Create();

        using var stream = File.OpenRead(file);

        var hash = sha.ComputeHash(stream);

        return Convert.ToHexString(hash);
    }
}
