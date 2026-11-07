using System;
using System.Security.Cryptography;
using System.Text;

namespace Automatica.Core.Cloud.EF.Models
{
    /// <summary>
    /// High-entropy machine secrets (core server api keys, deploy keys, refresh tokens, verification tokens)
    /// are stored as SHA-256 only.
    /// </summary>
    public static class SecretHashing
    {
        public static byte[] Sha256(string value)
        {
            return SHA256.HashData(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>Core api keys are GUIDs; hash their canonical lowercase "D" form so any casing matches.</summary>
        public static byte[] HashApiKey(Guid apiKey)
        {
            return Sha256(apiKey.ToString("D"));
        }

        public static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            return a != null && b != null && CryptographicOperations.FixedTimeEquals(a, b);
        }

        public static string NewToken(string prefix, int bytes = 32)
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(bytes)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return prefix + raw;
        }
    }
}
