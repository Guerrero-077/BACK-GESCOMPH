
using Business.Interfaces.Implements.SecurityAuthentication.Tokens;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Business.Services.SecurityAuthentication.Tokens
{

    /// <summary>
    /// Calcula HMAC-SHA512 del token usando una "pepper" procedente de <see cref="JwtSettings.Key"/>.
    /// </summary>
    public sealed class HmacTokenHasher : ITokenHasher
    {
        private readonly byte[] _pepper;

        public HmacTokenHasher(IOptions<JwtSettings> settings)
        {
            _pepper = Encoding.UTF8.GetBytes(settings.Value.Key);
        }

        public string Hash(string token)
        {
            using var hmac = new HMACSHA512(_pepper);
            var bytes = Encoding.UTF8.GetBytes(token);
            var mac = hmac.ComputeHash(bytes);
            return Convert.ToHexString(mac).ToLowerInvariant();
        }
    }
}