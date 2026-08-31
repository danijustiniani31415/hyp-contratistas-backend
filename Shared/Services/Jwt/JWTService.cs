using Abril_Backend.Application.DTOs;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Abril_Backend.Infrastructure.Services
{
    public class JwtService : IJWTService
    {
        private readonly IConfiguration _config;

        public JwtService(IConfiguration config)
        {
            _config = config;
        }

        public string GenerateToken(UserDTO user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),

                // Opcionales pero útiles
                new Claim(ClaimTypes.Email, user.Person.Email),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            };

            foreach (var role in user.Roles)
            {
                // El role claim lleva el ID (estable), no el nombre: lo consumen las
                // autorizaciones del backend ([Authorize(Roles=...)], User.IsInRole) y el
                // frontend. Ver Shared/Constants/Roles.cs y core/constants/roles.ts.
                claims.Add(new Claim(ClaimTypes.Role, role.RoleId.ToString()));
                // Nombre solo para mostrar (p. ej. "realizado por rol"); nunca para autorizar.
                claims.Add(new Claim("role_name", role.RoleDescription));
            }

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)
            );

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            
            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}