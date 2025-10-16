using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entity.DTOs.Implements.SecurityAuthentication.Auth
{
    /// <summary>
    /// DTO de solicitud para refrescar (rotar) el token de acceso.
    /// </summary>
    public class TokenRefreshRequestDto
    {
        /// <summary>
        /// Refresh token actual (valor plano enviado por el cliente).
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// Dirección IP del cliente, opcional (para auditoría y trazabilidad).
        /// </summary>
        public string? RemoteIp { get; set; }
    }
}
