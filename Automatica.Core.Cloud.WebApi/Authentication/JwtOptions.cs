namespace Automatica.Core.Cloud.WebApi.Authentication
{
    public class JwtOptions
    {
        public const string Section = "Jwt";

        public string Issuer { get; set; } = "automatica-cloud";
        public string Audience { get; set; } = "automatica-cloud-web";

        /// <summary>Base64 encoded HMAC key, at least 32 bytes. Comes from Key Vault (Jwt--SigningKey).</summary>
        public string SigningKey { get; set; }

        public int AccessTokenMinutes { get; set; } = 15;
        public int RefreshTokenDays { get; set; } = 14;
    }
}
