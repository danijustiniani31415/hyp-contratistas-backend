using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Services
{
    /// <summary>
    /// JWT propio de Las Bravas / HP Constructores — el JwtService de Abril está acoplado a
    /// UserDTO (Roles con RoleId/RoleDescription), no sirve para el modelo de
    /// rol+permiso+scope de lb_usuario_asignacion. Reusa la misma llave/issuer/audience
    /// (Jwt:Key/Issuer/Audience) para no duplicar configuración.
    ///
    /// Las asignaciones (rol + proyecto/almacén) y los permisos van como claims JSON serializados
    /// ("lb_asignaciones" / "lb_permisos") porque un usuario puede tener varios roles con distinto
    /// scope a la vez — un ClaimTypes.Role simple no alcanza para eso. El middleware/guard de cada
    /// endpoint debe leer estos dos claims, no asumir un solo rol por usuario.
    /// </summary>
    public class LbJwtService : ILbJwtService
    {
        private readonly IConfiguration _config;

        public LbJwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(LbLoginResponseDto usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.UsuarioSistemaId.ToString()),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(JwtRegisteredClaimNames.Sub, usuario.UsuarioSistemaId.ToString()),
                new Claim("lb_asignaciones", JsonSerializer.Serialize(usuario.Asignaciones)),
                new Claim("lb_permisos", JsonSerializer.Serialize(usuario.Permisos)),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
