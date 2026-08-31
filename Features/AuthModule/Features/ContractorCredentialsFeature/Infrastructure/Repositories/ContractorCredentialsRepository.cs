using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Dtos;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Services.Contractors;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.AuthModule.ContractorCredentials.Infrastructure.Repositories
{
    public class ContractorCredentialsRepository : IContractorCredentialsRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ContractorCredentialsRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<ContractorForCredentialsDto?> GetByToken(string token)
        {
            using var ctx = _factory.CreateDbContext();

            var result = await (
                from ct in ctx.Contractor
                join c in ctx.Contributor on ct.ContributorId equals c.ContributorId
                where ct.ActivationToken == token
                      && ct.ActivationTokenExpiry > DateTime.UtcNow
                      && ct.Active
                select new ContractorForCredentialsDto
                {
                    ContractorId = ct.ContractorId,
                    ContributorName = c.ContributorName
                }
            ).FirstOrDefaultAsync();

            if (result == null) return null;

            result.Emails = await ctx.ContractorEmail
                .Where(e => e.ContractorId == result.ContractorId && e.Active)
                .Select(e => e.Email)
                .ToListAsync();

            return result;
        }

        public async Task Create(int contractorId, string email, string password)
        {
            using var ctx = _factory.CreateDbContext();

            var contractor = await ctx.Contractor.FirstOrDefaultAsync(c => c.ContractorId == contractorId)
                ?? throw new AbrilException("Contratista no encontrado.", 404);

            // Un correo de usuario pertenece a UNA sola contratista (contractor_user).
            // contractor_email queda solo como correos de contacto, por lo que este flujo
            // ya no crea ni vincula filas allí.
            var emailNormalizado = email.Trim().ToLower();
            var existingUser = await ContractorAccountEmailPolicy.ValidateAndGetUserAsync(
                ctx, emailNormalizado, contractorId, contractor.ContributorId);

            User user;
            if (existingUser != null)
            {
                user = existingUser;
                user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                user.UpdatedDateTime = DateTime.UtcNow;
            }
            else
            {
                user = new User
                {
                    Email = emailNormalizado,
                    EmailConfirmed = true,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow
                };
                user.Password = BCrypt.Net.BCrypt.HashPassword(password);
                ctx.User.Add(user);
            }

            await ctx.SaveChangesAsync();

            var contractorUserExists = await ctx.ContractorUser
                .AnyAsync(cu => cu.ContractorId == contractorId && cu.UserId == user.UserId && cu.Active);
            if (!contractorUserExists)
                ctx.ContractorUser.Add(new ContractorUser
                {
                    ContractorId = contractorId,
                    UserId = user.UserId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    Active = true,
                    State = true
                });

            var roleExists = await ctx.UserRole
                .AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == 11 && ur.Active);
            if (!roleExists)
                ctx.UserRole.Add(new UserRole
                {
                    UserId = user.UserId,
                    RoleId = 11,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId = user.UserId,
                    Active = true,
                    State = true
                });

            contractor.ActivationToken = null;
            contractor.ActivationTokenExpiry = null;
            contractor.UpdatedDateTime = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }
    }
}
