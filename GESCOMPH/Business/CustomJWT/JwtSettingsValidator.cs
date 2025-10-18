using System.Text;
using Entity.Domain.Models.Implements.SecurityAuthentication;
using Microsoft.Extensions.Options;

namespace Business.CustomJWT
{
    /// <summary>
    /// Valida que JwtSettings cuente con una clave suficientemente fuerte y valores básicos.
    /// </summary>
    public sealed class JwtSettingsValidator : IValidateOptions<JwtSettings>
    {
        public ValidateOptionsResult Validate(string name, JwtSettings options)
        {
            if (options is null)
                return ValidateOptionsResult.Fail("JwtSettings no configurado.");

            if (string.IsNullOrWhiteSpace(options.Key) || Encoding.UTF8.GetByteCount(options.Key) < 32)
                return ValidateOptionsResult.Fail("JwtSettings.Key debe tener al menos 32 caracteres aleatorios (=256 bits).");

            if (string.IsNullOrWhiteSpace(options.Issuer))
                return ValidateOptionsResult.Fail("JwtSettings.Issuer no puede ser vacío.");

            if (string.IsNullOrWhiteSpace(options.Audience))
                return ValidateOptionsResult.Fail("JwtSettings.Audience no puede ser vacío.");

            return ValidateOptionsResult.Success;
        }
    }
}

