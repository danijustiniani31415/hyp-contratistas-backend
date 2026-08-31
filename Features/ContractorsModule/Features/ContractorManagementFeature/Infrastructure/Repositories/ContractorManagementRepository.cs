using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Contractors.ContractorManagement.Application;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos;
using Abril_Backend.Features.Contractors.ContractorManagement.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Contractors.ContractorManagement.Infrastructure.Repositories
{
    public class ContractorManagementRepository : IContractorManagementRepository
    {
        private const int PendingContractorStateId = 1;
        private const int ApprovedContractorStateId = 2;
        private const int RejectedContractorStateId = 3;
        private const int PendingUpdateContractorStateId = 4;

        private const int PendingUpdateRequestStateId  = 1; // contractor_update_state: PENDIENTE
        private const int AppliedUpdateRequestStateId  = 2; // APLICADA
        private const int RejectedUpdateRequestStateId = 3; // RECHAZADA

        private readonly IDbContextFactory<AppDbContext> _factory;

        public ContractorManagementRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<PagedResult<ContributorPagedDto>> GetPaged(ContributorFilterDto filter)
        {
            using var ctx = _factory.CreateDbContext();

            const int pageSize = 10;

            var query =
                from ct in ctx.Contractor
                join c in ctx.Contributor on ct.ContributorId equals c.ContributorId
                join cs in ctx.ContractorState on ct.ContractorStateId equals cs.ContractorStateId
                join p in ctx.Person on c.LegalRepresentativePersonId equals p.PersonId into personJoin
                from p in personJoin.DefaultIfEmpty()
                where ct.Active && ct.State
                select new { ct, c, cs, p };

            if (!string.IsNullOrWhiteSpace(filter.ContributorName))
                query = query.Where(x => x.c.ContributorName.ToLower().Contains(filter.ContributorName.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.ContributorRuc))
                query = query.Where(x => x.c.ContributorRuc.Contains(filter.ContributorRuc));

            if (filter.ContractorStateId.HasValue)
                query = query.Where(x => x.ct.ContractorStateId == filter.ContractorStateId.Value);

            if (!string.IsNullOrWhiteSpace(filter.LegalRepresentativeDni))
                query = query.Where(x => x.p != null && x.p.DocumentIdentityCode != null &&
                    x.p.DocumentIdentityCode.Contains(filter.LegalRepresentativeDni));

            if (!string.IsNullOrWhiteSpace(filter.LegalRepresentativeName))
                query = query.Where(x => x.p != null && x.p.FullName != null &&
                    x.p.FullName.ToLower().Contains(filter.LegalRepresentativeName.ToLower()));

            if (!string.IsNullOrWhiteSpace(filter.LegalEntityRegistryNumber))
                query = query.Where(x => x.c.LegalEntityRegistryNumber != null &&
                    x.c.LegalEntityRegistryNumber.Contains(filter.LegalEntityRegistryNumber));

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(x => x.ct.ContractorId)
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ContributorPagedDto
                {
                    ContractorId = x.ct.ContractorId,
                    ContributorId = x.c.ContributorId,
                    ContributorRuc = x.c.ContributorRuc,
                    ContributorName = x.c.ContributorName,
                    ContributorAddress = x.c.ContributorAddress,
                    ContributorEconomicActivityDescription = x.c.ContributorEconomicActivityDescription,
                    ContributorDistrict   = x.c.ContributorDistrict,
                    ContributorProvince   = x.c.ContributorProvince,
                    ContributorDepartment = x.c.ContributorDepartment,
                    LegalRepresentativeDni      = x.p != null ? x.p.DocumentIdentityCode : null,
                    LegalRepresentativeFullName = x.p != null ? x.p.FullName : null,
                    LegalEntityRegistryNumber   = x.c.LegalEntityRegistryNumber,
                    ContractorStateId = x.cs.ContractorStateId,
                    ContractorStateDescription = x.cs.ContractorStateDescription,
                    LogoFileUrl           = x.ct.LogoFileUrl,
                    BrochureFileUrl       = x.ct.BrochureFileUrl,
                    FichaRucFileUrl       = x.ct.FichaRucFileUrl,
                    ReferencesListFileUrl = x.ct.ReferencesListFileUrl,
                    HasUser = ctx.ContractorUser.Any(cu => cu.ContractorId == x.ct.ContractorId && cu.Active),
                    CreatedDateTime = x.ct.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime
                })
                .ToListAsync();

            var ids = items.Select(c => c.ContractorId).ToList();

            var emails = await (
                from e in ctx.ContractorEmail
                join pt in ctx.ContractorPersonType on e.ContractorPersonTypeId equals pt.ContractorPersonTypeId into ptJoin
                from pt in ptJoin.DefaultIfEmpty()
                where ids.Contains(e.ContractorId) && e.State
                select new
                {
                    e.ContractorId,
                    e.ContractorEmailId,
                    e.Email,
                    e.Active,
                    e.ContractorPersonTypeId,
                    ContractorPersonTypeDescription = pt != null ? pt.Description : null
                }
            ).ToListAsync();

            var emailsByContractor = emails
                .GroupBy(e => e.ContractorId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in items)
            {
                var contractorEmails = emailsByContractor.GetValueOrDefault(item.ContractorId);
                if (contractorEmails is null) continue;

                item.Emails = contractorEmails.Select(e => e.Email).ToList();
                item.EmailDetails = contractorEmails
                    .Select(e => new ContractorEmailItemDto
                    {
                        ContractorEmailId = e.ContractorEmailId,
                        Email = e.Email,
                        Active = e.Active,
                        ContractorPersonTypeId = e.ContractorPersonTypeId,
                        ContractorPersonTypeDescription = e.ContractorPersonTypeDescription
                    })
                    .ToList();
            }

            var users = await (
                from cu in ctx.ContractorUser
                join u in ctx.User on cu.UserId equals u.UserId
                where ids.Contains(cu.ContractorId) && cu.Active
                select new { cu.ContractorId, cu.UserId, u.Email, cu.CreatedDateTime }
            ).ToListAsync();

            var usersByContractor = users
                .GroupBy(u => u.ContractorId)
                .ToDictionary(g => g.Key, g => g.Select(u => new ContractorUserItemDto
                {
                    UserId = u.UserId,
                    Email = u.Email,
                    CreatedDateTime = u.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime
                }).ToList());

            foreach (var item in items)
                item.Users = usersByContractor.GetValueOrDefault(item.ContractorId, new());

            // ── Datos propuestos para contratistas en estado 4 (actualización pendiente) ──
            var pendingIds = items
                .Where(i => i.ContractorStateId == PendingUpdateContractorStateId)
                .Select(i => i.ContractorId)
                .ToList();

            if (pendingIds.Count > 0)
            {
                var requests = await ctx.ContractorUpdateRequest
                    .Where(r => pendingIds.Contains(r.ContractorId)
                                && r.State
                                && r.ContractorUpdateStateId == PendingUpdateRequestStateId)
                    .ToListAsync();

                var requestIds = requests.Select(r => r.ContractorUpdateRequestId).ToList();
                var requestEmails = await ctx.ContractorUpdateRequestEmail
                    .Where(e => requestIds.Contains(e.ContractorUpdateRequestId) && e.State)
                    .Select(e => new { e.ContractorUpdateRequestId, e.Email })
                    .ToListAsync();

                var emailsByRequest = requestEmails
                    .GroupBy(e => e.ContractorUpdateRequestId)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.Email).ToList());

                // Un solo request pendiente por contratista (índice único parcial lo garantiza).
                var requestByContractor = requests
                    .GroupBy(r => r.ContractorId)
                    .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.CreatedDateTime).First());

                foreach (var item in items)
                {
                    if (!requestByContractor.TryGetValue(item.ContractorId, out var r)) continue;
                    item.PendingUpdate = new ContractorPendingUpdateDto
                    {
                        ContractorUpdateRequestId               = r.ContractorUpdateRequestId,
                        ContributorRuc                          = r.ContributorRuc,
                        ContributorName                         = r.ContributorName,
                        ContributorAddress                      = r.ContributorAddress,
                        ContributorEconomicActivityDescription  = r.ContributorEconomicActivityDescription,
                        ContributorDistrict                     = r.ContributorDistrict,
                        ContributorProvince                     = r.ContributorProvince,
                        ContributorDepartment                   = r.ContributorDepartment,
                        LegalRepresentativeDni                  = r.LegalRepresentativeDni,
                        LegalRepresentativeFullName             = r.LegalRepresentativeFullName,
                        LegalEntityRegistryNumber               = r.LegalEntityRegistryNumber,
                        LogoFileUrl                             = r.LogoFileUrl,
                        BrochureFileUrl                         = r.BrochureFileUrl,
                        FichaRucFileUrl                         = r.FichaRucFileUrl,
                        ReferencesListFileUrl                   = r.ReferencesListFileUrl,
                        CreatedDateTime                         = r.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                        Emails                                  = emailsByRequest.GetValueOrDefault(r.ContractorUpdateRequestId, new()),
                    };
                }
            }

            return new PagedResult<ContributorPagedDto>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = items
            };
        }

        public async Task Approve(int contractorId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.ContractorStateId == PendingContractorStateId);
            if (contractor is null) return;

            contractor.ContractorStateId = ApprovedContractorStateId;
            contractor.UpdatedDateTime = DateTimeOffset.UtcNow;
            contractor.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task Reject(int contractorId, int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var contractor = await ctx.Contractor.FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.ContractorStateId == PendingContractorStateId);
            if (contractor is null) return;
            contractor.ContractorStateId = RejectedContractorStateId;
            contractor.UpdatedDateTime = DateTimeOffset.UtcNow;
            contractor.UpdatedUserId = userId;
            await ctx.SaveChangesAsync();
        }

        public async Task ApproveUpdate(int contractorId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.State && c.ContractorStateId == PendingUpdateContractorStateId)
                ?? throw new AbrilException("Este contratista no tiene una actualización pendiente.", 404);

            var request = await ctx.ContractorUpdateRequest
                .FirstOrDefaultAsync(r => r.ContractorId == contractorId && r.State && r.ContractorUpdateStateId == PendingUpdateRequestStateId)
                ?? throw new AbrilException("No se encontró la solicitud de actualización pendiente.", 404);

            var contributor = await ctx.Contributor
                .FirstOrDefaultAsync(c => c.ContributorId == contractor.ContributorId)
                ?? throw new AbrilException("Contribuyente no encontrado.", 404);

            // ── Aplicar datos de empresa (el RUC no cambia: el flujo se identifica por RUC) ──
            contributor.ContributorName                        = request.ContributorName;
            contributor.ContributorAddress                     = request.ContributorAddress ?? contributor.ContributorAddress;
            contributor.ContributorEconomicActivityDescription = request.ContributorEconomicActivityDescription;
            contributor.ContributorDistrict                    = request.ContributorDistrict;
            contributor.ContributorProvince                    = request.ContributorProvince;
            contributor.ContributorDepartment                  = request.ContributorDepartment;
            contributor.LegalEntityRegistryNumber              = request.LegalEntityRegistryNumber;
            contributor.UpdatedDateTime                        = DateTimeOffset.UtcNow;
            contributor.UpdatedUserId                          = userId;

            // ── Representante legal ──────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(request.LegalRepresentativeDni) || !string.IsNullOrWhiteSpace(request.LegalRepresentativeFullName))
            {
                var dni = request.LegalRepresentativeDni?.Trim();
                var person = !string.IsNullOrWhiteSpace(dni)
                    ? await ctx.Person.FirstOrDefaultAsync(p => p.DocumentIdentityCode == dni)
                    : null;

                if (person is null)
                {
                    person = new Abril_Backend.Infrastructure.Models.Person
                    {
                        DocumentIdentityCode = dni,
                        FullName             = request.LegalRepresentativeFullName?.Trim(),
                        CreatedDateTime      = DateTime.UtcNow,
                        CreatedUserId        = userId,
                        Active = true,
                        State  = true
                    };
                    ctx.Person.Add(person);
                    await ctx.SaveChangesAsync();
                }
                else
                {
                    person.FullName        = request.LegalRepresentativeFullName?.Trim();
                    person.UpdatedDateTime = DateTime.UtcNow;
                    person.UpdatedUserId   = userId;
                }
                contributor.LegalRepresentativePersonId = person.PersonId;
            }

            // ── Archivos (null = conservar el vigente) ───────────────────────────
            if (request.LogoFileUrl           is not null) contractor.LogoFileUrl           = request.LogoFileUrl;
            if (request.BrochureFileUrl       is not null) contractor.BrochureFileUrl       = request.BrochureFileUrl;
            if (request.FichaRucFileUrl       is not null) contractor.FichaRucFileUrl       = request.FichaRucFileUrl;
            if (request.ReferencesListFileUrl is not null) contractor.ReferencesListFileUrl = request.ReferencesListFileUrl;

            // ── Correos: reemplazar el conjunto vigente por el propuesto ─────────
            var reqEmails = await ctx.ContractorUpdateRequestEmail
                .Where(e => e.ContractorUpdateRequestId == request.ContractorUpdateRequestId && e.State)
                .ToListAsync();

            var current = await ctx.ContractorEmail
                .Where(e => e.ContractorId == contractorId && e.State)
                .ToListAsync();
            foreach (var e in current)
            {
                e.State           = false;
                e.Active          = false;
                e.UpdatedDateTime = DateTimeOffset.UtcNow;
                e.UpdatedUserId   = userId;
            }

            // Los correos de contacto no se vinculan a usuarios: la cuenta de usuario de la
            // contratista vive únicamente en contractor_user.
            foreach (var re in reqEmails)
            {
                ctx.ContractorEmail.Add(new Abril_Backend.Features.CostsModule.Shared.Models.ContractorEmail
                {
                    ContractorId           = contractorId,
                    Email                  = re.Email,
                    ContractorPersonTypeId = re.ContractorPersonTypeId,
                    CreatedDateTime        = DateTimeOffset.UtcNow,
                    CreatedUserId          = userId,
                    Active = true,
                    State  = true
                });
            }

            request.ContractorUpdateStateId = AppliedUpdateRequestStateId;
            request.UpdatedDateTime         = DateTimeOffset.UtcNow;
            request.UpdatedUserId           = userId;

            contractor.ContractorStateId = ApprovedContractorStateId;
            contractor.UpdatedDateTime   = DateTimeOffset.UtcNow;
            contractor.UpdatedUserId     = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task RejectUpdate(int contractorId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.State && c.ContractorStateId == PendingUpdateContractorStateId)
                ?? throw new AbrilException("Este contratista no tiene una actualización pendiente.", 404);

            var request = await ctx.ContractorUpdateRequest
                .FirstOrDefaultAsync(r => r.ContractorId == contractorId && r.State && r.ContractorUpdateStateId == PendingUpdateRequestStateId)
                ?? throw new AbrilException("No se encontró la solicitud de actualización pendiente.", 404);

            // Se descartan los datos nuevos; el contratista vuelve a "Aprobado" con los datos antiguos.
            request.ContractorUpdateStateId = RejectedUpdateRequestStateId;
            request.UpdatedDateTime         = DateTimeOffset.UtcNow;
            request.UpdatedUserId           = userId;

            contractor.ContractorStateId = ApprovedContractorStateId;
            contractor.UpdatedDateTime   = DateTimeOffset.UtcNow;
            contractor.UpdatedUserId     = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task<ContractorWithEmailsDto?> GetWithEmails(int contractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var result = await (
                from ct in ctx.Contractor
                join c in ctx.Contributor on ct.ContributorId equals c.ContributorId
                where ct.ContractorId == contractorId && ct.Active && ct.State
                select new ContractorWithEmailsDto
                {
                    ContractorId = ct.ContractorId,
                    ContributorName = c.ContributorName,
                    ContributorRuc = c.ContributorRuc,
                    ContractorStateId = ct.ContractorStateId
                }
            ).FirstOrDefaultAsync();

            if (result == null) return null;

            // Solo correos vigentes y activos: un correo desactivado (active=false) no debe
            // recibir credenciales ni notificaciones.
            result.Emails = await ctx.ContractorEmail
                .Where(e => e.ContractorId == contractorId && e.State && e.Active)
                .Select(e => e.Email)
                .ToListAsync();

            return result;
        }

        public async Task Update(
            int contractorId,
            ContractorUpdateDto dto,
            string? logoUrl,
            string? brochureUrl,
            string? fichaRucUrl,
            string? referencesUrl,
            int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.Active && c.State)
                ?? throw new Exception("Contratista no encontrado.");

            var contributor = await ctx.Contributor
                .FirstOrDefaultAsync(c => c.ContributorId == contractor.ContributorId)
                ?? throw new Exception("Contribuyente no encontrado.");

            // ── Validar RUC duplicado ─────────────────────────────────────────────
            // Si el RUC cambia, no debe coincidir con el de otro contribuyente vigente
            // (evita romper el índice único y devuelve un mensaje claro).
            var newRuc = dto.ContributorRuc?.Trim();
            if (!string.IsNullOrWhiteSpace(newRuc) && !string.Equals(newRuc, contributor.ContributorRuc, StringComparison.Ordinal))
            {
                var rucEnUso = await ctx.Contributor.AnyAsync(c =>
                    c.ContributorId != contributor.ContributorId &&
                    c.State &&
                    c.ContributorRuc == newRuc);

                if (rucEnUso)
                    throw new AbrilException($"Ya existe otro contribuyente registrado con el RUC {newRuc}.", 409);
            }

            // ── Campos texto del Contributor ─────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(dto.ContributorRuc))
                contributor.ContributorRuc                      = dto.ContributorRuc.Trim();
            contributor.ContributorName                         = dto.ContributorName.Trim();
            contributor.ContributorAddress                      = dto.ContributorAddress.Trim();
            contributor.ContributorEconomicActivityDescription  = dto.ContributorEconomicActivityDescription.Trim();
            contributor.ContributorDistrict                     = dto.ContributorDistrict?.Trim();
            contributor.ContributorProvince                     = dto.ContributorProvince?.Trim();
            contributor.ContributorDepartment                   = dto.ContributorDepartment?.Trim();
            contributor.LegalEntityRegistryNumber               = dto.LegalEntityRegistryNumber?.Trim();
            contributor.UpdatedDateTime                         = DateTimeOffset.UtcNow;
            contributor.UpdatedUserId                           = userId;

            // ── Representante legal (Person) ──────────────────────────────────────
            if (contributor.LegalRepresentativePersonId.HasValue)
            {
                var person = await ctx.Person
                    .FirstOrDefaultAsync(p => p.PersonId == contributor.LegalRepresentativePersonId.Value);
                if (person is not null)
                {
                    person.DocumentIdentityCode = dto.LegalRepresentativeDni?.Trim();
                    person.FullName             = dto.LegalRepresentativeFullName?.Trim();
                    person.UpdatedDateTime      = DateTime.UtcNow;
                    person.UpdatedUserId        = userId;
                }
            }
            else if (!string.IsNullOrWhiteSpace(dto.LegalRepresentativeDni)
                  || !string.IsNullOrWhiteSpace(dto.LegalRepresentativeFullName))
            {
                var dni = dto.LegalRepresentativeDni?.Trim();

                // Si ya existe una persona con ese DNI (aunque no esté asociada a este contratista),
                // se reutiliza y se le actualiza el nombre — así no se viola la restricción única
                // person_document_identity_code_key al intentar insertar un duplicado.
                var existingPerson = !string.IsNullOrWhiteSpace(dni)
                    ? await ctx.Person.FirstOrDefaultAsync(p => p.DocumentIdentityCode == dni)
                    : null;

                if (existingPerson is not null)
                {
                    existingPerson.FullName        = dto.LegalRepresentativeFullName?.Trim();
                    existingPerson.UpdatedDateTime = DateTime.UtcNow;
                    existingPerson.UpdatedUserId   = userId;
                    contributor.LegalRepresentativePersonId = existingPerson.PersonId;
                }
                else
                {
                    var newPerson = new Abril_Backend.Infrastructure.Models.Person
                    {
                        DocumentIdentityCode = dni,
                        FullName             = dto.LegalRepresentativeFullName?.Trim(),
                        CreatedDateTime      = DateTime.UtcNow,
                        CreatedUserId        = userId,
                        Active               = true,
                        State                = true
                    };
                    ctx.Person.Add(newPerson);
                    await ctx.SaveChangesAsync();   // flush para obtener PersonId
                    contributor.LegalRepresentativePersonId = newPerson.PersonId;
                }
            }

            // ── Archivos (null = conservar el existente) ──────────────────────────
            if (logoUrl       is not null) contractor.LogoFileUrl          = logoUrl;
            if (brochureUrl   is not null) contractor.BrochureFileUrl      = brochureUrl;
            if (fichaRucUrl   is not null) contractor.FichaRucFileUrl      = fichaRucUrl;
            if (referencesUrl is not null) contractor.ReferencesListFileUrl = referencesUrl;

            contractor.UpdatedDateTime = DateTimeOffset.UtcNow;
            contractor.UpdatedUserId   = userId;

            // ── Sincronizar correos ───────────────────────────────────────────────
            // 'State' es el flag de soft-delete: ningún registro se elimina físicamente.
            // 'Active' indica si el correo está habilitado (aparece en filtros/desplegables y
            // recibe correos). Ahora los correos existentes son editables: se puede cambiar su
            // texto y activarlos/desactivarlos sin borrarlos.
            //   - Correo con id existente que llega en la lista → se actualiza Email y Active.
            //   - Correo con id existente que NO llega en la lista → State = false (soft-delete).
            //   - Correo sin id (nuevo) → se INSERTA con su flag Active.
            // Existe un índice único parcial (contractor_id, correo) WHERE state = true que
            // garantiza un único registro vigente por correo EXACTO, permitiendo múltiples
            // históricos con State = false. La comparación es sensible a mayúsculas.
            var incomingEmails = ContractorEmailParser.Parse(dto.EmailsJson);

            var currentEmails = await ctx.ContractorEmail
                .Where(e => e.ContractorId == contractorId && e.State)
                .ToListAsync();

            var incomingById = incomingEmails
                .Where(e => e.ContractorEmailId is > 0)
                .ToDictionary(e => e.ContractorEmailId!.Value);

            // Actualizar / soft-delete de los correos existentes
            foreach (var existing in currentEmails)
            {
                if (incomingById.TryGetValue(existing.ContractorEmailId, out var match))
                {
                    existing.Email                  = match.Email.Trim();
                    existing.Active                 = match.Active;
                    existing.ContractorPersonTypeId = match.ContractorPersonTypeId;
                    existing.UpdatedDateTime        = DateTimeOffset.UtcNow;
                    existing.UpdatedUserId          = userId;
                }
                else
                {
                    existing.State           = false;
                    existing.Active          = false;
                    existing.UpdatedDateTime = DateTimeOffset.UtcNow;
                    existing.UpdatedUserId   = userId;
                }
            }

            // Insertar los nuevos (sin id)
            foreach (var newEmail in incomingEmails.Where(e => e.ContractorEmailId is null or 0))
            {
                var email = newEmail.Email.Trim();
                if (string.IsNullOrEmpty(email)) continue;

                ctx.ContractorEmail.Add(new Abril_Backend.Features.CostsModule.Shared.Models.ContractorEmail
                {
                    ContractorId           = contractorId,
                    Email                  = email,
                    ContractorPersonTypeId = newEmail.ContractorPersonTypeId,
                    CreatedDateTime        = DateTimeOffset.UtcNow,
                    CreatedUserId          = userId,
                    Active                 = newEmail.Active,
                    State                  = true
                });
            }

            await ctx.SaveChangesAsync();
        }

        public async Task SetActivationToken(int contractorId, string token, DateTime expiry)
        {
            using var ctx = _factory.CreateDbContext();
            var contractor = await ctx.Contractor
                .FirstOrDefaultAsync(c => c.ContractorId == contractorId && c.Active && c.State);
            if (contractor is null) return;
            contractor.ActivationToken = token;
            contractor.ActivationTokenExpiry = expiry;
            contractor.UpdatedDateTime = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }
    }
}
