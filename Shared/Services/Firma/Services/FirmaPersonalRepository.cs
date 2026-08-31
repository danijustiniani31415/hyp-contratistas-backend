using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Services.Firma.Dtos;
using Abril_Backend.Shared.Services.Firma.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.Firma.Services
{
    public class FirmaPersonalRepository : IFirmaPersonalRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public FirmaPersonalRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<FirmaPersonalDto?> GetByUserId(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var p = await ctx.Person
                .Where(x => x.UserId == userId && x.SignatureImageBytes != null)
                .Select(x => new { x.SignatureImageBytes, x.SignatureMime, x.SignatureUpdatedDateTime })
                .FirstOrDefaultAsync();

            if (p == null) return null;

            return new FirmaPersonalDto
            {
                ImageDataUrl = $"data:{p.SignatureMime};base64,{Convert.ToBase64String(p.SignatureImageBytes!)}",
                UpdatedDateTime = p.SignatureUpdatedDateTime.HasValue
                    ? p.SignatureUpdatedDateTime.Value.ToOffset(TimeSpan.FromHours(-5)).DateTime
                    : (DateTime?)null
            };
        }

        public async Task Upsert(int userId, byte[] imageBytes, string mime)
        {
            using var ctx = _factory.CreateDbContext();

            var person = await ctx.Person.FirstOrDefaultAsync(x => x.UserId == userId)
                ?? throw new AbrilException("No se encontró una persona asociada al usuario actual.");

            person.SignatureImageBytes = imageBytes;
            person.SignatureMime = mime;
            person.SignatureUpdatedDateTime = DateTimeOffset.UtcNow;

            await ctx.SaveChangesAsync();
        }

        public async Task<(byte[] Bytes, string Mime)?> GetActiveBytesByUserId(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var p = await ctx.Person
                .Where(x => x.UserId == userId && x.SignatureImageBytes != null)
                .Select(x => new { x.SignatureImageBytes, x.SignatureMime })
                .FirstOrDefaultAsync();

            return p == null ? null : (p.SignatureImageBytes!, p.SignatureMime!);
        }
    }
}
