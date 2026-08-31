using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;

namespace Abril_Backend.Infrastructure.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AuthRepository(AppDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        public async Task<UserDTO?> ValidateUserAsync(string email, string password)
        {
            var query =
                from u in _context.User
                join ur in _context.UserRole on u.UserId equals ur.UserId into urGroup
                from ur in urGroup.DefaultIfEmpty()
                join r in _context.Role on ur.RoleId equals r.RoleId into rGroup
                from r in rGroup.DefaultIfEmpty()
                where u.Email == email &&
                      u.Active &&
                      u.State
                group new { u, r } by new
                {
                    u.UserId,
                    u.Password,
                    u.Active,
                    u.Email
                }
                into g
                select new
                {
                    g.Key,
                    Roles = g.Where(x => x.r != null)
                              .Select(x => new RoleSimpleDTO
                              {
                                  RoleId = x.r.RoleId,
                                  RoleDescription = x.r.RoleDescription
                              }).ToList()
                };

            var result = await query.FirstOrDefaultAsync();

            if (result == null)
                return null;

            var passwordHasher = new PasswordHasher<object>();

            var verify = passwordHasher.VerifyHashedPassword(
                new object(),
                result.Key.Password!,
                password
            );

            if (verify != PasswordVerificationResult.Success)
                return null;

            return new UserDTO
            {
                UserId = result.Key.UserId,
                Active = result.Key.Active,
                Person = new PersonDTO
                {
                    Email = result.Key.Email
                },
                Roles = result.Roles
            };
        }

        public async Task<UserSession> CreateSessionAsync(int userId)
        {
            var token = GenerateToken();

            var session = new UserSession
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(24),
                Revoked = false,
                CreatedDateTime = DateTime.UtcNow
            };

            await _context.UserSession.AddAsync(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<(int UserId, string Email)?> GetUserByEmailAsync(string email)
        {
            var result = await _context.User
                .Where(u => u.Email == email && u.Active && u.State)
                .Select(u => new { u.UserId, u.Email })
                .FirstOrDefaultAsync();

            if (result == null)
                return null;

            return (result.UserId, result.Email);
        }

        public async Task<(int UserId, string Email)?> GetUserByIdAsync(int userId)
        {
            var result = await _context.User
                .Where(u => u.UserId == userId && u.State)
                .Select(u => new { u.UserId, u.Email })
                .FirstOrDefaultAsync();

            if (result == null)
                return null;

            return (result.UserId, result.Email);
        }

        public async Task<int?> GetUserIdByValidSessionAsync(string sessionToken)
        {
            var now = DateTime.UtcNow;
            return await _context.UserSession
                .Where(s => s.Token == sessionToken && !s.Revoked && s.ExpiresAt > now)
                .Select(s => (int?)s.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<UserDTO?> GetUserForTokenAsync(int userId)
        {
            // Mismo armado que ValidateUserAsync pero por userId: trae el email y los
            // roles ACTUALES para regenerar el JWT con datos frescos en cada refresh.
            var query =
                from u in _context.User
                join ur in _context.UserRole on u.UserId equals ur.UserId into urGroup
                from ur in urGroup.DefaultIfEmpty()
                join r in _context.Role on ur.RoleId equals r.RoleId into rGroup
                from r in rGroup.DefaultIfEmpty()
                where u.UserId == userId && u.Active && u.State
                group new { u, r } by new { u.UserId, u.Email } into g
                select new
                {
                    g.Key,
                    Roles = g.Where(x => x.r != null)
                              .Select(x => new RoleSimpleDTO
                              {
                                  RoleId = x.r.RoleId,
                                  RoleDescription = x.r.RoleDescription
                              }).ToList()
                };

            var result = await query.FirstOrDefaultAsync();
            if (result == null)
                return null;

            return new UserDTO
            {
                UserId = result.Key.UserId,
                Active = true,
                Person = new PersonDTO { Email = result.Key.Email },
                Roles = result.Roles
            };
        }

        public async Task<List<string>> GetAllowedFeaturesAsync(int userId)
        {
            try
            {
                // Se respeta el soft delete de las dos tablas de la cadena: una asignación
                // dada de baja (user_role.state = false) o un rol dado de baja (role.state =
                // false) NO conceden la feature. Sin estos filtros, quitarle un rol a alguien
                // no le quitaba el acceso: seguía entrando hasta que la fila se borrara duro.
                // feature y role_feature no tienen columna state, no hay nada que filtrar ahí.
                //
                // Excepción del rol TESORERO: ver la nota de abajo — es el único rol cuyas
                // features dependen además del puesto del trabajador.
                // El rol TESORERO es el único que además exige un puesto: sus features solo se
                // conceden si alguno de los workers vivos del usuario tiene un puesto de categoría
                // Tesorero. Tener el rol sin ese puesto no abre nada, y el puesto sin el rol
                // tampoco. Va en la misma consulta y no en un post-filtro en C# para no sumar un
                // roundtrip al login, que es el camino más caliente de la app.
                //
                // Las features que ese usuario también recibe por OTRO rol no se ven afectadas: el
                // recorte es por fila de role_feature, no por feature.
                var rolTesorero = int.Parse(Shared.Constants.Roles.Tesorero);
                var categoriaTesorero = Shared.Constants.CategoriaIds.Tesorero;

                return await _context.Database
                    .SqlQuery<string>($"""
                        SELECT DISTINCT f.feature_key
                        FROM feature f
                        JOIN role_feature rf ON rf.feature_id = f.feature_id
                        JOIN user_role ur     ON ur.role_id   = rf.role_id
                        JOIN role r           ON r.role_id    = ur.role_id
                        WHERE ur.user_id = {userId}
                          AND ur.state
                          AND r.state
                          AND (
                                r.role_id <> {rolTesorero}
                             OR EXISTS (
                                    SELECT 1
                                    FROM workers w
                                    JOIN person p  ON p.person_id  = w.person_id
                                    JOIN puesto pu ON pu.puesto_id = w.puesto_id
                                    WHERE p.user_id = {userId}
                                      AND w.state
                                      AND pu.categoria_id = {categoriaTesorero}
                                )
                          )
                        """)
                    .ToListAsync();
            }
            catch
            {
                return new List<string>();
            }
        }

        private string GenerateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }
    }
}