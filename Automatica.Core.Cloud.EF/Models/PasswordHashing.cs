using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

namespace Automatica.Core.Cloud.EF.Models
{
    /// <summary>
    /// Password hashes are stored as "v2$iterations$base64" (PBKDF2-HMAC-SHA512, salt in User.Salt).
    /// The legacy format is a bare base64 PBKDF2-HMAC-SHA1 (10k iterations) hash; it is still verified
    /// and upgraded on the next successful login.
    /// A hash starting with "!" never matches (used to lock accounts that carry a known/leaked password).
    /// </summary>
    public static class PasswordHashing
    {
        public const int Iterations = 210_000;
        public const int MinPasswordLength = 12;
        private const string V2Prefix = "v2$";
        public const string UnusablePrefix = "!";

        public static string GenerateSalt()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        }

        public static string Hash(string password, string salt)
        {
            var bytes = KeyDerivation.Pbkdf2(password, Convert.FromBase64String(salt), KeyDerivationPrf.HMACSHA512, Iterations, 32);
            return $"{V2Prefix}{Iterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(bytes)}";
        }

        public static string LegacyHash(string password, string salt)
        {
            return Convert.ToBase64String(KeyDerivation.Pbkdf2(password, Convert.FromBase64String(salt), KeyDerivationPrf.HMACSHA1, 10000, 32));
        }

        public static bool IsUsable(string hash)
        {
            return !string.IsNullOrEmpty(hash) && !hash.StartsWith(UnusablePrefix, StringComparison.Ordinal);
        }

        public static bool IsLegacy(string hash)
        {
            return IsUsable(hash) && !hash.StartsWith(V2Prefix, StringComparison.Ordinal);
        }

        public static string Unusable()
        {
            return UnusablePrefix + Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        }

        public static bool Verify(string password, string salt, string storedHash)
        {
            if (password == null || string.IsNullOrEmpty(salt) || !IsUsable(storedHash))
            {
                return false;
            }

            string computed;
            if (storedHash.StartsWith(V2Prefix, StringComparison.Ordinal))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 3 || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations < 10_000)
                {
                    return false;
                }
                var bytes = KeyDerivation.Pbkdf2(password, Convert.FromBase64String(salt), KeyDerivationPrf.HMACSHA512, iterations, 32);
                computed = $"{V2Prefix}{parts[1]}${Convert.ToBase64String(bytes)}";
            }
            else
            {
                computed = LegacyHash(password, salt);
            }

            return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computed), Encoding.ASCII.GetBytes(storedHash));
        }

        /// <summary>
        /// Runs a full hash so a login for an unknown user costs as much time as one for a known user.
        /// </summary>
        public static void DummyVerify(string password)
        {
            Hash(password ?? string.Empty, "AAAAAAAAAAAAAAAAAAAAAA==");
        }

        public static string GeneratePassword(int length = 24)
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789-_.";
            var chars = new char[length];
            for (var i = 0; i < length; i++)
            {
                chars[i] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
            }
            return new string(chars);
        }
    }
}
