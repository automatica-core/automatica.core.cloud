using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace Automatica.Core.Cloud.EF.Models
{
    public enum UserRole
    {
        Nothing,
        Viewer,
        Manager,
        Administrator,
        SystemAdministrator
    }
    public class User
    {
        [Key]
        public Guid ObjId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }

        public string PublisherName { get; set; }

        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public bool Enabled { get; set; }
        public string ActivationCode { get; set; }

        public UserRole UserRole { get; set; }

        public Guid ApiKey { get; set; }

        [NotMapped]
        public string Token { get; set; }

        public static string HashPassword(string password, string saltString)
        {
            byte[] salt = Convert.FromBase64String(saltString);

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA1,
                10000,
                256 / 8));
            return hashed;
        }

        public static string GenerateNewSalt()
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }
    }
}
