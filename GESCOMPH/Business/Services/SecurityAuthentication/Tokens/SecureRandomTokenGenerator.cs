
using Business.Interfaces.Implements.SecurityAuthentication.Tokens;
using System.Security.Cryptography;

namespace Business.Services.SecurityAuthentication.Tokens
{

    /// <summary>
    /// Generador de tokens aleatorios en formato Base64URL (sin + / =).
    /// </summary>
    public sealed class SecureRandomTokenGenerator : IRandomTokenGenerator
    {
        public string Generate(int bytes)
        {
            var data = RandomNumberGenerator.GetBytes(bytes);
            return Convert.ToBase64String(data)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');
        }
    }
}

