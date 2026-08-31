using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Features.Contractors.ContractorRegistration.Application.Dtos;
using Abril_Backend.Features.Contractors.ContractorRegistration.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Contractors.ContractorRegistration.Infrastructure.Repositories
{
    public class ContractorRegistrationRepository : IContractorRegistrationRepository
    {
        private const int PendingContractorStateId       = 1;
        private const int ApprovedContractorStateId       = 2;
        private const int PendingUpdateContractorStateId  = 4;

        private const int PendingUpdateRequestStateId     = 1; // contractor_update_state: PENDIENTE

        // Estados de contratista considerados "activos/operativos" (un único vigente por contributor).
        private static readonly int[] ActiveContractorStates = { 1, 2, 4 };

        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<ContractorRegistrationRepository> _logger;

        public ContractorRegistrationRepository(
            IDbContextFactory<AppDbContext> factory,
            ILogger<ContractorRegistrationRepository> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<List<ContractorPersonTypeDto>> GetPersonTypes()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.ContractorPersonType
                .Where(t => t.State)
                .OrderBy(t => t.ContractorPersonTypeId)
                .Select(t => new ContractorPersonTypeDto
                {
                    ContractorPersonTypeId = t.ContractorPersonTypeId,
                    Description            = t.Description,
                })
                .ToListAsync();
        }

        public async Task<ContractorRucStatusDto> GetRucStatusAsync(string ruc)
        {
            using var ctx = _factory.CreateDbContext();

            var contributor = await ctx.Contributor
                .FirstOrDefaultAsync(c => c.ContributorRuc == ruc && c.State);

            if (contributor == null)
                return new ContractorRucStatusDto { Exists = false };

            var contractors = await ctx.Contractor
                .Where(c => c.ContributorId == contributor.ContributorId)
                .ToListAsync();

            var active = contractors
                .Where(c => c.State && ActiveContractorStates.Contains(c.ContractorStateId))
                .OrderByDescending(c => c.CreatedDateTime)
                .FirstOrDefault();

            int updateRequestCount = 0;
            if (active != null)
                updateRequestCount = await ctx.ContractorUpdateRequest
                    .CountAsync(r => r.ContractorId == active.ContractorId);

            return new ContractorRucStatusDto
            {
                Exists                  = true,
                ContributorId           = contributor.ContributorId,
                ContributorName         = contributor.ContributorName,
                ActiveContractorId      = active?.ContractorId,
                ActiveContractorStateId = active?.ContractorStateId,
                ContractorCount         = contractors.Count,
                UpdateRequestCount      = updateRequestCount,
            };
        }

        // ── Registro nuevo (RUC inexistente) ───────────────────────────────────
        public async Task CreateNew(ContributorCreateDto dto, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();

                var legalRepresentativePersonId = await ResolveLegalRepresentativeAsync(ctx, dto, userId);

                var contributor = new Contributor
                {
                    ContributorRuc                          = dto.ContributorRuc,
                    ContributorName                         = dto.ContributorName,
                    ContributorAddress                      = dto.ContributorAddress,
                    ContributorEconomicActivityDescription  = dto.ContributorEconomicActivityDescription,
                    ContributorDistrict                     = dto.ContributorDistrict,
                    ContributorProvince                     = dto.ContributorProvince,
                    ContributorDepartment                   = dto.ContributorDepartment,
                    LegalRepresentativePersonId             = legalRepresentativePersonId,
                    LegalEntityRegistryNumber               = dto.LegalEntityRegistryNumber,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId   = userId,
                    Active = true,
                    State  = true
                };
                ctx.Contributor.Add(contributor);
                await ctx.SaveChangesAsync();

                var contractor = NewPendingContractor(contributor.ContributorId, userId, logoUrl, brochureUrl, fichaRucUrl, referencesUrl);
                AddContractorEmails(contractor, dto, userId);

                ctx.Contractor.Add(contractor);
                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR REGISTRO CONTRATISTA (nuevo): {msg}", ex.ToString());
                throw;
            }
        }

        // ── Solicitud de actualización sobre contratista APROBADO (staging) ─────
        public async Task CreateUpdateRequest(int contractorId, ContributorCreateDto dto, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();

                var contractor = await ctx.Contractor.FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.State)
                    ?? throw new AbrilException("Contratista no encontrado.", 404);

                if (contractor.ContractorStateId == PendingUpdateContractorStateId)
                    throw new AbrilException("Ya existe una solicitud de actualización pendiente de revisión para este contratista.", 409);

                var request = new ContractorUpdateRequest
                {
                    ContractorId                            = contractorId,
                    ContractorUpdateStateId                 = PendingUpdateRequestStateId,
                    ContributorRuc                          = dto.ContributorRuc,
                    ContributorName                         = dto.ContributorName,
                    ContributorAddress                      = dto.ContributorAddress,
                    ContributorEconomicActivityDescription  = dto.ContributorEconomicActivityDescription,
                    ContributorDistrict                     = dto.ContributorDistrict,
                    ContributorProvince                     = dto.ContributorProvince,
                    ContributorDepartment                   = dto.ContributorDepartment,
                    LegalRepresentativeDni                  = dto.LegalRepresentativeDni,
                    LegalRepresentativeFullName             = dto.LegalRepresentativeFullName,
                    LegalEntityRegistryNumber               = dto.LegalEntityRegistryNumber,
                    LogoFileUrl                             = logoUrl,
                    BrochureFileUrl                         = brochureUrl,
                    FichaRucFileUrl                         = fichaRucUrl,
                    ReferencesListFileUrl                   = referencesUrl,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId   = userId,
                    Active = true,
                    State  = true,
                };

                for (int i = 0; i < dto.ContributorEmails.Count; i++)
                {
                    request.Emails.Add(new ContractorUpdateRequestEmail
                    {
                        Email                  = dto.ContributorEmails[i],
                        ContractorPersonTypeId = ParsePersonTypeId(dto, i),
                        CreatedDateTime        = DateTimeOffset.UtcNow,
                        CreatedUserId          = userId,
                        Active = true,
                        State  = true,
                    });
                }

                ctx.ContractorUpdateRequest.Add(request);

                // El contratista pasa a "Aprobado - actualización pendiente"; sigue operando con datos antiguos.
                contractor.ContractorStateId = PendingUpdateContractorStateId;
                contractor.UpdatedDateTime   = DateTimeOffset.UtcNow;
                contractor.UpdatedUserId     = userId;

                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR SOLICITUD ACTUALIZACION CONTRATISTA: {msg}", ex.ToString());
                throw;
            }
        }

        // ── Sobrescritura directa (contratista PENDIENTE/RECHAZADO o sin activo) ─
        public async Task OverwriteOrCreateDirect(int contributorId, int? existingContractorId, ContributorCreateDto dto, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl)
        {
            try
            {
                using var ctx = _factory.CreateDbContext();

                var contributor = await ctx.Contributor.FirstOrDefaultAsync(c => c.ContributorId == contributorId && c.State)
                    ?? throw new AbrilException("Contribuyente no encontrado.", 404);

                var legalRepresentativePersonId = await ResolveLegalRepresentativeAsync(ctx, dto, userId);

                // Sobrescribir datos de la empresa en sitio (no se conservan los anteriores).
                contributor.ContributorName                        = dto.ContributorName;
                contributor.ContributorAddress                     = dto.ContributorAddress;
                contributor.ContributorEconomicActivityDescription = dto.ContributorEconomicActivityDescription;
                contributor.ContributorDistrict                    = dto.ContributorDistrict;
                contributor.ContributorProvince                    = dto.ContributorProvince;
                contributor.ContributorDepartment                  = dto.ContributorDepartment;
                if (legalRepresentativePersonId.HasValue)
                    contributor.LegalRepresentativePersonId        = legalRepresentativePersonId;
                contributor.LegalEntityRegistryNumber              = dto.LegalEntityRegistryNumber;
                contributor.UpdatedDateTime                        = DateTimeOffset.UtcNow;
                contributor.UpdatedUserId                          = userId;

                if (existingContractorId.HasValue)
                {
                    // Reutilizar el contratista pendiente: actualizar archivos, correos y estado.
                    var contractor = await ctx.Contractor.FirstOrDefaultAsync(c => c.ContractorId == existingContractorId.Value)
                        ?? throw new AbrilException("Contratista no encontrado.", 404);

                    if (logoUrl       is not null) contractor.LogoFileUrl           = logoUrl;
                    if (brochureUrl   is not null) contractor.BrochureFileUrl       = brochureUrl;
                    if (fichaRucUrl   is not null) contractor.FichaRucFileUrl       = fichaRucUrl;
                    if (referencesUrl is not null) contractor.ReferencesListFileUrl = referencesUrl;
                    contractor.ContractorStateId = PendingContractorStateId;
                    contractor.UpdatedDateTime   = DateTimeOffset.UtcNow;
                    contractor.UpdatedUserId     = userId;

                    // Reemplazar el conjunto de correos: soft-delete de los vigentes + insertar los nuevos.
                    var current = await ctx.ContractorEmail
                        .Where(e => e.ContractorId == contractor.ContractorId && e.State)
                        .ToListAsync();
                    foreach (var e in current)
                    {
                        e.State           = false;
                        e.Active          = false;
                        e.UpdatedDateTime = DateTimeOffset.UtcNow;
                        e.UpdatedUserId   = userId;
                    }
                    AddContractorEmails(contractor, dto, userId);
                }
                else
                {
                    // Solo había contratistas rechazados (o ninguno): crear uno nuevo en espera.
                    var contractor = NewPendingContractor(contributorId, userId, logoUrl, brochureUrl, fichaRucUrl, referencesUrl);
                    AddContractorEmails(contractor, dto, userId);
                    ctx.Contractor.Add(contractor);
                }

                await ctx.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR SOBRESCRITURA CONTRATISTA: {msg}", ex.ToString());
                throw;
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────
        private static Contractor NewPendingContractor(int contributorId, int? userId, string? logoUrl, string? brochureUrl, string? fichaRucUrl, string? referencesUrl) => new()
        {
            ContributorId         = contributorId,
            ContractorStateId     = PendingContractorStateId,
            LogoFileUrl           = logoUrl,
            BrochureFileUrl       = brochureUrl,
            FichaRucFileUrl       = fichaRucUrl,
            ReferencesListFileUrl = referencesUrl,
            CreatedDateTime       = DateTimeOffset.UtcNow,
            CreatedUserId         = userId,
            Active = true,
            State  = true
        };

        private static async Task<int?> ResolveLegalRepresentativeAsync(AppDbContext ctx, ContributorCreateDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.LegalRepresentativeDni))
                return null;

            var person = await ctx.Person
                .FirstOrDefaultAsync(p => p.DocumentIdentityCode == dto.LegalRepresentativeDni && p.State);

            if (person == null)
            {
                person = new Person
                {
                    DocumentIdentityCode = dto.LegalRepresentativeDni,
                    FullName             = dto.LegalRepresentativeFullName,
                    Active = true,
                    State = true,
                    CreatedDateTime = DateTime.UtcNow,
                    CreatedUserId   = userId,
                };
                ctx.Person.Add(person);
                await ctx.SaveChangesAsync();
            }
            else if (!string.IsNullOrWhiteSpace(dto.LegalRepresentativeFullName))
            {
                person.FullName        = dto.LegalRepresentativeFullName;
                person.UpdatedDateTime = DateTime.UtcNow;
                person.UpdatedUserId   = userId;
            }

            return person.PersonId;
        }

        // Los correos de contacto no se vinculan a usuarios: la cuenta de usuario de la
        // contratista vive únicamente en contractor_user.
        private static void AddContractorEmails(Contractor contractor, ContributorCreateDto dto, int? userId)
        {
            for (int i = 0; i < dto.ContributorEmails.Count; i++)
            {
                contractor.Emails.Add(new ContractorEmail
                {
                    Email                  = dto.ContributorEmails[i],
                    ContractorPersonTypeId = ParsePersonTypeId(dto, i),
                    CreatedDateTime        = DateTimeOffset.UtcNow,
                    CreatedUserId          = userId,
                    Active = true,
                    State  = true
                });
            }
        }

        private static int? ParsePersonTypeId(ContributorCreateDto dto, int index)
        {
            if (index < dto.ContributorEmailPersonTypeIds.Count
                && int.TryParse(dto.ContributorEmailPersonTypeIds[index], out var ptId))
                return ptId;
            return null;
        }
    }
}
