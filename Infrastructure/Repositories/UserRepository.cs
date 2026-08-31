using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Microsoft.AspNetCore.Identity;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Infrastructure.Repositories {
    public class UserRepository : IUserRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IPasswordHasher<User> _passwordHasher;
        public UserRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory, IPasswordHasher<User> passwordHasher) {
            _context = contexto;
            _factory = factory;
            _passwordHasher = passwordHasher;
        }

        public async Task<List<UserFilterDTO>> GetAllUsersFactory()
        {
            using var ctx = _factory.CreateDbContext();

            var data = await (
                from u in ctx.User
                join p in ctx.Person on u.UserId equals p.UserId
                where u.Active == true
                orderby p.FullName
                select new UserFilterDTO
                {
                    UserId = u.UserId,
                    FullName = p.FullName
                }
            ).ToListAsync();

            return data;
        }

        public async Task<PagedResult<UserDTO>> GetPagedFactory(int page, int pageSizeQuery)
        {
            int pageSize = pageSizeQuery;
            page = page < 1 ? 1 : page;

            using var ctx = _factory.CreateDbContext();

            var query =
                from u in ctx.User
                join p in ctx.Person 
                on u.UserId equals p.UserId
                join dit in ctx.DocumentIdentityType on p.DocumentIdentityTypeId equals dit.DocumentIdentityTypeId
                join ur in ctx.UserRole on u.UserId equals ur.UserId
                join r in ctx.Role on ur.RoleId equals r.RoleId
                where u.State == true
                group new { u, p, dit, r } by new
                {
                    u.UserId,
                    u.Active,
                    p.PersonId,
                    p.DocumentIdentityCode,
                    p.FullName,
                    u.Email,
                    dit.DocumentIdentityTypeId,
                    dit.DocumentIdentityTypeDescription,
                    dit.DocumentIdentityTypeAbbreviation
                }
                into g
                orderby g.Key.UserId descending
                select new UserDTO
                {
                    UserId = g.Key.UserId,
                    Active = g.Key.Active,

                    Person = new PersonDTO
                    {
                        PersonId = g.Key.PersonId,
                        DocumentIdentityCode = g.Key.DocumentIdentityCode,
                        FullName = g.Key.FullName,
                        Email = g.Key.Email,

                        DocumentIdentityType = new DocumentIdentityTypeDTO
                        {
                            DocumentIdentityTypeId = g.Key.DocumentIdentityTypeId,
                            DocumentIdentityTypeDescription = g.Key.DocumentIdentityTypeDescription,
                            DocumentIdentityTypeAbbreviation = g.Key.DocumentIdentityTypeAbbreviation
                        }
                    },

                    Roles = g.Select(x => new RoleSimpleDTO
                    {
                        RoleId = x.r.RoleId,
                        RoleDescription = x.r.RoleDescription
                    }).ToList()
                };

            var totalRecords = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<UserDTO>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = data
            };
        }

        public async Task<User?> Create(UserCreateDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verificar si ya existe una persona con ese documento
                var person = await _context.Person
                    .FirstOrDefaultAsync(p =>
                        p.DocumentIdentityCode == dto.DocumentIdentityCode &&
                        p.State == true
                    );

                if (person != null)
                {
                    // Persona existe — verificar si ya tiene usuario
                    var userExists = await _context.User
                        .AnyAsync(u =>
                            u.UserId == person.UserId &&
                            u.State == true
                        );

                    if (userExists)
                        return null;
                }
                else
                {
                    // Persona no existe — crearla sin usuario aún
                    person = new Person
                    {
                        DocumentIdentityCode = dto.DocumentIdentityCode,
                        DocumentIdentityTypeId = 1,
                        FirstNames = dto.FirstNames,
                        FirstLastName = dto.FirstLastName,
                        SecondLastName = dto.SecondLastName,
                        FullName = $"{dto.FirstNames} {dto.FirstLastName} {dto.SecondLastName}",
                        PhoneNumber = dto.PhoneNumber,
                        Active = true,
                        State = true,
                        CreatedDateTime = DateTime.UtcNow,
                        CreatedUserId = dto.CreatedUserId
                    };

                    _context.Person.Add(person);
                    await _context.SaveChangesAsync();
                }

                // Crear el usuario con el email
                var user = new User
                {
                    Email = dto.Email,
                    Active = false,
                    State = true,
                    EmailConfirmed = false,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = dto.CreatedUserId
                };

                _context.User.Add(user);
                await _context.SaveChangesAsync();

                // Vincular persona con usuario
                person.UserId = user.UserId;
                await _context.SaveChangesAsync();

                // Asignar rol
                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = dto.RoleId,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = dto.CreatedUserId
                };

                _context.UserRole.Add(userRole);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return user;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        public async Task Update(int userId, UserUpdateDTO dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await _context.User
                    .FirstOrDefaultAsync(u => u.UserId == userId && u.State == true);
                if (user == null)
                    throw new AbrilException("Usuario no encontrado.", 404);

                var person = await _context.Person
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.State == true);
                if (person == null)
                    throw new AbrilException("Persona asociada no encontrada.", 404);

                user.Email = dto.Email;
                user.UpdatedDateTime = DateTime.UtcNow;
                user.UpdatedUserId = dto.UpdatedUserId;

                person.FirstNames = dto.FirstNames;
                person.FirstLastName = dto.FirstLastName;
                person.SecondLastName = dto.SecondLastName;
                person.FullName = $"{dto.FirstNames} {dto.FirstLastName} {dto.SecondLastName}";
                person.PhoneNumber = dto.PhoneNumber;
                person.UpdatedDateTime = DateTime.UtcNow;
                person.UpdatedUserId = dto.UpdatedUserId;

                await _context.SaveChangesAsync();

                var existingRoles = await _context.UserRole
                    .Where(ur => ur.UserId == userId && ur.State == true)
                    .ToListAsync();

                foreach (var r in existingRoles)
                {
                    r.State = false;
                    r.Active = false;
                    r.UpdatedDateTime = DateTime.UtcNow;
                    r.UpdatedUserId = dto.UpdatedUserId;
                }

                _context.UserRole.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = dto.RoleId,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = dto.UpdatedUserId
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (AbrilException)
            {
                await transaction.RollbackAsync();
                throw;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task ToggleActive(int userId, int updatedUserId)
        {
            using var ctx = _factory.CreateDbContext();
            var user = await ctx.User
                .FirstOrDefaultAsync(u => u.UserId == userId && u.State == true);
            if (user == null)
                throw new AbrilException("Usuario no encontrado.", 404);

            user.Active = !user.Active;
            user.UpdatedDateTime = DateTime.UtcNow;
            user.UpdatedUserId = updatedUserId;

            await ctx.SaveChangesAsync();
        }

        public async Task SetPassword(int userId, string plainPassword)
        {
            var user = await _context.User.FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                throw new Exception("Usuario no encontrado.");

            user.Password = _passwordHasher.HashPassword(user, plainPassword);

            user.Active = true;
            user.EmailConfirmed = true;
            user.UpdatedDateTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<UserFilterDTO>> GetResidentsFullName()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = from user in ctx.User
                join user_role in ctx.UserRole on user.UserId equals user_role.UserId
                join role in ctx.Role on user_role.RoleId equals role.RoleId
                join person in ctx.Person on user.UserId equals person.UserId
                where (role.RoleDescription == "RESIDENTE") && (person.State == true) && (person.Active == true)
                && (user.State == true) && (user.Active == true)
                select new UserFilterDTO
                {
                    UserId = user.UserId,
                    FullName = person.FullName
                };

            return await registros.Distinct().ToListAsync();
        }
    }
}