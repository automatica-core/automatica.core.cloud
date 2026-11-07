using System;
using System.Security.Cryptography;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Automatica.Core.Cloud.WebApi.Authentication
{
    /// <summary>
    /// Holds the JWT HMAC key. Outside Development/Testing a configured key (Jwt:SigningKey, base64, >= 32 bytes)
    /// is mandatory; locally an ephemeral random key is generated, so tokens do not survive a restart.
    /// </summary>
    public class SigningKeyProvider
    {
        public SigningKeyProvider(JwtOptions options, IHostEnvironment environment, ILogger<SigningKeyProvider> logger = null)
        {
            byte[] bytes = null;
            if (!string.IsNullOrWhiteSpace(options.SigningKey))
            {
                try
                {
                    bytes = Convert.FromBase64String(options.SigningKey.Trim());
                }
                catch (FormatException)
                {
                    throw new InvalidOperationException("Jwt:SigningKey must be base64 encoded");
                }

                if (bytes.Length < 32)
                {
                    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes");
                }
            }
            else if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                bytes = RandomNumberGenerator.GetBytes(64);
                logger?.LogWarning("Jwt:SigningKey is not configured, using an ephemeral key");
            }
            else
            {
                throw new InvalidOperationException("Jwt:SigningKey is not configured");
            }

            Key = new SymmetricSecurityKey(bytes) { KeyId = "acc-1" };
        }

        public SymmetricSecurityKey Key { get; }
    }
}
