using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Infrastructure.Interfaces;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Dtos;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.AuthModule.MicrosoftLogin.Infrastructure.Repositories
{
    public class MicrosoftLoginRepository : IMicrosoftLoginRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public MicrosoftLoginRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<UserDTO?> GetUserByEmailAsync(string email)
        {
            using var ctx = _factory.CreateDbContext();

            var query =
                from u in ctx.User
                join p in ctx.Person on u.UserId equals p.UserId into pGroup   // LEFT JOIN
                from p in pGroup.DefaultIfEmpty()
                join ur in ctx.UserRole on u.UserId equals ur.UserId into urGroup
                from ur in urGroup.DefaultIfEmpty()
                join r in ctx.Role on ur.RoleId equals r.RoleId into rGroup
                from r in rGroup.DefaultIfEmpty()
                where u.Email.ToLower() == email.ToLower() && u.Active && u.State
                group new { u, p, r } by new
                {
                    u.UserId,
                    u.Active,
                    u.Email,
                    PersonId             = (int?)   (p == null ? null : (int?)p.PersonId),
                    DocumentIdentityCode = (string?)(p == null ? null : p.DocumentIdentityCode),
                    FullName             = (string?)(p == null ? null : p.FullName)
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

            return new UserDTO
            {
                UserId = result.Key.UserId,
                Active = result.Key.Active,
                Person = new PersonDTO
                {
                    PersonId             = result.Key.PersonId ?? 0,
                    DocumentIdentityCode = result.Key.DocumentIdentityCode,
                    FullName             = result.Key.FullName ?? string.Empty,
                    Email                = result.Key.Email ?? string.Empty
                },
                Roles = result.Roles
            };
        }

        public async Task<PersonDTO?> GetPersonByWorkerEmailAsync(string email)
        {
            using var ctx = _factory.CreateDbContext();

            var emailLower = email.ToLower();

            return await ctx.Worker
                .Where(w => w.Person != null
                         && w.EmailCorporativo != null
                         && w.EmailCorporativo.ToLower() == emailLower)
                .Select(w => new PersonDTO
                {
                    PersonId             = w.Person!.PersonId,
                    DocumentIdentityCode = w.Person.DocumentIdentityCode,
                    FullName             = w.Person.FullName ?? string.Empty,
                    Email                = email
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserDTO> CreateUserAndLinkPersonAsync(MicrosoftProfileDto profile, int personId)
        {
            using var ctx = _factory.CreateDbContext();
            var strategy = ctx.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await ctx.Database.BeginTransactionAsync();

                var email = profile.Mail ?? profile.UserPrincipalName;

                var user = new User
                {
                    Email           = email,
                    Password        = null,
                    EmailConfirmed  = true,
                    Active          = true,
                    State           = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId   = null
                };

                ctx.User.Add(user);
                await ctx.SaveChangesAsync();

                var person = await ctx.Person.FindAsync(personId)
                    ?? throw new AbrilException("Persona no encontrada.", 404);

                person.UserId          = user.UserId;
                person.UpdatedDateTime = DateTime.UtcNow;
                await ctx.SaveChangesAsync();

                await transaction.CommitAsync();

                return new UserDTO
                {
                    UserId = user.UserId,
                    Active = user.Active,
                    Person = new PersonDTO
                    {
                        PersonId             = person.PersonId,
                        DocumentIdentityCode = person.DocumentIdentityCode,
                        FullName             = person.FullName ?? string.Empty,
                        Email                = email ?? string.Empty
                    },
                    Roles = new()
                };
            });
        }

        public async Task<PersonDTO> LinkPersonToUserAsync(int userId, int personId, string email)
        {
            using var ctx = _factory.CreateDbContext();

            var person = await ctx.Person.FindAsync(personId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            person.UserId          = userId;
            person.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();

            return new PersonDTO
            {
                PersonId             = person.PersonId,
                DocumentIdentityCode = person.DocumentIdentityCode,
                FullName             = person.FullName ?? string.Empty,
                Email                = email
            };
        }

        public async Task<PersonDTO> CreatePersonForUserAsync(int userId, MicrosoftProfileDto profile)
        {
            using var ctx = _factory.CreateDbContext();

            var person = new Person
            {
                UserId                 = userId,
                DocumentIdentityTypeId = null,
                DocumentIdentityCode   = null,
                FirstNames             = profile.GivenName?.ToUpper(),
                FirstLastName          = profile.Surname?.ToUpper(),
                FullName               = profile.DisplayName?.ToUpper() ?? string.Empty,
                Active                 = true,
                State                  = true,
                CreatedDateTime        = DateTime.UtcNow,
                CreatedUserId          = null
            };

            ctx.Person.Add(person);
            await ctx.SaveChangesAsync();

            return new PersonDTO
            {
                PersonId             = person.PersonId,
                DocumentIdentityCode = null,
                FullName             = person.FullName,
                Email                = profile.Mail ?? profile.UserPrincipalName ?? string.Empty
            };
        }

        public async Task<RoleSimpleDTO?> AssignRoleAsync(int userId, int roleId)
        {
            using var ctx = _factory.CreateDbContext();

            var role = await ctx.Role.FirstOrDefaultAsync(r => r.RoleId == roleId);
            if (role is null) return null;

            var yaExiste = await ctx.UserRole
                .AnyAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

            if (!yaExiste)
            {
                ctx.UserRole.Add(new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = userId
                });
                await ctx.SaveChangesAsync();
            }

            return new RoleSimpleDTO
            {
                RoleId = role.RoleId,
                RoleDescription = role.RoleDescription
            };
        }

        public async Task<string?> GetWorkerAreaByPersonIdAsync(int personId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Worker
                .Where(w => w.PersonId == personId && w.WorkersEstadoId == WorkersEstadoIds.Activo)
                .Select(w => w.Area)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsAreaRevisorByPersonIdAsync(int personId)
        {
            using var ctx = _factory.CreateDbContext();

            // State && Active: mismo criterio con el que JefeRevisorResolver elige al revisor
            // de una solicitud y con el que SalidaVisibilityResolver le da visibilidad de su
            // área. No se filtra por Worker.Estado a propósito: la designación vive en
            // area_revisores y es la que manda, igual que en esos dos resolvers.
            return await (
                from r in ctx.AreaRevisores.AsNoTracking()
                where r.State && r.Active
                join w in ctx.Worker.AsNoTracking() on r.RevisorId equals w.Id
                where w.PersonId == personId
                select r.AreaRevisoresId
            ).AnyAsync();
        }

        public async Task<UserDTO> CreateUserFromGraphAsync(MicrosoftProfileDto profile)
        {
            using var ctx = _factory.CreateDbContext();
            var strategy = ctx.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await ctx.Database.BeginTransactionAsync();

                var email = profile.Mail ?? profile.UserPrincipalName;

                var user = new User
                {
                    Email = email,
                    Password = null,
                    EmailConfirmed = true,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = null
                };

                ctx.User.Add(user);
                await ctx.SaveChangesAsync();

                var person = new Person
                {
                    UserId = user.UserId,
                    DocumentIdentityTypeId = null,
                    DocumentIdentityCode = null,
                    FirstNames = profile.GivenName?.ToUpper(),
                    FirstLastName = profile.Surname?.ToUpper(),
                    FullName = profile.DisplayName.ToUpper(),
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = null
                };

                ctx.Person.Add(person);
                await ctx.SaveChangesAsync();

                await transaction.CommitAsync();

                return new UserDTO
                {
                    UserId = user.UserId,
                    Active = user.Active,
                    Person = new PersonDTO
                    {
                        PersonId = person.PersonId,
                        DocumentIdentityCode = null,
                        FullName = person.FullName,
                        Email = email
                    },
                    Roles = new()
                };
            });
        }
    }
}
