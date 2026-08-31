using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Models;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Extensions;
using Dapper;
using System.Data;
using ProjectModel = Abril_Backend.Shared.Models.Project;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Repositories {
    public class ProjectSubContractorRepository : IProjectSubContractorRepository {
        private readonly AppDbContext _context;
        private readonly IDbContextFactory<AppDbContext> _factory;
        public ProjectSubContractorRepository(AppDbContext contexto, IDbContextFactory<AppDbContext> factory) {
            _context = contexto;
            _factory = factory;
        }

        public async Task<int> Create(ProjectSubContractorCreateDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var subContractor = new ProjectSubContractor
            {
                ProjectId = dto.ProjectId,
                ContractorId = dto.ContractorId,
                ContractTypeId = dto.ContractTypeId,
                ContractModalityId = dto.ContractModalityId,
                PaymentMethodId = dto.PaymentMethodId,
                PaymentFormId = dto.PaymentFormId,
                IncludesCartaFianza = dto.IncludesCartaFianza,
                AdvancePercentage = dto.AdvancePercentage,
                AdvanceAmount = dto.AdvanceAmount,
                Amount = dto.Amount,
                CurrencyId  = dto.CurrencyId,
                HasIgv = dto.HasIgv,
                ContractorEmail = string.Empty,
                WorkItemId = dto.WorkItemId,
                WorkItemCategoryId = dto.WorkItemCategoryId,
                WorkSpecialtyId = dto.WorkSpecialtyId,
                IsSubcontract = dto.IsSubcontract,
                IsLabor = dto.IsLabor,
                ContractWorkItemName = string.IsNullOrWhiteSpace(dto.ContractWorkItemName) ? null : dto.ContractWorkItemName.Trim(),
                ProjectSubContractorStatusId = 1,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true
            };

            ctx.ProjectSubContractor.Add(subContractor);
            await ctx.SaveChangesAsync();

            return subContractor.ProjectSubContractorId;
        }

        /// <summary>
        /// Misma resolución de carpeta 04_OBRAS y especialidad que <see cref="GetPathDataAsync"/>,
        /// pero a partir de los datos del alta (la adjudicación aún no existe), en un solo round-trip.
        /// </summary>
        public async Task<AdjudicacionPathDataDto> GetCreatePathDataAsync(int projectId, int? workSpecialtyId)
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from p in ctx.Project
                join pafObras in ctx.ProjectAdjudicacionFolder.Where(f =>
                        f.Active && f.State && f.FolderTypeId == AdjudicacionFolderTypes.Obras)
                    on p.ProjectId equals pafObras.ProjectId into pafObrasG
                from obrasFolder in pafObrasG.DefaultIfEmpty()
                from ws in ctx.WorkSpecialty
                    .Where(w => w.WorkSpecialtyId == workSpecialtyId).DefaultIfEmpty()
                where p.ProjectId == projectId
                select new AdjudicacionPathDataDto
                {
                    ProjectId          = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    ObrasDriveId       = obrasFolder != null ? obrasFolder.DriveId : null,
                    ObrasFolderId      = obrasFolder != null ? obrasFolder.FolderId : null,
                    WorkSpecialtyDescription = ws != null ? ws.WorkSpecialtyDescription : null,
                }
            ).FirstOrDefaultAsync()
                ?? throw new AbrilException("El proyecto seleccionado no existe.");
        }

        public async Task SoftDeleteAsync(int projectSubContractorId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var psc = await ctx.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State);
            if (psc is null) return;

            psc.State = false;
            psc.UpdatedDateTime = DateTimeOffset.UtcNow;
            psc.UpdatedUserId = userId;

            await ctx.SaveChangesAsync();
        }

        public async Task UpdateInfo(int projectSubContractorId, ProjectSubContractorUpdateInfoDTO dto, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var psc = await ctx.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            // Editable solo mientras esté en pasos 1–4. Del paso 5 en adelante queda bloqueada.
            if (psc.ProjectSubContractorStatusId >= 5)
                throw new AbrilException("La información de la adjudicación ya no se puede editar a partir del paso 5.");

            psc.ProjectId           = dto.ProjectId;
            psc.ContractorId        = dto.ContractorId;
            psc.ContractTypeId      = dto.ContractTypeId;
            psc.ContractModalityId  = dto.ContractModalityId;
            psc.PaymentMethodId     = dto.PaymentMethodId;
            psc.PaymentFormId       = dto.PaymentFormId;
            psc.IncludesCartaFianza = dto.IncludesCartaFianza;
            psc.AdvancePercentage   = dto.AdvancePercentage;
            psc.AdvanceAmount       = dto.AdvanceAmount;
            psc.Amount              = dto.Amount;
            psc.CurrencyId          = dto.CurrencyId;
            psc.HasIgv              = dto.HasIgv;
            psc.WorkItemId          = dto.WorkItemId;
            psc.WorkItemCategoryId  = dto.WorkItemCategoryId;
            psc.WorkSpecialtyId     = dto.WorkSpecialtyId;
            psc.IsSubcontract       = dto.IsSubcontract;
            psc.IsLabor             = dto.IsLabor;
            psc.ContractWorkItemName = string.IsNullOrWhiteSpace(dto.ContractWorkItemName) ? null : dto.ContractWorkItemName.Trim();
            psc.UpdatedDateTime     = DateTimeOffset.UtcNow;
            psc.UpdatedUserId       = userId;

            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Soft delete de cotizaciones / cuadros comparativos del paso 1. Los ids se filtran además
        /// por la adjudicación, para que no se pueda quitar un archivo de otra pasando su id.
        /// </summary>
        public async Task RemoveInitialFilesAsync(
            int projectSubContractorId,
            List<int>? quotationFileIds,
            List<int>? comparativeFileIds,
            int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            if (quotationFileIds is { Count: > 0 })
            {
                var rows = await ctx.ProjectSubContractorQuotationFile
                    .Where(f => f.ProjectSubContractorId == projectSubContractorId
                             && quotationFileIds.Contains(f.ProjectSubContractorQuotationFileId)
                             && f.State)
                    .ToListAsync();

                foreach (var row in rows)
                {
                    row.State = false;
                    row.UpdatedDateTime = now;
                    row.UpdatedUserId = userId;
                }
            }

            if (comparativeFileIds is { Count: > 0 })
            {
                var rows = await ctx.ProjectSubContractorComparativeFile
                    .Where(f => f.ProjectSubContractorId == projectSubContractorId
                             && comparativeFileIds.Contains(f.ProjectSubContractorComparativeFileId)
                             && f.State)
                    .ToListAsync();

                foreach (var row in rows)
                {
                    row.State = false;
                    row.UpdatedDateTime = now;
                    row.UpdatedUserId = userId;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task SaveInitialFilesAsync(
            int projectSubContractorId,
            List<(string Url, string OriginalFileName, string? ItemId)> quotationFiles,
            List<(string Url, string OriginalFileName, string? ItemId)> comparativeFiles,
            int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            foreach (var file in quotationFiles)
            {
                ctx.ProjectSubContractorQuotationFile.Add(new ProjectSubContractorQuotationFile
                {
                    ProjectSubContractorId = projectSubContractorId,
                    FileUrl = file.Url,
                    OriginalFileName = file.OriginalFileName,
                    SharepointItemId = file.ItemId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true
                });
            }

            foreach (var file in comparativeFiles)
            {
                ctx.ProjectSubContractorComparativeFile.Add(new ProjectSubContractorComparativeFile
                {
                    ProjectSubContractorId = projectSubContractorId,
                    FileUrl = file.Url,
                    OriginalFileName = file.OriginalFileName,
                    SharepointItemId = file.ItemId,
                    CreatedDateTime = now,
                    CreatedUserId = userId,
                    Active = true,
                    State = true
                });
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<List<ContractTypeSimpleDTO>> GetContractTypeFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.ContractType
                .Where(item => item.Active)
                .OrderBy(item => item.ContractTypeDescription)
                .Select(item => new ContractTypeSimpleDTO
                {
                    ContractTypeId = item.ContractTypeId,
                    ContractTypeDescription = item.ContractTypeDescription,
                });
            return await registros.ToListAsync();
        }

        public async Task<List<PaymentMethodSimpleDTO>> GetPaymentMethodFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.PaymentMethod
                .Where(item => item.Active)
                .OrderBy(item => item.PaymentMethodDescription)
                .Select(item => new PaymentMethodSimpleDTO
                {
                    PaymentMethodId = item.PaymentMethodId,
                    PaymentMethodDescription = item.PaymentMethodDescription,
                });
            return await registros.ToListAsync();
        }

        public async Task<List<CurrencySimpleDTO>> GetCurrencyFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.Currency
                .Where(item => item.Active)
                .OrderBy(item => item.CurrencyCode)
                .Select(item => new CurrencySimpleDTO
                {
                    CurrencyId = item.CurrencyId,
                    CurrencyDescription = item.CurrencyDescription,
                    CurrencyCode = item.CurrencyCode,
                    CurrencySymbol = item.CurrencySymbol,
                });
            return await registros.ToListAsync();
        }

        public async Task<List<WorkItemSimpleDTO>> GetWorkItemFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.WorkItem
                .Where(item => item.Active)
                .OrderBy(item => item.WorkItemDescription)
                .Select(item => new WorkItemSimpleDTO
                {
                    WorkItemId = item.WorkItemId,
                    WorkItemDescription = item.WorkItemDescription,
                });
            return await registros.ToListAsync();
        }

        public async Task<List<WorkItemCategorySimpleDTO>> GetWorkItemCategoryFactory()
        {
            using var ctx = _factory.CreateDbContext();
            var registros = ctx.WorkItemCategory
                .Where(item => item.Active)
                .OrderBy(item => item.WorkItemCategoryDescription)
                .Select(item => new WorkItemCategorySimpleDTO
                {
                    WorkItemCategoryId = item.WorkItemCategoryId,
                    WorkItemCategoryDescription = item.WorkItemCategoryDescription,
                });
            return await registros.ToListAsync();
        }

        public async Task<List<ContributorFactoryDTO>> GetCompanyFactory()
        {
            using var ctx = _factory.CreateDbContext();

            const int approvedContractorStateId = 2;

            var contractors = await (
                from ct in ctx.Contractor
                join contrib in ctx.Contributor on ct.ContributorId equals contrib.ContributorId
                where ct.Active && ct.State && ct.ContractorStateId == approvedContractorStateId
                orderby contrib.ContributorName
                select new ContributorFactoryDTO
                {
                    ContractorId = ct.ContractorId,
                    ContributorId = contrib.ContributorId,
                    ContributorName = contrib.ContributorName,
                    ContributorRuc = contrib.ContributorRuc
                }
            ).ToListAsync();

            var ids = contractors.Select(contrib => contrib.ContractorId).ToList();

            var emails = await ctx.ContractorEmail
                .Where(e => ids.Contains(e.ContractorId) && e.Active)
                .Select(e => new { e.ContractorId, e.Email })
                .ToListAsync();

            var emailsByContractor = emails
                .GroupBy(e => e.ContractorId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Email).ToList());

            foreach (var contractor in contractors)
                contractor.Emails = emailsByContractor.GetValueOrDefault(contractor.ContractorId, new());


            return contractors;
        }

        /// <summary>
        /// Trae todos los catálogos del formulario de creación de adjudicaciones en
        /// UN SOLO round-trip a la BD usando Dapper con multi-statement.
        ///
        /// Refactor-safety: los nombres de tablas y columnas se obtienen del modelo de
        /// EF Core en runtime (vía <see cref="EfMetadataExtensions"/>), por lo que renombrar
        /// una propiedad o entidad mantiene el SQL sincronizado automáticamente.
        ///
        /// El binding columna→propiedad lo hace Dapper con
        /// <c>DefaultTypeMap.MatchNamesWithUnderscores = true</c> (configurado en Program.cs).
        /// </summary>
        public async Task<ProjectSubContractorFormDataDTO> GetFormDataAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // ----- Tablas y columnas resueltas desde EF (refactor-safe) -----

            // Project
            string tProject       = ctx.Table<ProjectModel>();
            string cProjectId     = ctx.Col<ProjectModel>(nameof(ProjectModel.ProjectId));
            string cProjectDesc   = ctx.Col<ProjectModel>(nameof(ProjectModel.ProjectDescription));
            string cProjectActive = ctx.Col<ProjectModel>(nameof(ProjectModel.Active));
            string cProjectState  = ctx.Col<ProjectModel>(nameof(ProjectModel.State));

            // ContractType
            string tContractType       = ctx.Table<ContractType>();
            string cContractTypeId     = ctx.Col<ContractType>(nameof(ContractType.ContractTypeId));
            string cContractTypeDesc   = ctx.Col<ContractType>(nameof(ContractType.ContractTypeDescription));
            string cContractTypeActive = ctx.Col<ContractType>(nameof(ContractType.Active));

            // ContractModality
            string tContractModality       = ctx.Table<ContractModality>();
            string cContractModalityId     = ctx.Col<ContractModality>(nameof(ContractModality.ContractModalityId));
            string cContractModalityDesc   = ctx.Col<ContractModality>(nameof(ContractModality.ContractModalityDescription));
            string cContractModalityState  = ctx.Col<ContractModality>(nameof(ContractModality.State));

            // PaymentMethod
            string tPaymentMethod       = ctx.Table<PaymentMethod>();
            string cPaymentMethodId     = ctx.Col<PaymentMethod>(nameof(PaymentMethod.PaymentMethodId));
            string cPaymentMethodDesc   = ctx.Col<PaymentMethod>(nameof(PaymentMethod.PaymentMethodDescription));
            string cPaymentMethodActive = ctx.Col<PaymentMethod>(nameof(PaymentMethod.Active));

            // PaymentForm
            string tPaymentForm      = ctx.Table<PaymentForm>();
            string cPaymentFormId    = ctx.Col<PaymentForm>(nameof(PaymentForm.PaymentFormId));
            string cPaymentFormDesc  = ctx.Col<PaymentForm>(nameof(PaymentForm.PaymentFormDescription));
            string cPaymentFormState = ctx.Col<PaymentForm>(nameof(PaymentForm.State));

            // Currency
            string tCurrency        = ctx.Table<Currency>();
            string cCurrencyId      = ctx.Col<Currency>(nameof(Currency.CurrencyId));
            string cCurrencyDesc    = ctx.Col<Currency>(nameof(Currency.CurrencyDescription));
            string cCurrencyCode    = ctx.Col<Currency>(nameof(Currency.CurrencyCode));
            string cCurrencySymbol  = ctx.Col<Currency>(nameof(Currency.CurrencySymbol));
            string cCurrencyActive  = ctx.Col<Currency>(nameof(Currency.Active));

            // WorkItem
            string tWorkItem            = ctx.Table<WorkItem>();
            string cWorkItemId          = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemId));
            string cWorkItemDesc        = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemDescription));
            string cWorkItemCategoryFk  = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemCategoryId));
            string cWorkItemActive      = ctx.Col<WorkItem>(nameof(WorkItem.Active));

            // WorkItemValorizationForm (formas de valorización de la partida)
            string tWorkItemValForm            = ctx.Table<WorkItemValorizationForm>();
            string cWorkItemValFormWorkItemId  = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.WorkItemId));
            string cWorkItemValFormConcept     = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.Concept));
            string cWorkItemValFormPercentage  = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.Percentage));
            string cWorkItemValFormSortOrder   = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.SortOrder));
            string cWorkItemValFormState       = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.State));

            // WorkItemCategory
            string tWorkItemCategory            = ctx.Table<WorkItemCategory>();
            string cWorkItemCategoryId          = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.WorkItemCategoryId));
            string cWorkItemCategoryDesc        = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.WorkItemCategoryDescription));
            string cWorkItemCategorySpecialtyFk = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.WorkSpecialtyId));
            string cWorkItemCategoryActive      = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.Active));
            string cWorkItemCategorySyncStatus  = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.InstructivosSyncStatus));
            string cWorkItemCategoryFolderName  = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.InstructivosFolderName));

            // WorkSpecialty
            string tWorkSpecialty       = ctx.Table<WorkSpecialty>();
            string cWorkSpecialtyId     = ctx.Col<WorkSpecialty>(nameof(WorkSpecialty.WorkSpecialtyId));
            string cWorkSpecialtyDesc   = ctx.Col<WorkSpecialty>(nameof(WorkSpecialty.WorkSpecialtyDescription));
            string cWorkSpecialtyActive = ctx.Col<WorkSpecialty>(nameof(WorkSpecialty.Active));
            string cWorkSpecialtyState  = ctx.Col<WorkSpecialty>(nameof(WorkSpecialty.State));

            // ProjectSubContractorStatus (pasos de la adjudicación)
            string tStatus     = ctx.Table<ProjectSubContractorStatus>();
            string cStatusId   = ctx.Col<ProjectSubContractorStatus>(nameof(ProjectSubContractorStatus.ProjectSubContractorStatusId));
            string cStatusDesc = ctx.Col<ProjectSubContractorStatus>(nameof(ProjectSubContractorStatus.ProjectSubContractorStatusDescription));

            // Contractor + Contributor
            string tContractor          = ctx.Table<Contractor>();
            string cContractorId        = ctx.Col<Contractor>(nameof(Contractor.ContractorId));
            string cContractorContribId = ctx.Col<Contractor>(nameof(Contractor.ContributorId));
            string cContractorActive    = ctx.Col<Contractor>(nameof(Contractor.Active));
            string cContractorState     = ctx.Col<Contractor>(nameof(Contractor.State));
            string cContractorStateId   = ctx.Col<Contractor>(nameof(Contractor.ContractorStateId));

            string tContributor       = ctx.Table<Contributor>();
            string cContributorId     = ctx.Col<Contributor>(nameof(Contributor.ContributorId));
            string cContributorName   = ctx.Col<Contributor>(nameof(Contributor.ContributorName));
            string cContributorRuc    = ctx.Col<Contributor>(nameof(Contributor.ContributorRuc));

            // ContractorEmail
            string tContractorEmail            = ctx.Table<ContractorEmail>();
            string cContractorEmailContractor  = ctx.Col<ContractorEmail>(nameof(ContractorEmail.ContractorId));
            string cContractorEmailEmail       = ctx.Col<ContractorEmail>(nameof(ContractorEmail.Email));
            string cContractorEmailActive      = ctx.Col<ContractorEmail>(nameof(ContractorEmail.Active));

            // ----- SQL multi-statement (un solo round-trip) -----

            const int approvedContractorStateId = 2;

            string sql = $@"
                SELECT {cProjectId}, {cProjectDesc}
                  FROM {tProject}
                 WHERE {cProjectActive} = TRUE AND {cProjectState} = TRUE
                 ORDER BY {cProjectDesc};

                SELECT {cContractTypeId}, {cContractTypeDesc}
                  FROM {tContractType}
                 WHERE {cContractTypeActive} = TRUE
                 ORDER BY {cContractTypeDesc};

                SELECT {cContractModalityId}, {cContractModalityDesc}
                  FROM {tContractModality}
                 WHERE {cContractModalityState} = TRUE
                 ORDER BY {cContractModalityId};

                SELECT {cPaymentMethodId}, {cPaymentMethodDesc}
                  FROM {tPaymentMethod}
                 WHERE {cPaymentMethodActive} = TRUE
                 ORDER BY {cPaymentMethodDesc};

                SELECT {cPaymentFormId}, {cPaymentFormDesc}
                  FROM {tPaymentForm}
                 WHERE {cPaymentFormState} = TRUE
                 ORDER BY {cPaymentFormId};

                SELECT {cCurrencyId}, {cCurrencyDesc}, {cCurrencyCode}, {cCurrencySymbol}
                  FROM {tCurrency}
                 WHERE {cCurrencyActive} = TRUE
                 ORDER BY {cCurrencyCode};

                SELECT {cWorkItemId}, {cWorkItemDesc}, {cWorkItemCategoryFk}
                  FROM {tWorkItem}
                 WHERE {cWorkItemActive} = TRUE
                 ORDER BY {cWorkItemDesc};

                SELECT {cWorkItemValFormWorkItemId} AS work_item_id,
                       {cWorkItemValFormConcept} AS concept,
                       {cWorkItemValFormPercentage} AS percentage,
                       {cWorkItemValFormSortOrder} AS sort_order
                  FROM {tWorkItemValForm}
                 WHERE {cWorkItemValFormState} = TRUE
                 ORDER BY {cWorkItemValFormWorkItemId}, {cWorkItemValFormSortOrder};

                SELECT {cWorkItemCategoryId}, {cWorkItemCategoryDesc}, {cWorkItemCategorySpecialtyFk}, {cWorkItemCategorySyncStatus}, {cWorkItemCategoryFolderName}
                  FROM {tWorkItemCategory}
                 WHERE {cWorkItemCategoryActive} = TRUE
                 ORDER BY {cWorkItemCategoryDesc};

                SELECT {cWorkSpecialtyId}, {cWorkSpecialtyDesc}
                  FROM {tWorkSpecialty}
                 WHERE {cWorkSpecialtyActive} = TRUE AND {cWorkSpecialtyState} = TRUE
                 ORDER BY {cWorkSpecialtyDesc};

                SELECT {cStatusId} AS ""ProjectSubContractorStatusId"", {cStatusDesc} AS ""ProjectSubContractorStatusDescription""
                  FROM {tStatus}
                 ORDER BY {cStatusId};

                SELECT ct.{cContractorId} AS contractor_id,
                       contrib.{cContributorId} AS contributor_id,
                       contrib.{cContributorName} AS contributor_name,
                       contrib.{cContributorRuc} AS contributor_ruc
                  FROM {tContractor} ct
                  JOIN {tContributor} contrib ON contrib.{cContributorId} = ct.{cContractorContribId}
                 WHERE ct.{cContractorActive} = TRUE
                   AND ct.{cContractorState} = TRUE
                   AND ct.{cContractorStateId} = {approvedContractorStateId}
                 ORDER BY contrib.{cContributorName};

                SELECT {cContractorEmailContractor} AS contractor_id,
                       {cContractorEmailEmail} AS email
                  FROM {tContractorEmail}
                 WHERE {cContractorEmailActive} = TRUE;
            ";

            // ----- Ejecutar y leer los 12 result sets -----

            var connection = ctx.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(sql);

            var projects           = (await multi.ReadAsync<ProjectSimpleDTO>()).ToList();
            var contractTypes      = (await multi.ReadAsync<ContractTypeSimpleDTO>()).ToList();
            var contractModalities = (await multi.ReadAsync<ContractModalitySimpleDTO>()).ToList();
            var paymentMethods     = (await multi.ReadAsync<PaymentMethodSimpleDTO>()).ToList();
            var paymentForms       = (await multi.ReadAsync<PaymentFormSimpleDTO>()).ToList();
            var currencies         = (await multi.ReadAsync<CurrencySimpleDTO>()).ToList();
            var workItems          = (await multi.ReadAsync<WorkItemSimpleDTO>()).ToList();
            var valorizationForms  = (await multi.ReadAsync<WorkItemValorizationFormSimpleDTO>()).ToList();
            var workItemCategories = (await multi.ReadAsync<WorkItemCategorySimpleDTO>()).ToList();
            var workSpecialties    = (await multi.ReadAsync<WorkSpecialtySimpleDTO>()).ToList();
            var statuses           = (await multi.ReadAsync<ProjectSubContractorStatusSimpleDTO>()).ToList();
            var contractors        = (await multi.ReadAsync<ContributorFactoryDTO>()).ToList();
            var emails             = (await multi.ReadAsync<(int ContractorId, string Email)>()).ToList();

            // ----- Asociar emails a cada contratista -----

            var emailsByContractor = emails
                .GroupBy(e => e.ContractorId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Email).ToList());

            foreach (var contractor in contractors)
                contractor.Emails = emailsByContractor.GetValueOrDefault(contractor.ContractorId, new());

            // ----- Asociar formas de valorización a cada partida -----

            var formsByWorkItem = valorizationForms
                .GroupBy(f => f.WorkItemId)
                .ToDictionary(g => g.Key, g => g.OrderBy(f => f.SortOrder).ToList());

            foreach (var workItem in workItems)
                workItem.ValorizationForms = formsByWorkItem.GetValueOrDefault(workItem.WorkItemId, new());

            return new ProjectSubContractorFormDataDTO
            {
                Projects           = projects,
                ContractTypes      = contractTypes,
                ContractModalities = contractModalities,
                PaymentMethods     = paymentMethods,
                PaymentForms       = paymentForms,
                Currencies         = currencies,
                WorkItems          = workItems,
                WorkItemCategories = workItemCategories,
                WorkSpecialties    = workSpecialties,
                ProjectSubContractorStatuses = statuses,
                Contributors       = contractors,
            };
        }

        public async Task<PagedResult<ProjectSubContractorDTO>> GetPaged(ProjectSubContractorFilterDTO filter)
        {
            using var ctx = _factory.CreateDbContext();

            const int pageSize = 10;

            var query =
                from psc in ctx.ProjectSubContractor
                join p in ctx.Project on psc.ProjectId equals p.ProjectId
                join contractor in ctx.Contractor on psc.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                join ct in ctx.ContractType on psc.ContractTypeId equals ct.ContractTypeId
                join pm in ctx.PaymentMethod on psc.PaymentMethodId equals pm.PaymentMethodId
                join pfJoin in ctx.PaymentForm on psc.PaymentFormId equals pfJoin.PaymentFormId into pfGroup
                from pf in pfGroup.DefaultIfEmpty()
                join cur in ctx.Currency on psc.CurrencyId equals cur.CurrencyId
                join wi in ctx.WorkItem on psc.WorkItemId equals wi.WorkItemId
                join pscs in ctx.ProjectSubContractorStatus on psc.ProjectSubContractorStatusId equals pscs.ProjectSubContractorStatusId
                join wic in ctx.WorkItemCategory on psc.WorkItemCategoryId equals wic.WorkItemCategoryId
                join cmJoin in ctx.ContractModality on psc.ContractModalityId equals cmJoin.ContractModalityId into cmGroup
                from cm in cmGroup.DefaultIfEmpty()
                join contractDocJoin in ctx.ProjectSubContractorContract on psc.ProjectSubContractorContractId equals contractDocJoin.ProjectSubContractorContractId into contractDocGroup
                from contractDoc in contractDocGroup.DefaultIfEmpty()
                join summarySheetDocJoin in ctx.ProjectSubContractorSummarySheet on psc.ProjectSubContractorSummarySheetId equals summarySheetDocJoin.ProjectSubContractorSummarySheetId into summarySheetDocGroup
                from summarySheetDoc in summarySheetDocGroup.DefaultIfEmpty()
                join budgetDocJoin in ctx.ProjectSubContractorBudget on psc.ProjectSubContractorBudgetId equals budgetDocJoin.ProjectSubContractorBudgetId into budgetDocGroup
                from budgetDoc in budgetDocGroup.DefaultIfEmpty()
                join scheduleDocJoin in ctx.ProjectSubContractorSchedule on psc.ProjectSubContractorScheduleId equals scheduleDocJoin.ProjectSubContractorScheduleId into scheduleDocGroup
                from scheduleDoc in scheduleDocGroup.DefaultIfEmpty()
                join attachedQuotationDocJoin in ctx.ProjectSubContractorAttachedQuotation on psc.ProjectSubContractorAttachedQuotationId equals attachedQuotationDocJoin.ProjectSubContractorAttachedQuotationId into attachedQuotationDocGroup
                from attachedQuotationDoc in attachedQuotationDocGroup.DefaultIfEmpty()
                join serviceOrderDocJoin in ctx.ProjectSubContractorServiceOrder on psc.ProjectSubContractorServiceOrderId equals serviceOrderDocJoin.ProjectSubContractorServiceOrderId into serviceOrderDocGroup
                from serviceOrderDoc in serviceOrderDocGroup.DefaultIfEmpty()
                join promissoryNoteDocJoin in ctx.ProjectSubContractorPromissoryNote on psc.ProjectSubContractorPromissoryNoteId equals promissoryNoteDocJoin.ProjectSubContractorPromissoryNoteId into promissoryNoteDocGroup
                from promissoryNoteDoc in promissoryNoteDocGroup.DefaultIfEmpty()
                join packageDocJoin in ctx.ProjectSubContractorPackage on psc.ProjectSubContractorPackageId equals packageDocJoin.ProjectSubContractorPackageId into packageDocGroup
                from packageDoc in packageDocGroup.DefaultIfEmpty()
                join instructivoDocJoin in ctx.ProjectSubContractorInstructivo on psc.ProjectSubContractorInstructivoId equals instructivoDocJoin.ProjectSubContractorInstructivoId into instructivoDocGroup
                from instructivoDoc in instructivoDocGroup.DefaultIfEmpty()
                join nonConformingDocJoin in ctx.ProjectSubContractorNonConformingOutput on psc.ProjectSubContractorNonConformingOutputId equals nonConformingDocJoin.ProjectSubContractorNonConformingOutputId into nonConformingDocGroup
                from nonConformingDoc in nonConformingDocGroup.DefaultIfEmpty()
                join toleranceChartDocJoin in ctx.ProjectSubContractorToleranceChart on psc.ProjectSubContractorToleranceChartId equals toleranceChartDocJoin.ProjectSubContractorToleranceChartId into toleranceChartDocGroup
                from toleranceChartDoc in toleranceChartDocGroup.DefaultIfEmpty()
                join fichaTecnicaDocJoin in ctx.ProjectSubContractorFichaTecnica on psc.ProjectSubContractorFichaTecnicaId equals fichaTecnicaDocJoin.ProjectSubContractorFichaTecnicaId into fichaTecnicaDocGroup
                from fichaTecnicaDoc in fichaTecnicaDocGroup.DefaultIfEmpty()
                join anexoDocJoin in ctx.ProjectSubContractorAnexo on psc.ProjectSubContractorAnexoId equals anexoDocJoin.ProjectSubContractorAnexoId into anexoDocGroup
                from anexoDoc in anexoDocGroup.DefaultIfEmpty()
                join personCreatorJoin in ctx.Person on psc.CreatedUserId equals personCreatorJoin.UserId into personCreatorGroup
                from personCreator in personCreatorGroup.DefaultIfEmpty()
                where psc.State
                select new { psc, p, contractor, c, ct, cm, pm, pf, cur, wi, pscs, wic, contractDoc, summarySheetDoc, budgetDoc, scheduleDoc, attachedQuotationDoc, serviceOrderDoc, promissoryNoteDoc, packageDoc, instructivoDoc, nonConformingDoc, toleranceChartDoc, fichaTecnicaDoc, anexoDoc, personCreator };

            if (filter.AllowedProjectIds != null)
                query = query.Where(x => filter.AllowedProjectIds.Contains(x.psc.ProjectId));

            if (filter.ProjectId.HasValue)
                query = query.Where(x => x.psc.ProjectId == filter.ProjectId.Value);

            if (!string.IsNullOrWhiteSpace(filter.ContributorName))
                query = query.Where(x => x.c.ContributorName.Contains(filter.ContributorName));

            if (!string.IsNullOrWhiteSpace(filter.ContributorRuc))
                query = query.Where(x => x.c.ContributorRuc.Contains(filter.ContributorRuc));

            if (filter.ContractTypeId.HasValue)
                query = query.Where(x => x.psc.ContractTypeId == filter.ContractTypeId.Value);

            if (filter.ContractModalityId.HasValue)
                query = query.Where(x => x.psc.ContractModalityId == filter.ContractModalityId.Value);

            if (filter.PaymentMethodId.HasValue)
                query = query.Where(x => x.psc.PaymentMethodId == filter.PaymentMethodId.Value);

            if (filter.ProjectSubContractorStatusId.HasValue)
                query = query.Where(x => x.psc.ProjectSubContractorStatusId == filter.ProjectSubContractorStatusId.Value);

            if (filter.CreatedUserId.HasValue)
                query = query.Where(x => x.psc.CreatedUserId == filter.CreatedUserId.Value);

            query = query.OrderByDescending(x => x.psc.ProjectSubContractorId);

            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((filter.Page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new ProjectSubContractorDTO
                {
                    ProjectSubContractorId = x.psc.ProjectSubContractorId,
                    ProjectId = x.psc.ProjectId,
                    ProjectDescription = x.p.ProjectDescription,
                    ContractorId = x.psc.ContractorId,
                    ContributorId = x.c.ContributorId,
                    ContributorName = x.c.ContributorName,
                    ContractTypeId = x.psc.ContractTypeId,
                    ContractTypeDescription = x.ct.ContractTypeDescription,
                    ContractModalityId = x.psc.ContractModalityId,
                    ContractModalityDescription = x.cm != null ? x.cm.ContractModalityDescription : null,
                    PaymentMethodId = x.psc.PaymentMethodId,
                    PaymentMethodDescription = x.pm.PaymentMethodDescription,
                    PaymentFormId = x.psc.PaymentFormId,
                    PaymentFormDescription = x.pf != null ? x.pf.PaymentFormDescription : null,
                    IncludesCartaFianza = x.psc.IncludesCartaFianza,
                    AdvancePercentage = x.psc.AdvancePercentage,
                    AdvanceAmount = x.psc.AdvanceAmount,
                    Amount = x.psc.Amount,
                    CurrencyId = x.psc.CurrencyId,
                    CurrencyCode = x.cur.CurrencyCode,
                    AmountHasIgv = x.psc.HasIgv,
                    WorkItemId = x.psc.WorkItemId,
                    WorkItemDescription = x.wi.WorkItemDescription,
                    IsSubcontract = x.psc.IsSubcontract,
                    IsLabor = x.psc.IsLabor,
                    ContractWorkItemName = x.psc.ContractWorkItemName,
                    WorkItemCategoryId = x.psc.WorkItemCategoryId,
                    WorkItemCategoryDescription = x.wic.WorkItemCategoryDescription,
                    WorkItemCategoryInstructivosSyncStatus = x.wic.InstructivosSyncStatus,
                    WorkItemCategoryInstructivosFolderName = x.wic.InstructivosFolderName,
                    WorkSpecialtyId = x.psc.WorkSpecialtyId,
                    WorkSpecialtyDescription = ctx.WorkSpecialty
                        .Where(s => s.WorkSpecialtyId == x.psc.WorkSpecialtyId)
                        .Select(s => s.WorkSpecialtyDescription)
                        .FirstOrDefault(),
                    ProjectSubContractorStatusId = x.pscs.ProjectSubContractorStatusId,
                    ProjectSubContractorStatusDescription = x.pscs.ProjectSubContractorStatusDescription,
                    SigningDate = x.psc.SigningDate,
                    StartDate = x.psc.StartDate,
                    EndDate = x.psc.EndDate,
                    TermDays = x.psc.TermDays,
                    ContractNumber           = x.psc.ContractNumber,
                    PromissoryNoteNumber     = x.psc.PromissoryNoteNumber,
                    GuaranteeFundPercentage  = x.psc.GuaranteeFundPercentage,
                    GuaranteeFundDays        = x.psc.GuaranteeFundDays,
                    GuaranteeValidityDays    = x.psc.GuaranteeValidityDays,
                    PaymentDays              = x.psc.PaymentDays,
                    ArrivedWithObservations  = x.psc.ArrivedWithObservations,
                    ArrivalObservation       = x.psc.ArrivalObservation,
                    Step6SignedCostos              = x.psc.Step6SignedCostos,
                    Step6SignedGerenteInmobiliario = x.psc.Step6SignedGerenteInmobiliario,
                    Step6SignedGerenteGeneral      = x.psc.Step6SignedGerenteGeneral,
                    CreatedDateTime          = x.psc.CreatedDateTime,
                    CreatedUserFullName      = x.personCreator != null ? x.personCreator.FullName : null,
                    Contract          = x.contractDoc == null          ? null : new ProjectSubContractorFileDto { FileUrl = x.contractDoc.FileUrl!,          OriginalFileName = x.contractDoc.OriginalFileName,          StatusId = x.contractDoc.ProjectSubContractorFileStatusId,          StatusDescription = x.contractDoc.FileStatus == null          ? null : x.contractDoc.FileStatus.ProjectSubContractorFileStatusDescription,          Observation = x.contractDoc.Observation },
                    SummarySheet      = x.summarySheetDoc == null      ? null : new ProjectSubContractorFileDto { FileUrl = x.summarySheetDoc.FileUrl!,      OriginalFileName = x.summarySheetDoc.OriginalFileName,      StatusId = x.summarySheetDoc.ProjectSubContractorFileStatusId,      StatusDescription = x.summarySheetDoc.FileStatus == null      ? null : x.summarySheetDoc.FileStatus.ProjectSubContractorFileStatusDescription,      Observation = x.summarySheetDoc.Observation },
                    Budget            = x.budgetDoc == null            ? null : new ProjectSubContractorFileDto { FileUrl = x.budgetDoc.FileUrl!,            OriginalFileName = x.budgetDoc.OriginalFileName,            StatusId = x.budgetDoc.ProjectSubContractorFileStatusId,            StatusDescription = x.budgetDoc.FileStatus == null            ? null : x.budgetDoc.FileStatus.ProjectSubContractorFileStatusDescription,            Observation = x.budgetDoc.Observation },
                    Schedule          = x.scheduleDoc == null          ? null : new ProjectSubContractorFileDto { FileUrl = x.scheduleDoc.FileUrl!,          OriginalFileName = x.scheduleDoc.OriginalFileName,          StatusId = x.scheduleDoc.ProjectSubContractorFileStatusId,          StatusDescription = x.scheduleDoc.FileStatus == null          ? null : x.scheduleDoc.FileStatus.ProjectSubContractorFileStatusDescription,          Observation = x.scheduleDoc.Observation },
                    AttachedQuotation = x.attachedQuotationDoc == null ? null : new ProjectSubContractorFileDto { FileUrl = x.attachedQuotationDoc.FileUrl!, OriginalFileName = x.attachedQuotationDoc.OriginalFileName, StatusId = x.attachedQuotationDoc.ProjectSubContractorFileStatusId, StatusDescription = x.attachedQuotationDoc.FileStatus == null ? null : x.attachedQuotationDoc.FileStatus.ProjectSubContractorFileStatusDescription, Observation = x.attachedQuotationDoc.Observation },
                    ServiceOrder      = x.serviceOrderDoc == null      ? null : new ProjectSubContractorFileDto { FileUrl = x.serviceOrderDoc.FileUrl!,      OriginalFileName = x.serviceOrderDoc.OriginalFileName,      StatusId = x.serviceOrderDoc.ProjectSubContractorFileStatusId,      StatusDescription = x.serviceOrderDoc.FileStatus == null      ? null : x.serviceOrderDoc.FileStatus.ProjectSubContractorFileStatusDescription,      Observation = x.serviceOrderDoc.Observation },
                    PromissoryNote    = x.promissoryNoteDoc == null    ? null : new ProjectSubContractorFileDto { FileUrl = x.promissoryNoteDoc.FileUrl!,    OriginalFileName = x.promissoryNoteDoc.OriginalFileName,    StatusId = x.promissoryNoteDoc.ProjectSubContractorFileStatusId,    StatusDescription = x.promissoryNoteDoc.FileStatus == null    ? null : x.promissoryNoteDoc.FileStatus.ProjectSubContractorFileStatusDescription,    Observation = x.promissoryNoteDoc.Observation },
                    Package           = x.packageDoc == null           ? null : new ProjectSubContractorFileDto { FileUrl = x.packageDoc.FileUrl!,           OriginalFileName = x.packageDoc.OriginalFileName },
                    Instructivo       = x.instructivoDoc == null       ? null : new ProjectSubContractorFileDto { FileUrl = x.instructivoDoc.FileUrl!,       OriginalFileName = x.instructivoDoc.OriginalFileName,       StatusId = x.instructivoDoc.ProjectSubContractorFileStatusId,       StatusDescription = x.instructivoDoc.FileStatus == null       ? null : x.instructivoDoc.FileStatus.ProjectSubContractorFileStatusDescription,       Observation = x.instructivoDoc.Observation },
                    NonConformingOutput = x.psc.NonConformingOutputStatusId == null ? null : new ProjectSubContractorFileDto { StatusId = x.psc.NonConformingOutputStatusId },
                    ToleranceChart    = x.psc.ToleranceChartStatusId == null ? null : new ProjectSubContractorFileDto { StatusId = x.psc.ToleranceChartStatusId },
                    FinishProtection  = x.psc.FinishProtectionStatusId == null ? null : new ProjectSubContractorFileDto { StatusId = x.psc.FinishProtectionStatusId },
                    FichaTecnica      = x.fichaTecnicaDoc == null      ? null : new ProjectSubContractorFileDto { FileUrl = x.fichaTecnicaDoc.FileUrl!,      OriginalFileName = x.fichaTecnicaDoc.OriginalFileName,      StatusId = x.fichaTecnicaDoc.ProjectSubContractorFileStatusId,      StatusDescription = x.fichaTecnicaDoc.FileStatus == null      ? null : x.fichaTecnicaDoc.FileStatus.ProjectSubContractorFileStatusDescription,      Observation = x.fichaTecnicaDoc.Observation },
                    Anexo             = x.anexoDoc == null             ? null : new ProjectSubContractorFileDto { FileUrl = x.anexoDoc.FileUrl!,             OriginalFileName = x.anexoDoc.OriginalFileName,             StatusId = x.anexoDoc.ProjectSubContractorFileStatusId,             StatusDescription = x.anexoDoc.FileStatus == null             ? null : x.anexoDoc.FileStatus.ProjectSubContractorFileStatusDescription,             Observation = x.anexoDoc.Observation },
                })
                .ToListAsync();

            var ids = items.Select(x => x.ProjectSubContractorId).ToList();

            var contractorIds = items.Select(x => x.ContractorId).Distinct().ToList();
            var contractorEmails = await ctx.ContractorEmail
                .Where(e => contractorIds.Contains(e.ContractorId) && e.Active)
                .Select(e => new { e.ContractorId, e.Email })
                .ToListAsync();
            var emailsByContractorId = contractorEmails
                .GroupBy(e => e.ContractorId)
                .ToDictionary(g => g.Key, g => g.Select(e => e.Email).ToList());

            var quotationFiles = await ctx.ProjectSubContractorQuotationFile
                .Where(f => ids.Contains(f.ProjectSubContractorId) && f.State)
                .Select(f => new { f.ProjectSubContractorQuotationFileId, f.ProjectSubContractorId, f.FileUrl, f.OriginalFileName })
                .ToListAsync();

            var comparativeFiles = await ctx.ProjectSubContractorComparativeFile
                .Where(f => ids.Contains(f.ProjectSubContractorId) && f.State)
                .Select(f => new { f.ProjectSubContractorComparativeFileId, f.ProjectSubContractorId, f.FileUrl, f.OriginalFileName })
                .ToListAsync();

            var quotationByPsc = quotationFiles
                .GroupBy(f => f.ProjectSubContractorId)
                .ToDictionary(g => g.Key, g => g.Select(f => new ProjectSubContractorFileDto
                {
                    FileId = f.ProjectSubContractorQuotationFileId,
                    FileUrl = f.FileUrl,
                    OriginalFileName = f.OriginalFileName
                }).ToList());

            var comparativeByPsc = comparativeFiles
                .GroupBy(f => f.ProjectSubContractorId)
                .ToDictionary(g => g.Key, g => g.Select(f => new ProjectSubContractorFileDto
                {
                    FileId = f.ProjectSubContractorComparativeFileId,
                    FileUrl = f.FileUrl,
                    OriginalFileName = f.OriginalFileName
                }).ToList());

            var scannedDocs = await ctx.ProjectSubContractorScannedDoc
                .Where(f => ids.Contains(f.ProjectSubContractorId) && f.State)
                .Select(f => new { f.ProjectSubContractorId, f.Slot, f.FileUrl, f.OriginalFileName })
                .ToListAsync();

            var scannedByPsc = scannedDocs
                .GroupBy(f => f.ProjectSubContractorId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(f => f.Slot));

            // Formas de valorización (cláusula 5.1) de las partidas presentes en esta página.
            var workItemIds = items.Select(x => x.WorkItemId).Distinct().ToList();
            var valorizationForms = await ctx.WorkItemValorizationForm
                .Where(f => workItemIds.Contains(f.WorkItemId) && f.State)
                .OrderBy(f => f.WorkItemId).ThenBy(f => f.SortOrder)
                .Select(f => new WorkItemValorizationFormSimpleDTO
                {
                    WorkItemId = f.WorkItemId,
                    Concept    = f.Concept,
                    Percentage = f.Percentage,
                    SortOrder  = f.SortOrder
                })
                .ToListAsync();
            var formsByWorkItem = valorizationForms
                .GroupBy(f => f.WorkItemId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var item in items)
            {
                item.ContractorEmails = emailsByContractorId.GetValueOrDefault(item.ContractorId, new());
                item.QuotationFiles   = quotationByPsc.GetValueOrDefault(item.ProjectSubContractorId, new());
                item.ComparativeFiles = comparativeByPsc.GetValueOrDefault(item.ProjectSubContractorId, new());
                item.WorkItemValorizationForms = formsByWorkItem.GetValueOrDefault(item.WorkItemId, new());

                if (scannedByPsc.TryGetValue(item.ProjectSubContractorId, out var slots))
                {
                    if (slots.TryGetValue(1, out var s1))
                        item.ScannedDoc1 = new ProjectSubContractorFileDto { FileUrl = s1.FileUrl!, OriginalFileName = s1.OriginalFileName };
                    if (slots.TryGetValue(2, out var s2))
                        item.ScannedDoc2 = new ProjectSubContractorFileDto { FileUrl = s2.FileUrl!, OriginalFileName = s2.OriginalFileName };
                    if (slots.TryGetValue(3, out var s3))
                        item.ScannedDoc3 = new ProjectSubContractorFileDto { FileUrl = s3.FileUrl!, OriginalFileName = s3.OriginalFileName };
                }
            }

            return new PagedResult<ProjectSubContractorDTO>
            {
                Page = filter.Page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data = items
            };
        }

        // Helper: convierte cualquier valor de fecha (DateOnly, DateTime, string, null) a DateOnly?
        private static DateOnly? ToDateOnly(dynamic value)
        {
            if (value == null) return null;
            if (value is DateOnly d) return d;
            if (value is DateTime dt) return DateOnly.FromDateTime(dt);
            if (value is string s) return DateOnly.Parse(s);
            return null;
        }

        public async Task<AdjudicacionDashboardDto> GetDashboardAsync(ProjectSubContractorFilterDTO filter, bool includeFilterOptions)
        {
            using var ctx = _factory.CreateDbContext();

            var baseQ = ctx.ProjectSubContractor.Where(x => x.State && x.Active);
            if (filter.AllowedProjectIds != null)
                baseQ = baseQ.Where(x => filter.AllowedProjectIds.Contains(x.ProjectId));

            // Filtros del dashboard (afectan a todos los gráficos).
            if (filter.ProjectId.HasValue)
                baseQ = baseQ.Where(x => x.ProjectId == filter.ProjectId.Value);
            if (filter.ContractTypeId.HasValue)
                baseQ = baseQ.Where(x => x.ContractTypeId == filter.ContractTypeId.Value);
            if (filter.ContractModalityId.HasValue)
                baseQ = baseQ.Where(x => x.ContractModalityId == filter.ContractModalityId.Value);
            if (filter.PaymentMethodId.HasValue)
                baseQ = baseQ.Where(x => x.PaymentMethodId == filter.PaymentMethodId.Value);
            if (filter.ProjectSubContractorStatusId.HasValue)
                baseQ = baseQ.Where(x => x.ProjectSubContractorStatusId == filter.ProjectSubContractorStatusId.Value);

            // ── Resumen ────────────────────────────────────────────────────────
            var total = await baseQ.CountAsync();
            var completadas = await baseQ.CountAsync(x => x.ProjectSubContractorStatusId == 9);
            var totalProyectos = await baseQ.Select(x => x.ProjectId).Distinct().CountAsync();
            // Plazo promedio en días (solo adjudicaciones con ambas fechas).
            var plazoPromedio = await baseQ
                .Where(x => x.StartDate != null && x.EndDate != null)
                .Select(x => (double)(x.EndDate!.Value.DayNumber - x.StartDate!.Value.DayNumber))
                .DefaultIfEmpty()
                .AverageAsync();

            // ── Por estado (los 9 pasos, incluso en 0) ──────────────────────────
            var estados = await ctx.ProjectSubContractorStatus
                .OrderBy(e => e.ProjectSubContractorStatusId)
                .Select(e => new { e.ProjectSubContractorStatusId, e.ProjectSubContractorStatusDescription })
                .ToListAsync();
            var countByEstado = await baseQ
                .GroupBy(x => x.ProjectSubContractorStatusId)
                .Select(g => new { EstadoId = g.Key, Count = g.Count() })
                .ToListAsync();
            // Detalle breve de cada adjudicación ("CONTRATISTA — PARTIDA") agrupado por estado,
            // para el tooltip de la barra en el gráfico "Adjudicaciones por estado".
            var estadoDetalleRaw = await (
                from x in baseQ
                join contractor in ctx.Contractor on x.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                join w in ctx.WorkItem on x.WorkItemId equals w.WorkItemId
                orderby x.ProjectSubContractorId descending
                select new { x.ProjectSubContractorStatusId, c.ContributorName, w.WorkItemDescription }
            ).ToListAsync();
            var detallePorEstado = estadoDetalleRaw
                .GroupBy(d => d.ProjectSubContractorStatusId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(d => $"{d.ContributorName} — {d.WorkItemDescription}").ToList());
            var porEstado = estados.Select(e => new AdjudicacionEstadoChartItemDto
            {
                Id = e.ProjectSubContractorStatusId,
                Label = e.ProjectSubContractorStatusDescription,
                Value = countByEstado.FirstOrDefault(c => c.EstadoId == e.ProjectSubContractorStatusId)?.Count ?? 0,
                Items = detallePorEstado.GetValueOrDefault(e.ProjectSubContractorStatusId) ?? new List<string>()
            }).ToList();

            // ── Monto por moneda ────────────────────────────────────────────────
            var montoPorMoneda = await (
                from x in baseQ
                join cur in ctx.Currency on x.CurrencyId equals cur.CurrencyId
                group x by new { cur.CurrencyCode, cur.CurrencySymbol } into g
                select new AdjudicacionMoneyByCurrencyDto
                {
                    Code = g.Key.CurrencyCode,
                    Symbol = g.Key.CurrencySymbol,
                    Total = g.Sum(e => e.Amount)
                }
            ).ToListAsync();

            // ── Top subcontratistas por monto (PEN y USD) ───────────────────────
            const int TopN = 10;

            var topSubcontratistasPen = await (
                from x in baseQ
                join cur in ctx.Currency on x.CurrencyId equals cur.CurrencyId
                where cur.CurrencyCode == "PEN"
                join contractor in ctx.Contractor on x.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                group x by new { c.ContributorId, c.ContributorName } into g
                orderby g.Sum(e => e.Amount) descending
                select new AdjudicacionChartItemDto
                {
                    Id = g.Key.ContributorId,
                    Label = g.Key.ContributorName,
                    Value = g.Sum(e => e.Amount)
                }
            ).Take(TopN).ToListAsync();

            var topSubcontratistasUsd = await (
                from x in baseQ
                join cur in ctx.Currency on x.CurrencyId equals cur.CurrencyId
                where cur.CurrencyCode == "USD"
                join contractor in ctx.Contractor on x.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                group x by new { c.ContributorId, c.ContributorName } into g
                orderby g.Sum(e => e.Amount) descending
                select new AdjudicacionChartItemDto
                {
                    Id = g.Key.ContributorId,
                    Label = g.Key.ContributorName,
                    Value = g.Sum(e => e.Amount)
                }
            ).Take(TopN).ToListAsync();

            // ── Top subcontratistas por monto, solo contratos con adelanto ──────
            // "Contrato con adelanto" (2) y "Pago a cuenta" (4) — mismos ids que usa la
            // generación de contratos/pagarés. El adelanto es el monto explícito o, si no
            // se registró, el porcentaje aplicado al monto total.
            var advancePaymentMethodIds = new[] { 2, 4 };

            var topSubcontratistasAdelantoPen = await (
                from x in baseQ
                where advancePaymentMethodIds.Contains(x.PaymentMethodId)
                join cur in ctx.Currency on x.CurrencyId equals cur.CurrencyId
                where cur.CurrencyCode == "PEN"
                join contractor in ctx.Contractor on x.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                group x by new { c.ContributorId, c.ContributorName } into g
                orderby g.Sum(e => e.Amount) descending
                select new AdjudicacionAdvanceChartItemDto
                {
                    Id = g.Key.ContributorId,
                    Label = g.Key.ContributorName,
                    Total = g.Sum(e => e.Amount),
                    Advance = g.Sum(e => e.AdvanceAmount ?? Math.Round(e.AdvancePercentage / 100m * e.Amount, 2))
                }
            ).Take(TopN).ToListAsync();

            var topSubcontratistasAdelantoUsd = await (
                from x in baseQ
                where advancePaymentMethodIds.Contains(x.PaymentMethodId)
                join cur in ctx.Currency on x.CurrencyId equals cur.CurrencyId
                where cur.CurrencyCode == "USD"
                join contractor in ctx.Contractor on x.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                group x by new { c.ContributorId, c.ContributorName } into g
                orderby g.Sum(e => e.Amount) descending
                select new AdjudicacionAdvanceChartItemDto
                {
                    Id = g.Key.ContributorId,
                    Label = g.Key.ContributorName,
                    Total = g.Sum(e => e.Amount),
                    Advance = g.Sum(e => e.AdvanceAmount ?? Math.Round(e.AdvancePercentage / 100m * e.Amount, 2))
                }
            ).Take(TopN).ToListAsync();

            // ── Pendientes de Oficina Técnica por trabajador (pasos 2 y 4) ──────
            // Los pasos 2 (datos del contrato) y 4 (por enviar al SC) los ejecuta OT.
            // Cada adjudicación detenida en esos pasos se atribuye a los trabajadores de OT
            // del proyecto (staff_project_email tipo 3), resolviendo el nombre por el correo
            // corporativo del worker → person.full_name (si no hay person, se muestra el correo).
            const int otEmailTypeId = 3;
            var pendientesOtRaw = await (
                from x in baseQ
                where x.ProjectSubContractorStatusId == 2 || x.ProjectSubContractorStatusId == 4
                join spe in ctx.StaffProjectEmail.Where(s =>
                        s.State && s.Active && s.StaffProjectEmailTypeId == otEmailTypeId)
                    on x.ProjectId equals spe.ProjectId
                join contractor in ctx.Contractor on x.ContractorId equals contractor.ContractorId
                join c in ctx.Contributor on contractor.ContributorId equals c.ContributorId
                join wi in ctx.WorkItem on x.WorkItemId equals wi.WorkItemId
                join w in ctx.Worker on spe.Email.ToLower() equals (w.EmailCorporativo ?? "").ToLower() into wg
                from w in wg.DefaultIfEmpty()
                join per in ctx.Person on w.PersonId equals (int?)per.PersonId into perg
                from per in perg.DefaultIfEmpty()
                select new
                {
                    x.ProjectSubContractorId,
                    x.ProjectSubContractorStatusId,
                    Email = spe.Email.ToLower(),
                    WorkerName = per != null ? per.FullName : null,
                    c.ContributorName,
                    wi.WorkItemDescription,
                }
            ).ToListAsync();

            List<AdjudicacionEstadoChartItemDto> PendientesOtPorPaso(int statusId) => pendientesOtRaw
                .Where(r => r.ProjectSubContractorStatusId == statusId)
                // Un mismo correo puede matchear más de un worker: nos quedamos con uno por
                // adjudicación+correo, prefiriendo el que sí tiene nombre resuelto.
                .GroupBy(r => new { r.ProjectSubContractorId, r.Email })
                .Select(g => g.OrderBy(r => string.IsNullOrWhiteSpace(r.WorkerName) ? 1 : 0).First())
                .GroupBy(r => new { r.Email, Label = string.IsNullOrWhiteSpace(r.WorkerName) ? r.Email : r.WorkerName! })
                .Select(g => new AdjudicacionEstadoChartItemDto
                {
                    Id = 0,
                    Label = g.Key.Label,
                    Value = g.Count(),
                    Items = g.Select(r => $"{r.ContributorName} — {r.WorkItemDescription}").ToList(),
                })
                .OrderByDescending(i => i.Value)
                .ThenBy(i => i.Label)
                .ToList();

            var pendientesOtPaso2 = PendientesOtPorPaso(2);
            var pendientesOtPaso4 = PendientesOtPorPaso(4);

            // ── Catálogos de filtros (solo en la primera carga) ─────────────────
            AdjudicacionDashboardFiltersDto? filtros = null;
            if (includeFilterOptions)
            {
                var proyectosQ = ctx.Project.Where(p => p.Active && p.State);
                if (filter.AllowedProjectIds != null)
                    proyectosQ = proyectosQ.Where(p => filter.AllowedProjectIds.Contains(p.ProjectId));
                var proyectosOpt = await proyectosQ
                    .OrderBy(p => p.ProjectDescription)
                    .Select(p => new AdjudicacionOptionDto { Id = p.ProjectId, Label = p.ProjectDescription })
                    .ToListAsync();

                var tiposContratoOpt = await ctx.ContractType
                    .Where(ct => ct.Active)
                    .OrderBy(ct => ct.ContractTypeDescription)
                    .Select(ct => new AdjudicacionOptionDto { Id = ct.ContractTypeId, Label = ct.ContractTypeDescription! })
                    .ToListAsync();

                var modalidadesOpt = await ctx.ContractModality
                    .Where(cm => cm.State)
                    .OrderBy(cm => cm.ContractModalityId)
                    .Select(cm => new AdjudicacionOptionDto { Id = cm.ContractModalityId, Label = cm.ContractModalityDescription! })
                    .ToListAsync();

                var modalidadesPagoOpt = await ctx.PaymentMethod
                    .Where(pm => pm.Active)
                    .OrderBy(pm => pm.PaymentMethodDescription)
                    .Select(pm => new AdjudicacionOptionDto { Id = pm.PaymentMethodId, Label = pm.PaymentMethodDescription! })
                    .ToListAsync();

                var estadosOpt = await ctx.ProjectSubContractorStatus
                    .OrderBy(e => e.ProjectSubContractorStatusId)
                    .Select(e => new AdjudicacionOptionDto { Id = e.ProjectSubContractorStatusId, Label = e.ProjectSubContractorStatusDescription })
                    .ToListAsync();

                filtros = new AdjudicacionDashboardFiltersDto
                {
                    Projects = proyectosOpt,
                    ContractTypes = tiposContratoOpt,
                    ContractModalities = modalidadesOpt,
                    PaymentMethods = modalidadesPagoOpt,
                    Statuses = estadosOpt
                };
            }

            return new AdjudicacionDashboardDto
            {
                Filters = filtros,
                Summary = new AdjudicacionDashboardSummaryDto
                {
                    Total = total,
                    Completadas = completadas,
                    EnProceso = total - completadas,
                    TotalProyectos = totalProyectos,
                    MontoPenTotal = montoPorMoneda.FirstOrDefault(m => m.Code == "PEN")?.Total ?? 0,
                    MontoUsdTotal = montoPorMoneda.FirstOrDefault(m => m.Code == "USD")?.Total ?? 0,
                    PlazoPromedioDias = (int)Math.Round(plazoPromedio)
                },
                PorEstado = porEstado,
                MontoPorMoneda = montoPorMoneda,
                TopSubcontratistasPen = topSubcontratistasPen,
                TopSubcontratistasUsd = topSubcontratistasUsd,
                TopSubcontratistasAdelantoPen = topSubcontratistasAdelantoPen,
                TopSubcontratistasAdelantoUsd = topSubcontratistasAdelantoUsd,
                PendientesOtPaso2 = pendientesOtPaso2,
                PendientesOtPaso4 = pendientesOtPaso4
            };
        }

        public async Task<ProjectSubContractorPagedWithFiltersDTO> GetPagedWithFiltersAsync(ProjectSubContractorFilterDTO filter)
        {
            if (filter.Page < 1) filter.Page = 1;

            using var ctx = _factory.CreateDbContext();
            var connection = ctx.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            const int pageSize = 10;
            var offset = (filter.Page - 1) * pageSize;

            // Resolver tablas y columnas reales desde EF (refactor-safe + funciona en SQL Server y PostgreSQL/snake_case)
            string tPsc = ctx.Table<ProjectSubContractor>();
            string cPscId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorId));
            string cPscProjectId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectId));
            string cPscContractorId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ContractorId));
            string cPscContractTypeId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ContractTypeId));
            string cPscContractModalityId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ContractModalityId));
            string cPscPaymentMethodId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.PaymentMethodId));
            string cPscAmount = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.Amount));
            string cPscCurrencyId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.CurrencyId));
            string cPscHasIgv = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.HasIgv));
            string cPscWorkItemId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.WorkItemId));
            string cPscWorkItemCategoryId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.WorkItemCategoryId));
            string cPscStatusId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorStatusId));
            string cPscState = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.State));
            string cPscSigningDate = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.SigningDate));
            string cPscStartDate = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.StartDate));
            string cPscEndDate = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.EndDate));
            string cPscTermDays = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.TermDays));
            string cPscContractNumber = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ContractNumber));
            string cPscPromissoryNoteNumber = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.PromissoryNoteNumber));
            string cPscGuaranteeFundPercentage = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.GuaranteeFundPercentage));
            string cPscGuaranteeFundDays = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.GuaranteeFundDays));
            string cPscGuaranteeValidityDays = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.GuaranteeValidityDays));
            string cPscPaymentDays = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.PaymentDays));
            string cPscArrivedWithObservations = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ArrivedWithObservations));
            string cPscArrivalObservation = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ArrivalObservation));
            string cPscStep6SignedCostos = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.Step6SignedCostos));
            string cPscStep6SignedGerenteInmobiliario = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.Step6SignedGerenteInmobiliario));
            string cPscStep6SignedGerenteGeneral = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.Step6SignedGerenteGeneral));
            string cPscNonConformingStatusId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.NonConformingOutputStatusId));
            string cPscToleranceStatusId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ToleranceChartStatusId));
            string cPscFinishProtectionStatusId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.FinishProtectionStatusId));
            string cPscCreatedDateTime = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.CreatedDateTime));
            string cPscAdvancePercentage = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.AdvancePercentage));
            string cPscAdvanceAmount = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.AdvanceAmount));
            string cPscContractDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorContractId));
            string cPscSummarySheetDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorSummarySheetId));
            string cPscBudgetDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorBudgetId));
            string cPscScheduleDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorScheduleId));
            string cPscAttachedQuotationDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorAttachedQuotationId));
            string cPscServiceOrderDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorServiceOrderId));
            string cPscPromissoryNoteDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorPromissoryNoteId));
            string cPscPackageDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorPackageId));
            string cPscInstructivoDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorInstructivoId));
            string cPscNonConformingOutputDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorNonConformingOutputId));
            string cPscToleranceChartDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorToleranceChartId));
            string cPscFichaTecnicaDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorFichaTecnicaId));
            string cPscAnexoDocId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorAnexoId));
            string cPscCreatedUserId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.CreatedUserId));

            string tPersonCreator = ctx.Table<Person>();
            string cPersonCreatorUserId = ctx.Col<Person>(nameof(Person.UserId));
            string cPersonCreatorFullName = ctx.Col<Person>(nameof(Person.FullName));

            string tProject = ctx.Table<ProjectModel>();
            string cProjectId = ctx.Col<ProjectModel>(nameof(ProjectModel.ProjectId));
            string cProjectDesc = ctx.Col<ProjectModel>(nameof(ProjectModel.ProjectDescription));
            string cProjectActive = ctx.Col<ProjectModel>(nameof(ProjectModel.Active));

            string tContractor = ctx.Table<Contractor>();
            string cContractorId = ctx.Col<Contractor>(nameof(Contractor.ContractorId));
            string cContractorContribId = ctx.Col<Contractor>(nameof(Contractor.ContributorId));
            string cContractorActive = ctx.Col<Contractor>(nameof(Contractor.Active));
            string cContractorState = ctx.Col<Contractor>(nameof(Contractor.State));
            string cContractorStateId = ctx.Col<Contractor>(nameof(Contractor.ContractorStateId));

            string tContributor = ctx.Table<Contributor>();
            string cContributorId = ctx.Col<Contributor>(nameof(Contributor.ContributorId));
            string cContributorName = ctx.Col<Contributor>(nameof(Contributor.ContributorName));
            string cContributorRuc = ctx.Col<Contributor>(nameof(Contributor.ContributorRuc));

            string tContractType = ctx.Table<ContractType>();
            string cContractTypeId = ctx.Col<ContractType>(nameof(ContractType.ContractTypeId));
            string cContractTypeDesc = ctx.Col<ContractType>(nameof(ContractType.ContractTypeDescription));
            string cContractTypeActive = ctx.Col<ContractType>(nameof(ContractType.Active));

            string tContractModalityDapper = ctx.Table<ContractModality>();
            string cContractModalityIdDapper   = ctx.Col<ContractModality>(nameof(ContractModality.ContractModalityId));
            string cContractModalityDescDapper = ctx.Col<ContractModality>(nameof(ContractModality.ContractModalityDescription));
            string cContractModalityStateDapper = ctx.Col<ContractModality>(nameof(ContractModality.State));

            string tPaymentMethod = ctx.Table<PaymentMethod>();
            string cPaymentMethodId = ctx.Col<PaymentMethod>(nameof(PaymentMethod.PaymentMethodId));
            string cPaymentMethodDesc = ctx.Col<PaymentMethod>(nameof(PaymentMethod.PaymentMethodDescription));
            string cPaymentMethodActive = ctx.Col<PaymentMethod>(nameof(PaymentMethod.Active));

            string tPaymentForm = ctx.Table<PaymentForm>();
            string cPaymentFormId = ctx.Col<PaymentForm>(nameof(PaymentForm.PaymentFormId));
            string cPaymentFormDesc = ctx.Col<PaymentForm>(nameof(PaymentForm.PaymentFormDescription));
            string cPscPaymentFormId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.PaymentFormId));
            string cPscIncludesCartaFianza = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.IncludesCartaFianza));

            string tCurrency = ctx.Table<Currency>();
            string cCurrencyId = ctx.Col<Currency>(nameof(Currency.CurrencyId));
            string cCurrencyCode = ctx.Col<Currency>(nameof(Currency.CurrencyCode));
            string cCurrencyDesc = ctx.Col<Currency>(nameof(Currency.CurrencyDescription));
            string cCurrencySymbol = ctx.Col<Currency>(nameof(Currency.CurrencySymbol));
            string cCurrencyActive = ctx.Col<Currency>(nameof(Currency.Active));

            string tWorkItem = ctx.Table<WorkItem>();
            string cWorkItemId = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemId));
            string cWorkItemDesc = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemDescription));
            string cWorkItemActive = ctx.Col<WorkItem>(nameof(WorkItem.Active));

            string tWorkItemCategory = ctx.Table<WorkItemCategory>();
            string cWorkItemCategoryId = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.WorkItemCategoryId));
            string cWorkItemCategoryDesc = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.WorkItemCategoryDescription));
            string cWorkItemCategoryActive = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.Active));
            string cWorkItemCategorySyncStatus = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.InstructivosSyncStatus));
            string cWorkItemCategoryFolderName = ctx.Col<WorkItemCategory>(nameof(WorkItemCategory.InstructivosFolderName));

            // WorkItemValorizationForm (formas de valorización de la partida)
            string tWorkItemValForm = ctx.Table<WorkItemValorizationForm>();
            string cWorkItemValFormWorkItemId = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.WorkItemId));
            string cWorkItemValFormConcept = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.Concept));
            string cWorkItemValFormPercentage = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.Percentage));
            string cWorkItemValFormSortOrder = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.SortOrder));
            string cWorkItemValFormState = ctx.Col<WorkItemValorizationForm>(nameof(WorkItemValorizationForm.State));

            string cPscWorkSpecialtyId = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.WorkSpecialtyId));
            string cPscIsSubcontract = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.IsSubcontract));
            string cPscIsLabor = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.IsLabor));
            string cPscContractWorkItemName = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ContractWorkItemName));
            string tWorkSpecialtyDapper = ctx.Table<WorkSpecialty>();
            string cWorkSpecialtyIdDapper = ctx.Col<WorkSpecialty>(nameof(WorkSpecialty.WorkSpecialtyId));
            string cWorkSpecialtyDescDapper = ctx.Col<WorkSpecialty>(nameof(WorkSpecialty.WorkSpecialtyDescription));

            string tStatus = ctx.Table<ProjectSubContractorStatus>();
            string cStatusId = ctx.Col<ProjectSubContractorStatus>(nameof(ProjectSubContractorStatus.ProjectSubContractorStatusId));
            string cStatusDesc = ctx.Col<ProjectSubContractorStatus>(nameof(ProjectSubContractorStatus.ProjectSubContractorStatusDescription));

            // Document tables
            string tContractDoc = ctx.Table<ProjectSubContractorContract>();
            string cContractDocId = ctx.Col<ProjectSubContractorContract>(nameof(ProjectSubContractorContract.ProjectSubContractorContractId));
            string cContractDocFileUrl = ctx.Col<ProjectSubContractorContract>(nameof(ProjectSubContractorContract.FileUrl));
            string cContractDocFileName = ctx.Col<ProjectSubContractorContract>(nameof(ProjectSubContractorContract.OriginalFileName));
            string cContractDocStatusId = ctx.Col<ProjectSubContractorContract>(nameof(ProjectSubContractorContract.ProjectSubContractorFileStatusId));
            string cContractDocObs = ctx.Col<ProjectSubContractorContract>(nameof(ProjectSubContractorContract.Observation));

            string tSummaryDoc = ctx.Table<ProjectSubContractorSummarySheet>();
            string cSummaryDocId = ctx.Col<ProjectSubContractorSummarySheet>(nameof(ProjectSubContractorSummarySheet.ProjectSubContractorSummarySheetId));
            string cSummaryDocFileUrl = ctx.Col<ProjectSubContractorSummarySheet>(nameof(ProjectSubContractorSummarySheet.FileUrl));
            string cSummaryDocFileName = ctx.Col<ProjectSubContractorSummarySheet>(nameof(ProjectSubContractorSummarySheet.OriginalFileName));
            string cSummaryDocStatusId = ctx.Col<ProjectSubContractorSummarySheet>(nameof(ProjectSubContractorSummarySheet.ProjectSubContractorFileStatusId));
            string cSummaryDocObs = ctx.Col<ProjectSubContractorSummarySheet>(nameof(ProjectSubContractorSummarySheet.Observation));

            string tBudgetDoc = ctx.Table<ProjectSubContractorBudget>();
            string cBudgetDocId = ctx.Col<ProjectSubContractorBudget>(nameof(ProjectSubContractorBudget.ProjectSubContractorBudgetId));
            string cBudgetDocFileUrl = ctx.Col<ProjectSubContractorBudget>(nameof(ProjectSubContractorBudget.FileUrl));
            string cBudgetDocFileName = ctx.Col<ProjectSubContractorBudget>(nameof(ProjectSubContractorBudget.OriginalFileName));
            string cBudgetDocStatusId = ctx.Col<ProjectSubContractorBudget>(nameof(ProjectSubContractorBudget.ProjectSubContractorFileStatusId));
            string cBudgetDocObs = ctx.Col<ProjectSubContractorBudget>(nameof(ProjectSubContractorBudget.Observation));

            string tScheduleDoc = ctx.Table<ProjectSubContractorSchedule>();
            string cScheduleDocId = ctx.Col<ProjectSubContractorSchedule>(nameof(ProjectSubContractorSchedule.ProjectSubContractorScheduleId));
            string cScheduleDocFileUrl = ctx.Col<ProjectSubContractorSchedule>(nameof(ProjectSubContractorSchedule.FileUrl));
            string cScheduleDocFileName = ctx.Col<ProjectSubContractorSchedule>(nameof(ProjectSubContractorSchedule.OriginalFileName));
            string cScheduleDocStatusId = ctx.Col<ProjectSubContractorSchedule>(nameof(ProjectSubContractorSchedule.ProjectSubContractorFileStatusId));
            string cScheduleDocObs = ctx.Col<ProjectSubContractorSchedule>(nameof(ProjectSubContractorSchedule.Observation));

            string tAttQuotDoc = ctx.Table<ProjectSubContractorAttachedQuotation>();
            string cAttQuotDocId = ctx.Col<ProjectSubContractorAttachedQuotation>(nameof(ProjectSubContractorAttachedQuotation.ProjectSubContractorAttachedQuotationId));
            string cAttQuotDocFileUrl = ctx.Col<ProjectSubContractorAttachedQuotation>(nameof(ProjectSubContractorAttachedQuotation.FileUrl));
            string cAttQuotDocFileName = ctx.Col<ProjectSubContractorAttachedQuotation>(nameof(ProjectSubContractorAttachedQuotation.OriginalFileName));
            string cAttQuotDocStatusId = ctx.Col<ProjectSubContractorAttachedQuotation>(nameof(ProjectSubContractorAttachedQuotation.ProjectSubContractorFileStatusId));
            string cAttQuotDocObs = ctx.Col<ProjectSubContractorAttachedQuotation>(nameof(ProjectSubContractorAttachedQuotation.Observation));

            string tSvcOrderDoc = ctx.Table<ProjectSubContractorServiceOrder>();
            string cSvcOrderDocId = ctx.Col<ProjectSubContractorServiceOrder>(nameof(ProjectSubContractorServiceOrder.ProjectSubContractorServiceOrderId));
            string cSvcOrderDocFileUrl = ctx.Col<ProjectSubContractorServiceOrder>(nameof(ProjectSubContractorServiceOrder.FileUrl));
            string cSvcOrderDocFileName = ctx.Col<ProjectSubContractorServiceOrder>(nameof(ProjectSubContractorServiceOrder.OriginalFileName));
            string cSvcOrderDocStatusId = ctx.Col<ProjectSubContractorServiceOrder>(nameof(ProjectSubContractorServiceOrder.ProjectSubContractorFileStatusId));
            string cSvcOrderDocObs = ctx.Col<ProjectSubContractorServiceOrder>(nameof(ProjectSubContractorServiceOrder.Observation));

            string tPNoteDoc = ctx.Table<ProjectSubContractorPromissoryNote>();
            string cPNoteDocId = ctx.Col<ProjectSubContractorPromissoryNote>(nameof(ProjectSubContractorPromissoryNote.ProjectSubContractorPromissoryNoteId));
            string cPNoteDocFileUrl = ctx.Col<ProjectSubContractorPromissoryNote>(nameof(ProjectSubContractorPromissoryNote.FileUrl));
            string cPNoteDocFileName = ctx.Col<ProjectSubContractorPromissoryNote>(nameof(ProjectSubContractorPromissoryNote.OriginalFileName));
            string cPNoteDocStatusId = ctx.Col<ProjectSubContractorPromissoryNote>(nameof(ProjectSubContractorPromissoryNote.ProjectSubContractorFileStatusId));
            string cPNoteDocObs = ctx.Col<ProjectSubContractorPromissoryNote>(nameof(ProjectSubContractorPromissoryNote.Observation));

            string tPackageDoc = ctx.Table<ProjectSubContractorPackage>();
            string cPackageDocId = ctx.Col<ProjectSubContractorPackage>(nameof(ProjectSubContractorPackage.ProjectSubContractorPackageId));
            string cPackageDocFileUrl = ctx.Col<ProjectSubContractorPackage>(nameof(ProjectSubContractorPackage.FileUrl));
            string cPackageDocFileName = ctx.Col<ProjectSubContractorPackage>(nameof(ProjectSubContractorPackage.OriginalFileName));

            string tInstructivoDoc = ctx.Table<ProjectSubContractorInstructivo>();
            string cInstructivoDocId = ctx.Col<ProjectSubContractorInstructivo>(nameof(ProjectSubContractorInstructivo.ProjectSubContractorInstructivoId));
            string cInstructivoDocFileUrl = ctx.Col<ProjectSubContractorInstructivo>(nameof(ProjectSubContractorInstructivo.FileUrl));
            string cInstructivoDocFileName = ctx.Col<ProjectSubContractorInstructivo>(nameof(ProjectSubContractorInstructivo.OriginalFileName));
            string cInstructivoDocStatusId = ctx.Col<ProjectSubContractorInstructivo>(nameof(ProjectSubContractorInstructivo.ProjectSubContractorFileStatusId));
            string cInstructivoDocObs = ctx.Col<ProjectSubContractorInstructivo>(nameof(ProjectSubContractorInstructivo.Observation));

            string tNonConformingDoc = ctx.Table<ProjectSubContractorNonConformingOutput>();
            string cNonConformingDocId = ctx.Col<ProjectSubContractorNonConformingOutput>(nameof(ProjectSubContractorNonConformingOutput.ProjectSubContractorNonConformingOutputId));
            string cNonConformingDocFileUrl = ctx.Col<ProjectSubContractorNonConformingOutput>(nameof(ProjectSubContractorNonConformingOutput.FileUrl));
            string cNonConformingDocFileName = ctx.Col<ProjectSubContractorNonConformingOutput>(nameof(ProjectSubContractorNonConformingOutput.OriginalFileName));
            string cNonConformingDocStatusId = ctx.Col<ProjectSubContractorNonConformingOutput>(nameof(ProjectSubContractorNonConformingOutput.ProjectSubContractorFileStatusId));
            string cNonConformingDocObs = ctx.Col<ProjectSubContractorNonConformingOutput>(nameof(ProjectSubContractorNonConformingOutput.Observation));

            string tToleranceChartDoc = ctx.Table<ProjectSubContractorToleranceChart>();
            string cToleranceChartDocId = ctx.Col<ProjectSubContractorToleranceChart>(nameof(ProjectSubContractorToleranceChart.ProjectSubContractorToleranceChartId));
            string cToleranceChartDocFileUrl = ctx.Col<ProjectSubContractorToleranceChart>(nameof(ProjectSubContractorToleranceChart.FileUrl));
            string cToleranceChartDocFileName = ctx.Col<ProjectSubContractorToleranceChart>(nameof(ProjectSubContractorToleranceChart.OriginalFileName));
            string cToleranceChartDocStatusId = ctx.Col<ProjectSubContractorToleranceChart>(nameof(ProjectSubContractorToleranceChart.ProjectSubContractorFileStatusId));
            string cToleranceChartDocObs = ctx.Col<ProjectSubContractorToleranceChart>(nameof(ProjectSubContractorToleranceChart.Observation));

            string tFichaTecnicaDoc = ctx.Table<ProjectSubContractorFichaTecnica>();
            string cFichaTecnicaDocId = ctx.Col<ProjectSubContractorFichaTecnica>(nameof(ProjectSubContractorFichaTecnica.ProjectSubContractorFichaTecnicaId));
            string cFichaTecnicaDocFileUrl = ctx.Col<ProjectSubContractorFichaTecnica>(nameof(ProjectSubContractorFichaTecnica.FileUrl));
            string cFichaTecnicaDocFileName = ctx.Col<ProjectSubContractorFichaTecnica>(nameof(ProjectSubContractorFichaTecnica.OriginalFileName));
            string cFichaTecnicaDocStatusId = ctx.Col<ProjectSubContractorFichaTecnica>(nameof(ProjectSubContractorFichaTecnica.ProjectSubContractorFileStatusId));
            string cFichaTecnicaDocObs = ctx.Col<ProjectSubContractorFichaTecnica>(nameof(ProjectSubContractorFichaTecnica.Observation));

            string tAnexoDoc = ctx.Table<ProjectSubContractorAnexo>();
            string cAnexoDocId = ctx.Col<ProjectSubContractorAnexo>(nameof(ProjectSubContractorAnexo.ProjectSubContractorAnexoId));
            string cAnexoDocFileUrl = ctx.Col<ProjectSubContractorAnexo>(nameof(ProjectSubContractorAnexo.FileUrl));
            string cAnexoDocFileName = ctx.Col<ProjectSubContractorAnexo>(nameof(ProjectSubContractorAnexo.OriginalFileName));
            string cAnexoDocStatusId = ctx.Col<ProjectSubContractorAnexo>(nameof(ProjectSubContractorAnexo.ProjectSubContractorFileStatusId));
            string cAnexoDocObs = ctx.Col<ProjectSubContractorAnexo>(nameof(ProjectSubContractorAnexo.Observation));

            string tFileStatus = ctx.Table<ProjectSubContractorFileStatus>();
            string cFileStatusId = ctx.Col<ProjectSubContractorFileStatus>(nameof(ProjectSubContractorFileStatus.ProjectSubContractorFileStatusId));
            string cFileStatusDesc = ctx.Col<ProjectSubContractorFileStatus>(nameof(ProjectSubContractorFileStatus.ProjectSubContractorFileStatusDescription));

            string tContractorEmail = ctx.Table<ContractorEmail>();
            string cCEContractorId = ctx.Col<ContractorEmail>(nameof(ContractorEmail.ContractorId));
            string cCEEmail = ctx.Col<ContractorEmail>(nameof(ContractorEmail.Email));
            string cCEActive = ctx.Col<ContractorEmail>(nameof(ContractorEmail.Active));

            string tQuotFile = ctx.Table<ProjectSubContractorQuotationFile>();
            string cQFId = ctx.Col<ProjectSubContractorQuotationFile>(nameof(ProjectSubContractorQuotationFile.ProjectSubContractorQuotationFileId));
            string cQFPscId = ctx.Col<ProjectSubContractorQuotationFile>(nameof(ProjectSubContractorQuotationFile.ProjectSubContractorId));
            string cQFFileUrl = ctx.Col<ProjectSubContractorQuotationFile>(nameof(ProjectSubContractorQuotationFile.FileUrl));
            string cQFFileName = ctx.Col<ProjectSubContractorQuotationFile>(nameof(ProjectSubContractorQuotationFile.OriginalFileName));
            string cQFState = ctx.Col<ProjectSubContractorQuotationFile>(nameof(ProjectSubContractorQuotationFile.State));

            string tCompFile = ctx.Table<ProjectSubContractorComparativeFile>();
            string cCFId = ctx.Col<ProjectSubContractorComparativeFile>(nameof(ProjectSubContractorComparativeFile.ProjectSubContractorComparativeFileId));
            string cCFPscId = ctx.Col<ProjectSubContractorComparativeFile>(nameof(ProjectSubContractorComparativeFile.ProjectSubContractorId));
            string cCFFileUrl = ctx.Col<ProjectSubContractorComparativeFile>(nameof(ProjectSubContractorComparativeFile.FileUrl));
            string cCFFileName = ctx.Col<ProjectSubContractorComparativeFile>(nameof(ProjectSubContractorComparativeFile.OriginalFileName));
            string cCFState = ctx.Col<ProjectSubContractorComparativeFile>(nameof(ProjectSubContractorComparativeFile.State));

            string tScannedFile = ctx.Table<ProjectSubContractorScannedDoc>();
            string cSDPscId = ctx.Col<ProjectSubContractorScannedDoc>(nameof(ProjectSubContractorScannedDoc.ProjectSubContractorId));
            string cSDSlot = ctx.Col<ProjectSubContractorScannedDoc>(nameof(ProjectSubContractorScannedDoc.Slot));
            string cSDFileUrl = ctx.Col<ProjectSubContractorScannedDoc>(nameof(ProjectSubContractorScannedDoc.FileUrl));
            string cSDFileName = ctx.Col<ProjectSubContractorScannedDoc>(nameof(ProjectSubContractorScannedDoc.OriginalFileName));
            string cSDState = ctx.Col<ProjectSubContractorScannedDoc>(nameof(ProjectSubContractorScannedDoc.State));

            var parameters = new DynamicParameters();
            parameters.Add("@PageSize", pageSize);
            parameters.Add("@PageOffset", offset);

            var whereConditions = new List<string> { $"psc.{cPscState} = TRUE" };

            if (filter.AllowedProjectIds != null)
            {
                // Restricción de Oficina Técnica: solo sus proyectos.
                whereConditions.Add($"psc.{cPscProjectId} = ANY(@AllowedProjectIds)");
                parameters.Add("@AllowedProjectIds", filter.AllowedProjectIds.ToArray());
            }

            if (filter.ProjectId.HasValue)
            {
                whereConditions.Add($"psc.{cPscProjectId} = @ProjectId");
                parameters.Add("@ProjectId", filter.ProjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.ContributorName))
            {
                whereConditions.Add($"c.{cContributorName} ILIKE @ContributorName");
                parameters.Add("@ContributorName", $"%{filter.ContributorName}%");
            }

            if (!string.IsNullOrWhiteSpace(filter.ContributorRuc))
            {
                whereConditions.Add($"c.{cContributorRuc} ILIKE @ContributorRuc");
                parameters.Add("@ContributorRuc", $"%{filter.ContributorRuc}%");
            }

            if (filter.ContractTypeId.HasValue)
            {
                whereConditions.Add($"psc.{cPscContractTypeId} = @ContractTypeId");
                parameters.Add("@ContractTypeId", filter.ContractTypeId.Value);
            }

            if (filter.ContractModalityId.HasValue)
            {
                whereConditions.Add($"psc.{cPscContractModalityId} = @ContractModalityId");
                parameters.Add("@ContractModalityId", filter.ContractModalityId.Value);
            }

            if (filter.PaymentMethodId.HasValue)
            {
                whereConditions.Add($"psc.{cPscPaymentMethodId} = @PaymentMethodId");
                parameters.Add("@PaymentMethodId", filter.PaymentMethodId.Value);
            }

            if (filter.ProjectSubContractorStatusId.HasValue)
            {
                whereConditions.Add($"psc.{cPscStatusId} = @ProjectSubContractorStatusId");
                parameters.Add("@ProjectSubContractorStatusId", filter.ProjectSubContractorStatusId.Value);
            }

            if (filter.CreatedUserId.HasValue)
            {
                whereConditions.Add($"psc.{cPscCreatedUserId} = @CreatedUserId");
                parameters.Add("@CreatedUserId", filter.CreatedUserId.Value);
            }

            var whereClause = string.Join(" AND ", whereConditions);

            string sql = $@"
-- 1. Count
SELECT COUNT(DISTINCT psc.{cPscId}) AS ""Total""
FROM {tPsc} psc
JOIN {tContractor} contractor ON psc.{cPscContractorId} = contractor.{cContractorId}
JOIN {tContributor} c ON contractor.{cContractorContribId} = c.{cContributorId}
WHERE {whereClause};

-- 2. Paged Data (con aliases PascalCase para que Dapper mapee a dynamic / DTOs)
SELECT psc.{cPscId} AS ""ProjectSubContractorId"",
       psc.{cPscProjectId} AS ""ProjectId"",
       p.{cProjectDesc} AS ""ProjectDescription"",
       psc.{cPscContractorId} AS ""ContractorId"",
       c.{cContributorId} AS ""ContributorId"",
       c.{cContributorName} AS ""ContributorName"",
       psc.{cPscContractTypeId} AS ""ContractTypeId"",
       ct.{cContractTypeDesc} AS ""ContractTypeDescription"",
       psc.{cPscContractModalityId} AS ""ContractModalityId"",
       cm.{cContractModalityDescDapper} AS ""ContractModalityDescription"",
       psc.{cPscPaymentMethodId} AS ""PaymentMethodId"",
       pm.{cPaymentMethodDesc} AS ""PaymentMethodDescription"",
       psc.{cPscPaymentFormId} AS ""PaymentFormId"",
       pf.{cPaymentFormDesc} AS ""PaymentFormDescription"",
       psc.{cPscIncludesCartaFianza} AS ""IncludesCartaFianza"",
       psc.{cPscAdvancePercentage} AS ""AdvancePercentage"",
       psc.{cPscAdvanceAmount} AS ""AdvanceAmount"",
       psc.{cPscAmount} AS ""Amount"",
       psc.{cPscCurrencyId} AS ""CurrencyId"",
       cur.{cCurrencyCode} AS ""CurrencyCode"",
       psc.{cPscHasIgv} AS ""HasIgv"",
       psc.{cPscWorkItemId} AS ""WorkItemId"",
       wi.{cWorkItemDesc} AS ""WorkItemDescription"",
       psc.{cPscIsSubcontract} AS ""IsSubcontract"",
       psc.{cPscIsLabor} AS ""IsLabor"",
       psc.{cPscContractWorkItemName} AS ""ContractWorkItemName"",
       psc.{cPscWorkItemCategoryId} AS ""WorkItemCategoryId"",
       wic.{cWorkItemCategoryDesc} AS ""WorkItemCategoryDescription"",
       wic.{cWorkItemCategorySyncStatus} AS work_item_category_instructivos_sync_status,
       wic.{cWorkItemCategoryFolderName} AS work_item_category_instructivos_folder_name,
       psc.{cPscWorkSpecialtyId} AS ""WorkSpecialtyId"",
       ws.{cWorkSpecialtyDescDapper} AS work_specialty_description,
       psc.{cPscStatusId} AS ""ProjectSubContractorStatusId"",
       pscs.{cStatusDesc} AS ""ProjectSubContractorStatusDescription"",
       psc.{cPscSigningDate} AS ""SigningDate"",
       psc.{cPscStartDate} AS ""StartDate"",
       psc.{cPscEndDate} AS ""EndDate"",
       psc.{cPscTermDays} AS ""TermDays"",
       psc.{cPscContractNumber} AS ""ContractNumber"",
       psc.{cPscPromissoryNoteNumber} AS ""PromissoryNoteNumber"",
       psc.{cPscGuaranteeFundPercentage} AS ""GuaranteeFundPercentage"",
       psc.{cPscGuaranteeFundDays} AS ""GuaranteeFundDays"",
       psc.{cPscGuaranteeValidityDays} AS ""GuaranteeValidityDays"",
       psc.{cPscPaymentDays} AS ""PaymentDays"",
       psc.{cPscArrivedWithObservations} AS ""ArrivedWithObservations"",
       psc.{cPscArrivalObservation} AS ""ArrivalObservation"",
       psc.{cPscStep6SignedCostos} AS ""Step6SignedCostos"",
       psc.{cPscStep6SignedGerenteInmobiliario} AS ""Step6SignedGerenteInmobiliario"",
       psc.{cPscStep6SignedGerenteGeneral} AS ""Step6SignedGerenteGeneral"",
       psc.{cPscNonConformingStatusId} AS ""NonConformingOutputStatusId"",
       psc.{cPscToleranceStatusId} AS ""ToleranceChartStatusId"",
       psc.{cPscFinishProtectionStatusId} AS ""FinishProtectionStatusId"",
       psc.{cPscCreatedDateTime} AS ""CreatedDateTime"",
       creator_p.{cPersonCreatorFullName} AS ""CreatedUserFullName"",
       contractDoc.{cContractDocFileUrl} AS contract_file_url,
       contractDoc.{cContractDocFileName} AS contract_file_name,
       contractDoc.{cContractDocStatusId} AS contract_status_id,
       fs_contract.{cFileStatusDesc} AS contract_status_desc,
       contractDoc.{cContractDocObs} AS contract_observation,
       summaryDoc.{cSummaryDocFileUrl} AS summary_sheet_file_url,
       summaryDoc.{cSummaryDocFileName} AS summary_sheet_file_name,
       summaryDoc.{cSummaryDocStatusId} AS summary_sheet_status_id,
       fs_summary.{cFileStatusDesc} AS summary_sheet_status_desc,
       summaryDoc.{cSummaryDocObs} AS summary_sheet_observation,
       budgetDoc.{cBudgetDocFileUrl} AS budget_file_url,
       budgetDoc.{cBudgetDocFileName} AS budget_file_name,
       budgetDoc.{cBudgetDocStatusId} AS budget_status_id,
       fs_budget.{cFileStatusDesc} AS budget_status_desc,
       budgetDoc.{cBudgetDocObs} AS budget_observation,
       scheduleDoc.{cScheduleDocFileUrl} AS schedule_file_url,
       scheduleDoc.{cScheduleDocFileName} AS schedule_file_name,
       scheduleDoc.{cScheduleDocStatusId} AS schedule_status_id,
       fs_schedule.{cFileStatusDesc} AS schedule_status_desc,
       scheduleDoc.{cScheduleDocObs} AS schedule_observation,
       attQuotDoc.{cAttQuotDocFileUrl} AS attached_quotation_file_url,
       attQuotDoc.{cAttQuotDocFileName} AS attached_quotation_file_name,
       attQuotDoc.{cAttQuotDocStatusId} AS attached_quotation_status_id,
       fs_att_quot.{cFileStatusDesc} AS attached_quotation_status_desc,
       attQuotDoc.{cAttQuotDocObs} AS attached_quotation_observation,
       svcOrderDoc.{cSvcOrderDocFileUrl} AS service_order_file_url,
       svcOrderDoc.{cSvcOrderDocFileName} AS service_order_file_name,
       svcOrderDoc.{cSvcOrderDocStatusId} AS service_order_status_id,
       fs_svc_order.{cFileStatusDesc} AS service_order_status_desc,
       svcOrderDoc.{cSvcOrderDocObs} AS service_order_observation,
       pNoteDoc.{cPNoteDocFileUrl} AS promissory_note_file_url,
       pNoteDoc.{cPNoteDocFileName} AS promissory_note_file_name,
       pNoteDoc.{cPNoteDocStatusId} AS promissory_note_status_id,
       fs_p_note.{cFileStatusDesc} AS promissory_note_status_desc,
       pNoteDoc.{cPNoteDocObs} AS promissory_note_observation,
       packageDoc.{cPackageDocFileUrl} AS package_file_url,
       packageDoc.{cPackageDocFileName} AS package_file_name,
       instructivoDoc.{cInstructivoDocFileUrl} AS instructivo_file_url,
       instructivoDoc.{cInstructivoDocFileName} AS instructivo_file_name,
       instructivoDoc.{cInstructivoDocStatusId} AS instructivo_status_id,
       fs_instructivo.{cFileStatusDesc} AS instructivo_status_desc,
       instructivoDoc.{cInstructivoDocObs} AS instructivo_observation,
       nonConformingDoc.{cNonConformingDocFileUrl} AS non_conforming_file_url,
       nonConformingDoc.{cNonConformingDocFileName} AS non_conforming_file_name,
       nonConformingDoc.{cNonConformingDocStatusId} AS non_conforming_status_id,
       fs_non_conforming.{cFileStatusDesc} AS non_conforming_status_desc,
       nonConformingDoc.{cNonConformingDocObs} AS non_conforming_observation,
       toleranceChartDoc.{cToleranceChartDocFileUrl} AS tolerance_chart_file_url,
       toleranceChartDoc.{cToleranceChartDocFileName} AS tolerance_chart_file_name,
       toleranceChartDoc.{cToleranceChartDocStatusId} AS tolerance_chart_status_id,
       fs_tolerance_chart.{cFileStatusDesc} AS tolerance_chart_status_desc,
       toleranceChartDoc.{cToleranceChartDocObs} AS tolerance_chart_observation,
       fichaTecnicaDoc.{cFichaTecnicaDocFileUrl} AS ficha_tecnica_file_url,
       fichaTecnicaDoc.{cFichaTecnicaDocFileName} AS ficha_tecnica_file_name,
       fichaTecnicaDoc.{cFichaTecnicaDocStatusId} AS ficha_tecnica_status_id,
       fs_ficha_tecnica.{cFileStatusDesc} AS ficha_tecnica_status_desc,
       fichaTecnicaDoc.{cFichaTecnicaDocObs} AS ficha_tecnica_observation,
       anexoDoc.{cAnexoDocFileUrl} AS anexo_file_url,
       anexoDoc.{cAnexoDocFileName} AS anexo_file_name,
       anexoDoc.{cAnexoDocStatusId} AS anexo_status_id,
       fs_anexo.{cFileStatusDesc} AS anexo_status_desc,
       anexoDoc.{cAnexoDocObs} AS anexo_observation
FROM {tPsc} psc
JOIN {tProject} p ON psc.{cPscProjectId} = p.{cProjectId}
JOIN {tContractor} contractor ON psc.{cPscContractorId} = contractor.{cContractorId}
JOIN {tContributor} c ON contractor.{cContractorContribId} = c.{cContributorId}
JOIN {tContractType} ct ON psc.{cPscContractTypeId} = ct.{cContractTypeId}
LEFT JOIN {tContractModalityDapper} cm ON psc.{cPscContractModalityId} = cm.{cContractModalityIdDapper}
JOIN {tPaymentMethod} pm ON psc.{cPscPaymentMethodId} = pm.{cPaymentMethodId}
LEFT JOIN {tPaymentForm} pf ON psc.{cPscPaymentFormId} = pf.{cPaymentFormId}
JOIN {tCurrency} cur ON psc.{cPscCurrencyId} = cur.{cCurrencyId}
JOIN {tWorkItem} wi ON psc.{cPscWorkItemId} = wi.{cWorkItemId}
JOIN {tStatus} pscs ON psc.{cPscStatusId} = pscs.{cStatusId}
JOIN {tWorkItemCategory} wic ON psc.{cPscWorkItemCategoryId} = wic.{cWorkItemCategoryId}
LEFT JOIN {tWorkSpecialtyDapper} ws ON psc.{cPscWorkSpecialtyId} = ws.{cWorkSpecialtyIdDapper}
LEFT JOIN {tContractDoc} contractDoc ON psc.{cPscContractDocId} = contractDoc.{cContractDocId}
LEFT JOIN {tFileStatus} fs_contract ON contractDoc.{cContractDocStatusId} = fs_contract.{cFileStatusId}
LEFT JOIN {tSummaryDoc} summaryDoc ON psc.{cPscSummarySheetDocId} = summaryDoc.{cSummaryDocId}
LEFT JOIN {tFileStatus} fs_summary ON summaryDoc.{cSummaryDocStatusId} = fs_summary.{cFileStatusId}
LEFT JOIN {tBudgetDoc} budgetDoc ON psc.{cPscBudgetDocId} = budgetDoc.{cBudgetDocId}
LEFT JOIN {tFileStatus} fs_budget ON budgetDoc.{cBudgetDocStatusId} = fs_budget.{cFileStatusId}
LEFT JOIN {tScheduleDoc} scheduleDoc ON psc.{cPscScheduleDocId} = scheduleDoc.{cScheduleDocId}
LEFT JOIN {tFileStatus} fs_schedule ON scheduleDoc.{cScheduleDocStatusId} = fs_schedule.{cFileStatusId}
LEFT JOIN {tAttQuotDoc} attQuotDoc ON psc.{cPscAttachedQuotationDocId} = attQuotDoc.{cAttQuotDocId}
LEFT JOIN {tFileStatus} fs_att_quot ON attQuotDoc.{cAttQuotDocStatusId} = fs_att_quot.{cFileStatusId}
LEFT JOIN {tSvcOrderDoc} svcOrderDoc ON psc.{cPscServiceOrderDocId} = svcOrderDoc.{cSvcOrderDocId}
LEFT JOIN {tFileStatus} fs_svc_order ON svcOrderDoc.{cSvcOrderDocStatusId} = fs_svc_order.{cFileStatusId}
LEFT JOIN {tPNoteDoc} pNoteDoc ON psc.{cPscPromissoryNoteDocId} = pNoteDoc.{cPNoteDocId}
LEFT JOIN {tFileStatus} fs_p_note ON pNoteDoc.{cPNoteDocStatusId} = fs_p_note.{cFileStatusId}
LEFT JOIN {tPackageDoc} packageDoc ON psc.{cPscPackageDocId} = packageDoc.{cPackageDocId}
LEFT JOIN {tInstructivoDoc} instructivoDoc ON psc.{cPscInstructivoDocId} = instructivoDoc.{cInstructivoDocId}
LEFT JOIN {tFileStatus} fs_instructivo ON instructivoDoc.{cInstructivoDocStatusId} = fs_instructivo.{cFileStatusId}
LEFT JOIN {tNonConformingDoc} nonConformingDoc ON psc.{cPscNonConformingOutputDocId} = nonConformingDoc.{cNonConformingDocId}
LEFT JOIN {tFileStatus} fs_non_conforming ON nonConformingDoc.{cNonConformingDocStatusId} = fs_non_conforming.{cFileStatusId}
LEFT JOIN {tToleranceChartDoc} toleranceChartDoc ON psc.{cPscToleranceChartDocId} = toleranceChartDoc.{cToleranceChartDocId}
LEFT JOIN {tFileStatus} fs_tolerance_chart ON toleranceChartDoc.{cToleranceChartDocStatusId} = fs_tolerance_chart.{cFileStatusId}
LEFT JOIN {tFichaTecnicaDoc} fichaTecnicaDoc ON psc.{cPscFichaTecnicaDocId} = fichaTecnicaDoc.{cFichaTecnicaDocId}
LEFT JOIN {tFileStatus} fs_ficha_tecnica ON fichaTecnicaDoc.{cFichaTecnicaDocStatusId} = fs_ficha_tecnica.{cFileStatusId}
LEFT JOIN {tAnexoDoc} anexoDoc ON psc.{cPscAnexoDocId} = anexoDoc.{cAnexoDocId}
LEFT JOIN {tFileStatus} fs_anexo ON anexoDoc.{cAnexoDocStatusId} = fs_anexo.{cFileStatusId}
LEFT JOIN {tPersonCreator} creator_p ON creator_p.{cPersonCreatorUserId} = psc.{cPscCreatedUserId}
WHERE {whereClause}
ORDER BY psc.{cPscId} DESC
LIMIT @PageSize OFFSET @PageOffset;

-- 3-12. Form data queries (10 simple selects con aliases PascalCase para mapeo a DTOs)
SELECT {cProjectId} AS ""ProjectId"", {cProjectDesc} AS ""ProjectDescription"" FROM {tProject} WHERE {cProjectActive} = TRUE{(filter.AllowedProjectIds != null ? $" AND {cProjectId} = ANY(@AllowedProjectIds)" : string.Empty)} ORDER BY {cProjectDesc};
SELECT {cContractTypeId} AS ""ContractTypeId"", {cContractTypeDesc} AS ""ContractTypeDescription"" FROM {tContractType} WHERE {cContractTypeActive} = TRUE ORDER BY {cContractTypeDesc};
SELECT {cContractModalityIdDapper} AS ""ContractModalityId"", {cContractModalityDescDapper} AS ""ContractModalityDescription"" FROM {tContractModalityDapper} WHERE {cContractModalityStateDapper} = TRUE ORDER BY {cContractModalityIdDapper};
SELECT {cPaymentMethodId} AS ""PaymentMethodId"", {cPaymentMethodDesc} AS ""PaymentMethodDescription"" FROM {tPaymentMethod} WHERE {cPaymentMethodActive} = TRUE ORDER BY {cPaymentMethodDesc};
SELECT {cCurrencyId} AS ""CurrencyId"", {cCurrencyDesc} AS ""CurrencyDescription"", {cCurrencyCode} AS ""CurrencyCode"", {cCurrencySymbol} AS ""CurrencySymbol"" FROM {tCurrency} WHERE {cCurrencyActive} = TRUE ORDER BY {cCurrencyCode};
SELECT {cWorkItemId} AS ""WorkItemId"", {cWorkItemDesc} AS ""WorkItemDescription"" FROM {tWorkItem} WHERE {cWorkItemActive} = TRUE ORDER BY {cWorkItemDesc};
SELECT {cWorkItemCategoryId} AS ""WorkItemCategoryId"", {cWorkItemCategoryDesc} AS ""WorkItemCategoryDescription"" FROM {tWorkItemCategory} WHERE {cWorkItemCategoryActive} = TRUE ORDER BY {cWorkItemCategoryDesc};
SELECT {cStatusId} AS ""ProjectSubContractorStatusId"", {cStatusDesc} AS ""ProjectSubContractorStatusDescription"" FROM {tStatus} ORDER BY {cStatusId};
SELECT ct.{cContractorId} AS ""ContractorId"", contrib.{cContributorId} AS ""ContributorId"", contrib.{cContributorName} AS ""ContributorName"", contrib.{cContributorRuc} AS ""ContributorRuc""
FROM {tContractor} ct
JOIN {tContributor} contrib ON contrib.{cContributorId} = ct.{cContractorContribId}
WHERE ct.{cContractorActive} = TRUE AND ct.{cContractorState} = TRUE AND ct.{cContractorStateId} = 2
ORDER BY contrib.{cContributorName};

-- 12-14. Supporting data
SELECT {cCEContractorId} AS ""ContractorId"", {cCEEmail} AS ""Email"" FROM {tContractorEmail} WHERE {cCEActive} = TRUE;
SELECT {cQFId} AS ""FileId"", {cQFPscId} AS ""ProjectSubContractorId"", {cQFFileUrl} AS ""FileUrl"", {cQFFileName} AS ""OriginalFileName"" FROM {tQuotFile} WHERE {cQFState} = TRUE;
SELECT {cCFId} AS ""FileId"", {cCFPscId} AS ""ProjectSubContractorId"", {cCFFileUrl} AS ""FileUrl"", {cCFFileName} AS ""OriginalFileName"" FROM {tCompFile} WHERE {cCFState} = TRUE;
SELECT {cSDPscId} AS ""ProjectSubContractorId"", {cSDSlot} AS ""Slot"", {cSDFileUrl} AS ""FileUrl"", {cSDFileName} AS ""OriginalFileName"" FROM {tScannedFile} WHERE {cSDState} = TRUE;
SELECT {cWorkItemValFormWorkItemId} AS work_item_id, {cWorkItemValFormConcept} AS concept, {cWorkItemValFormPercentage} AS percentage, {cWorkItemValFormSortOrder} AS sort_order FROM {tWorkItemValForm} WHERE {cWorkItemValFormState} = TRUE ORDER BY {cWorkItemValFormWorkItemId}, {cWorkItemValFormSortOrder};
            ";

            using var multi = await connection.QueryMultipleAsync(sql, parameters);

            // Read COUNT (PostgreSQL devuelve bigint, por eso casteamos desde long)
            var countResult = await multi.ReadFirstOrDefaultAsync<dynamic>();
            int totalRecords = countResult == null ? 0 : Convert.ToInt32(countResult.Total);

            // Read paged data
            var itemsRaw = (await multi.ReadAsync<dynamic>()).ToList();

            // Read form data queries
            var projects = (await multi.ReadAsync<ProjectSimpleDTO>()).ToList();
            var contractTypes = (await multi.ReadAsync<ContractTypeSimpleDTO>()).ToList();
            var contractModalities = (await multi.ReadAsync<ContractModalitySimpleDTO>()).ToList();
            var paymentMethods = (await multi.ReadAsync<PaymentMethodSimpleDTO>()).ToList();
            var currencies = (await multi.ReadAsync<CurrencySimpleDTO>()).ToList();
            var workItems = (await multi.ReadAsync<WorkItemSimpleDTO>()).ToList();
            var workItemCategories = (await multi.ReadAsync<WorkItemCategorySimpleDTO>()).ToList();
            var statuses = (await multi.ReadAsync<ProjectSubContractorStatusSimpleDTO>()).ToList();
            var contractors = (await multi.ReadAsync<ContributorFactoryDTO>()).ToList();

            // Read supporting data (using dynamic to avoid tuple-naming issues)
            var emailsRaw = (await multi.ReadAsync<dynamic>()).ToList();
            var quotationFilesRaw = (await multi.ReadAsync<dynamic>()).ToList();
            var comparativeFilesRaw = (await multi.ReadAsync<dynamic>()).ToList();
            var scannedFilesRaw = (await multi.ReadAsync<dynamic>()).ToList();
            var valorizationForms = (await multi.ReadAsync<WorkItemValorizationFormSimpleDTO>()).ToList();

            var emails = emailsRaw.Select(e => new { ContractorId = (int)e.ContractorId, Email = (string)e.Email }).ToList();
            var quotationFiles = quotationFilesRaw.Select(f => new { FileId = (int)f.FileId, ProjectSubContractorId = (int)f.ProjectSubContractorId, FileUrl = (string)f.FileUrl, OriginalFileName = (string)f.OriginalFileName }).ToList();
            var comparativeFiles = comparativeFilesRaw.Select(f => new { FileId = (int)f.FileId, ProjectSubContractorId = (int)f.ProjectSubContractorId, FileUrl = (string)f.FileUrl, OriginalFileName = (string)f.OriginalFileName }).ToList();
            var scannedFiles = scannedFilesRaw.Select(f => new { ProjectSubContractorId = (int)f.ProjectSubContractorId, Slot = (int)f.Slot, FileUrl = (string)f.FileUrl, OriginalFileName = (string)f.OriginalFileName }).ToList();

            // Build dictionaries for supporting data
            var emailsByContractor = emails.GroupBy(e => e.ContractorId).ToDictionary(g => g.Key, g => g.Select(e => e.Email).ToList());
            var quotationByPsc = quotationFiles.GroupBy(f => f.ProjectSubContractorId).ToDictionary(g => g.Key, g => g.Select(f => new ProjectSubContractorFileDto { FileId = f.FileId, FileUrl = f.FileUrl, OriginalFileName = f.OriginalFileName }).ToList());
            var comparativeByPsc = comparativeFiles.GroupBy(f => f.ProjectSubContractorId).ToDictionary(g => g.Key, g => g.Select(f => new ProjectSubContractorFileDto { FileId = f.FileId, FileUrl = f.FileUrl, OriginalFileName = f.OriginalFileName }).ToList());
            // Escaneados agrupados por adjudicación y luego por slot (1/2/3).
            var scannedByPsc = scannedFiles.GroupBy(f => f.ProjectSubContractorId).ToDictionary(g => g.Key, g => g.ToDictionary(f => f.Slot));
            var formsByWorkItem = valorizationForms.GroupBy(f => f.WorkItemId).ToDictionary(g => g.Key, g => g.OrderBy(f => f.SortOrder).ToList());

            // Map contractors with emails (for form data / create dropdown)
            foreach (var contractor in contractors)
                contractor.Emails = emailsByContractor.GetValueOrDefault(contractor.ContractorId, new());

            // Map items from dynamic to DTO
            var items = new List<ProjectSubContractorDTO>();
            foreach (var raw in itemsRaw)
            {
                items.Add(new ProjectSubContractorDTO
                {
                    ProjectSubContractorId = (int)raw.ProjectSubContractorId,
                    ProjectId = (int)raw.ProjectId,
                    ProjectDescription = raw.ProjectDescription ?? "",
                    ContractorId = (int)raw.ContractorId,
                    ContributorId = (int)raw.ContributorId,
                    ContributorName = raw.ContributorName ?? "",
                    ContractTypeId = (int)raw.ContractTypeId,
                    ContractTypeDescription = raw.ContractTypeDescription ?? "",
                    ContractModalityId = (int?)raw.ContractModalityId,
                    ContractModalityDescription = (string?)raw.ContractModalityDescription,
                    PaymentMethodId = (int)raw.PaymentMethodId,
                    PaymentMethodDescription = raw.PaymentMethodDescription ?? "",
                    PaymentFormId = (int?)raw.PaymentFormId,
                    PaymentFormDescription = (string?)raw.PaymentFormDescription,
                    IncludesCartaFianza = (bool)raw.IncludesCartaFianza,
                    AdvancePercentage = (decimal?)raw.AdvancePercentage,
                    AdvanceAmount = (decimal?)raw.AdvanceAmount,
                    Amount = (decimal?)raw.Amount ?? 0m,
                    CurrencyId = (int)raw.CurrencyId,
                    CurrencyCode = raw.CurrencyCode ?? "",
                    AmountHasIgv = (bool)raw.HasIgv,
                    WorkItemId = (int)raw.WorkItemId,
                    WorkItemDescription = raw.WorkItemDescription ?? "",
                    IsSubcontract = (bool)raw.IsSubcontract,
                    IsLabor = (bool)raw.IsLabor,
                    ContractWorkItemName = (string?)raw.ContractWorkItemName,
                    WorkItemCategoryId = (int)raw.WorkItemCategoryId,
                    WorkItemCategoryDescription = raw.WorkItemCategoryDescription ?? "",
                    WorkItemCategoryInstructivosSyncStatus = (int?)raw.work_item_category_instructivos_sync_status,
                    WorkItemCategoryInstructivosFolderName = (string?)raw.work_item_category_instructivos_folder_name,
                    WorkSpecialtyId = (int?)raw.WorkSpecialtyId,
                    WorkSpecialtyDescription = (string?)raw.work_specialty_description,
                    ProjectSubContractorStatusId = (int)raw.ProjectSubContractorStatusId,
                    ProjectSubContractorStatusDescription = raw.ProjectSubContractorStatusDescription ?? "",
                    SigningDate = ToDateOnly(raw.SigningDate),
                    StartDate = ToDateOnly(raw.StartDate),
                    EndDate = ToDateOnly(raw.EndDate),
                    TermDays = (int?)raw.TermDays,
                    ContractNumber = (int?)raw.ContractNumber,
                    PromissoryNoteNumber = (int?)raw.PromissoryNoteNumber,
                    GuaranteeFundPercentage = (int?)raw.GuaranteeFundPercentage,
                    GuaranteeFundDays = (int?)raw.GuaranteeFundDays,
                    GuaranteeValidityDays = (int?)raw.GuaranteeValidityDays,
                    PaymentDays = (int)raw.PaymentDays,
                    ArrivedWithObservations = (bool?)raw.ArrivedWithObservations,
                    ArrivalObservation = (string?)raw.ArrivalObservation,
                    Step6SignedCostos = (bool)raw.Step6SignedCostos,
                    Step6SignedGerenteInmobiliario = (bool)raw.Step6SignedGerenteInmobiliario,
                    Step6SignedGerenteGeneral = (bool)raw.Step6SignedGerenteGeneral,
                    CreatedDateTime = new DateTimeOffset((DateTime)raw.CreatedDateTime, TimeSpan.Zero),
                    CreatedUserFullName = (string?)raw.CreatedUserFullName,
                    Contract = MapDocDto((string?)raw.contract_file_url, (string?)raw.contract_file_name, (int?)raw.contract_status_id, (string?)raw.contract_status_desc, (string?)raw.contract_observation),
                    SummarySheet = MapDocDto((string?)raw.summary_sheet_file_url, (string?)raw.summary_sheet_file_name, (int?)raw.summary_sheet_status_id, (string?)raw.summary_sheet_status_desc, (string?)raw.summary_sheet_observation),
                    Budget = MapDocDto((string?)raw.budget_file_url, (string?)raw.budget_file_name, (int?)raw.budget_status_id, (string?)raw.budget_status_desc, (string?)raw.budget_observation),
                    Schedule = MapDocDto((string?)raw.schedule_file_url, (string?)raw.schedule_file_name, (int?)raw.schedule_status_id, (string?)raw.schedule_status_desc, (string?)raw.schedule_observation),
                    AttachedQuotation = MapDocDto((string?)raw.attached_quotation_file_url, (string?)raw.attached_quotation_file_name, (int?)raw.attached_quotation_status_id, (string?)raw.attached_quotation_status_desc, (string?)raw.attached_quotation_observation),
                    ServiceOrder = MapDocDto((string?)raw.service_order_file_url, (string?)raw.service_order_file_name, (int?)raw.service_order_status_id, (string?)raw.service_order_status_desc, (string?)raw.service_order_observation),
                    PromissoryNote = MapDocDto((string?)raw.promissory_note_file_url, (string?)raw.promissory_note_file_name, (int?)raw.promissory_note_status_id, (string?)raw.promissory_note_status_desc, (string?)raw.promissory_note_observation),
                    Package = raw.package_file_url != null ? new ProjectSubContractorFileDto { FileUrl = raw.package_file_url, OriginalFileName = raw.package_file_name } : null,
                    Instructivo = MapDocDto((string?)raw.instructivo_file_url, (string?)raw.instructivo_file_name, (int?)raw.instructivo_status_id, (string?)raw.instructivo_status_desc, (string?)raw.instructivo_observation),
                    NonConformingOutput = ((int?)raw.NonConformingOutputStatusId) != null ? new ProjectSubContractorFileDto { StatusId = (int?)raw.NonConformingOutputStatusId } : null,
                    ToleranceChart = ((int?)raw.ToleranceChartStatusId) != null ? new ProjectSubContractorFileDto { StatusId = (int?)raw.ToleranceChartStatusId } : null,
                    FinishProtection = ((int?)raw.FinishProtectionStatusId) != null ? new ProjectSubContractorFileDto { StatusId = (int?)raw.FinishProtectionStatusId } : null,
                    FichaTecnica = MapDocDto((string?)raw.ficha_tecnica_file_url, (string?)raw.ficha_tecnica_file_name, (int?)raw.ficha_tecnica_status_id, (string?)raw.ficha_tecnica_status_desc, (string?)raw.ficha_tecnica_observation),
                    Anexo = MapDocDto((string?)raw.anexo_file_url, (string?)raw.anexo_file_name, (int?)raw.anexo_status_id, (string?)raw.anexo_status_desc, (string?)raw.anexo_observation),
                });
            }

            // Map contractor emails + formas de valorización + archivos de cotización/comparativo onto each paged item
            foreach (var item in items)
            {
                item.ContractorEmails = emailsByContractor.GetValueOrDefault(item.ContractorId, new());
                item.WorkItemValorizationForms = formsByWorkItem.GetValueOrDefault(item.WorkItemId, new());
                item.QuotationFiles   = quotationByPsc.GetValueOrDefault(item.ProjectSubContractorId, new());
                item.ComparativeFiles = comparativeByPsc.GetValueOrDefault(item.ProjectSubContractorId, new());

                // Documentos escaneados del paso 7 (slots 1/2/3), para que se vean en los pasos 7 y 8.
                if (scannedByPsc.TryGetValue(item.ProjectSubContractorId, out var slots))
                {
                    if (slots.TryGetValue(1, out var s1))
                        item.ScannedDoc1 = new ProjectSubContractorFileDto { FileUrl = s1.FileUrl, OriginalFileName = s1.OriginalFileName };
                    if (slots.TryGetValue(2, out var s2))
                        item.ScannedDoc2 = new ProjectSubContractorFileDto { FileUrl = s2.FileUrl, OriginalFileName = s2.OriginalFileName };
                    if (slots.TryGetValue(3, out var s3))
                        item.ScannedDoc3 = new ProjectSubContractorFileDto { FileUrl = s3.FileUrl, OriginalFileName = s3.OriginalFileName };
                }
            }

            var formDataDto = new ProjectSubContractorFormDataDTO
            {
                Projects = projects,
                ContractTypes = contractTypes,
                ContractModalities = contractModalities,
                PaymentMethods = paymentMethods,
                Currencies = currencies,
                WorkItems = workItems,
                WorkItemCategories = workItemCategories,
                ProjectSubContractorStatuses = statuses,
                Contributors = contractors
            };

            int totalPages = (totalRecords + pageSize - 1) / pageSize;

            return new ProjectSubContractorPagedWithFiltersDTO
            {
                Paged = new PagedResult<ProjectSubContractorDTO>
                {
                    Page = filter.Page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    TotalPages = totalPages,
                    Data = items
                },
                Filters = formDataDto
            };
        }

        public async Task<AdjudicacionNotificationDataDto> GetNotificationData(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .Include(x => x.QuotationFiles.Where(f => f.State))
                .Include(x => x.ComparativeFiles.Where(f => f.State))
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State);

            if (psc is null)
                throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 1)
                throw new AbrilException("La adjudicación ya fue notificada o no está en estado pendiente.");

            var projectDescription = await _context.Project
                .Where(p => p.ProjectId == psc.ProjectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync() ?? string.Empty;

            var workItem = await _context.WorkItem
                .FirstOrDefaultAsync(w => w.WorkItemId == psc.WorkItemId);

            // Proyección solo del nombre — evita SELECT de columnas opcionales del Contributor
            // (sp_password_temp, es_abril, contributor_nombre_comercial) que pueden no existir
            // en todos los entornos.
            var contributorName = await (
                from ct in _context.Contractor
                join contrib in _context.Contributor on ct.ContributorId equals contrib.ContributorId
                where ct.ContractorId == psc.ContractorId
                select contrib.ContributorName
            ).FirstOrDefaultAsync();

            var allEmails = await _context.StaffProjectEmail
                .Where(s => s.ProjectId == psc.ProjectId && s.State && s.Active)
                .Select(s => new { s.Email, s.StaffProjectEmailTypeId })
                .ToListAsync();

            // Tipo 1 = Staff de obra → matriz + CC | Tipo 2 = Oficina central → solo CC
            var staffEmails          = allEmails.Where(e => e.StaffProjectEmailTypeId == 1).Select(e => e.Email).ToList();
            var oficinaCentralEmails = allEmails.Where(e => e.StaffProjectEmailTypeId == 2).Select(e => e.Email).ToList();

            // Todos los correos registrados en la tabla contractor_email para este contratista
            var contractorEmails = await _context.ContractorEmail
                .Where(ce => ce.ContractorId == psc.ContractorId && ce.State && ce.Active)
                .Select(ce => ce.Email)
                .ToListAsync();

            return new AdjudicacionNotificationDataDto
            {
                ProjectSubContractorId       = psc.ProjectSubContractorId,
                ProjectSubContractorStatusId = psc.ProjectSubContractorStatusId,
                ProjectDescription           = projectDescription,
                WorkItemDescription          = workItem?.WorkItemDescription ?? string.Empty,
                ContributorName              = contributorName ?? string.Empty,
                ContractorEmails             = contractorEmails,
                StaffEmails                  = staffEmails,
                OficinaCentralEmails         = oficinaCentralEmails,
                QuotationFiles = psc.QuotationFiles.Select(f => new ProjectSubContractorFileDto
                {
                    FileUrl = f.FileUrl,
                    OriginalFileName = f.OriginalFileName
                }).ToList(),
                ComparativeFiles = psc.ComparativeFiles.Select(f => new ProjectSubContractorFileDto
                {
                    FileUrl = f.FileUrl,
                    OriginalFileName = f.OriginalFileName
                }).ToList()
            };
        }

        public async Task UpdateStatusToSent(int projectSubContractorId, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State);

            if (psc is null)
                throw new AbrilException("La adjudicación no existe.");

            psc.ProjectSubContractorStatusId = 2;
            psc.UpdatedDateTime = DateTimeOffset.UtcNow;
            psc.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task UpdateStatus(int projectSubContractorId, int statusId, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            psc.ProjectSubContractorStatusId = statusId;
            psc.UpdatedDateTime = DateTimeOffset.UtcNow;
            psc.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task SetArrivalOptionAsync(int projectSubContractorId, bool arrivedWithObservations, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 5)
                throw new AbrilException("La adjudicación no está en el paso de llegada a oficina central.");

            psc.ArrivedWithObservations = arrivedWithObservations;
            psc.UpdatedDateTime         = DateTimeOffset.UtcNow;
            psc.UpdatedUserId           = userId;

            await _context.SaveChangesAsync();
        }

        public async Task ConfirmStep5Async(int projectSubContractorId, bool arrivedWithObservations, string? arrivalObservation, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 5)
                throw new AbrilException("La adjudicación no está en el paso de llegada a oficina central.");

            psc.ArrivedWithObservations    = arrivedWithObservations;
            psc.ArrivalObservation         = string.IsNullOrWhiteSpace(arrivalObservation) ? null : arrivalObservation.Trim();
            psc.ProjectSubContractorStatusId = 6;
            psc.UpdatedDateTime            = DateTimeOffset.UtcNow;
            psc.UpdatedUserId              = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<ScNotificationDataDto> GetScNotificationDataAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();

            // ----- Tablas y columnas resueltas desde EF (refactor-safe) -----

            // ProjectSubContractor
            string tPsc       = ctx.Table<ProjectSubContractor>();
            string cPscId     = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorId));
            string cPscStatus = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectSubContractorStatusId));
            string cPscCont   = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ContractorId));
            string cPscProj   = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.ProjectId));
            string cPscWi     = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.WorkItemId));
            string cPscState  = ctx.Col<ProjectSubContractor>(nameof(ProjectSubContractor.State));

            // Project
            string tProject  = ctx.Table<ProjectModel>();
            string cProjId   = ctx.Col<ProjectModel>(nameof(ProjectModel.ProjectId));
            string cProjDesc = ctx.Col<ProjectModel>(nameof(ProjectModel.ProjectDescription));

            // WorkItem
            string tWorkItem = ctx.Table<WorkItem>();
            string cWiId     = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemId));
            string cWiDesc   = ctx.Col<WorkItem>(nameof(WorkItem.WorkItemDescription));

            // ContractorEmail
            string tCe       = ctx.Table<ContractorEmail>();
            string cCeContId = ctx.Col<ContractorEmail>(nameof(ContractorEmail.ContractorId));
            string cCeEmail  = ctx.Col<ContractorEmail>(nameof(ContractorEmail.Email));
            string cCeActive = ctx.Col<ContractorEmail>(nameof(ContractorEmail.Active));
            string cCeState  = ctx.Col<ContractorEmail>(nameof(ContractorEmail.State));

            // StaffProjectEmail
            string tSpe       = ctx.Table<StaffProjectEmail>();
            string cSpeProjId = ctx.Col<StaffProjectEmail>(nameof(StaffProjectEmail.ProjectId));
            string cSpeEmail  = ctx.Col<StaffProjectEmail>(nameof(StaffProjectEmail.Email));
            string cSpeTypeId = ctx.Col<StaffProjectEmail>(nameof(StaffProjectEmail.StaffProjectEmailTypeId));
            string cSpeState  = ctx.Col<StaffProjectEmail>(nameof(StaffProjectEmail.State));
            string cSpeActive = ctx.Col<StaffProjectEmail>(nameof(StaffProjectEmail.Active));

            // ----- SQL multi-statement (un solo round-trip) -----

            string sql = $@"
                SELECT psc.{cPscStatus} AS StatusId,
                       p.{cProjDesc}    AS ProjectDescription,
                       wi.{cWiDesc}     AS WorkItemDescription
                  FROM {tPsc} psc
                  JOIN {tProject}  p  ON p.{cProjId} = psc.{cPscProj}
                  JOIN {tWorkItem} wi ON wi.{cWiId}   = psc.{cPscWi}
                 WHERE psc.{cPscId} = @Id AND psc.{cPscState} = TRUE;

                SELECT ce.{cCeEmail}
                  FROM {tCe} ce
                  JOIN {tPsc} psc ON psc.{cPscCont} = ce.{cCeContId}
                 WHERE psc.{cPscId} = @Id AND psc.{cPscState} = TRUE
                   AND ce.{cCeActive} = TRUE AND ce.{cCeState} = TRUE;

                SELECT spe.{cSpeEmail}
                  FROM {tSpe} spe
                  JOIN {tPsc} psc ON psc.{cPscProj} = spe.{cSpeProjId}
                 WHERE psc.{cPscId} = @Id AND psc.{cPscState} = TRUE
                   AND spe.{cSpeTypeId} = 1 AND spe.{cSpeState} = TRUE AND spe.{cSpeActive} = TRUE;

                SELECT spe.{cSpeEmail}
                  FROM {tSpe} spe
                  JOIN {tPsc} psc ON psc.{cPscProj} = spe.{cSpeProjId}
                 WHERE psc.{cPscId} = @Id AND psc.{cPscState} = TRUE
                   AND spe.{cSpeTypeId} = 3 AND spe.{cSpeState} = TRUE AND spe.{cSpeActive} = TRUE;";

            // ----- Ejecutar y leer los 3 result sets -----

            var connection = ctx.Database.GetDbConnection();
            if (connection.State != ConnectionState.Open)
                await connection.OpenAsync();

            using var multi = await connection.QueryMultipleAsync(sql, new { Id = projectSubContractorId });

            var head = await multi.ReadSingleOrDefaultAsync<ScNotifHeadDto>()
                ?? throw new AbrilException("La adjudicación no existe.");

            if (head.StatusId != 4)
                throw new AbrilException("La adjudicación no está en estado 'Por enviar al SC'.");

            var contractorEmails     = (await multi.ReadAsync<string>()).ToList();
            var staffObraEmails      = (await multi.ReadAsync<string>()).ToList();
            var oficinaTecnicaEmails = (await multi.ReadAsync<string>()).ToList();

            return new ScNotificationDataDto
            {
                ProjectDescription   = head.ProjectDescription,
                WorkItemDescription  = head.WorkItemDescription,
                ContractorEmails     = contractorEmails,
                StaffObraEmails      = staffObraEmails,
                OficinaTecnicaEmails = oficinaTecnicaEmails
            };
        }

        private sealed class ScNotifHeadDto
        {
            public int    StatusId            { get; init; }
            public string ProjectDescription  { get; init; } = "";
            public string WorkItemDescription { get; init; } = "";
        }

        public async Task<Step6NotificationDataDto> GetStep6NotificationDataAsync(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 6)
                throw new AbrilException("La adjudicación no está en el paso de procesos de firma.");

            var projectDescription = await _context.Project
                .Where(p => p.ProjectId == psc.ProjectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync() ?? string.Empty;

            // Proyección solo del nombre — evita SELECT de columnas opcionales del Contributor
            // (sp_password_temp, es_abril, contributor_nombre_comercial) que pueden no existir
            // en todos los entornos.
            var contributorName = await (
                from ct in _context.Contractor
                join contrib in _context.Contributor on ct.ContributorId equals contrib.ContributorId
                where ct.ContractorId == psc.ContractorId
                select contrib.ContributorName
            ).FirstOrDefaultAsync();

            var workItem = await _context.WorkItem
                .FirstOrDefaultAsync(w => w.WorkItemId == psc.WorkItemId);

            var emails = await _context.StaffProjectEmail
                .Where(s => s.ProjectId == psc.ProjectId
                    && (s.StaffProjectEmailTypeId == 1 || s.StaffProjectEmailTypeId == 3)
                    && s.State && s.Active)
                .Select(s => new { s.Email, s.StaffProjectEmailTypeId })
                .ToListAsync();

            return new Step6NotificationDataDto
            {
                ProjectDescription  = projectDescription,
                ContributorName     = contributorName ?? string.Empty,
                WorkItemDescription = workItem?.WorkItemDescription  ?? string.Empty,
                ContractNumber      = psc.ContractNumber,
                StaffObraEmails      = emails.Where(e => e.StaffProjectEmailTypeId == 1).Select(e => e.Email).ToList(),
                OficinaTecnicaEmails = emails.Where(e => e.StaffProjectEmailTypeId == 3).Select(e => e.Email).ToList()
            };
        }

        public async Task UpdateStep6ChecksAsync(int projectSubContractorId, bool signedCostos, bool signedGerenteInmobiliario, bool signedGerenteGeneral, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 6)
                throw new AbrilException("La adjudicación no está en el paso de procesos de firma.");

            psc.Step6SignedCostos              = signedCostos;
            psc.Step6SignedGerenteInmobiliario = signedGerenteInmobiliario;
            psc.Step6SignedGerenteGeneral      = signedGerenteGeneral;
            psc.UpdatedDateTime                = DateTimeOffset.UtcNow;
            psc.UpdatedUserId                  = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<Step5EmailDataDto> GetStep5EmailDataAsync(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            var projectDescription = await _context.Project
                .Where(p => p.ProjectId == psc.ProjectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync() ?? string.Empty;

            // Proyección solo del nombre — evita SELECT de columnas opcionales del Contributor.
            var contributorName = await (
                from ct in _context.Contractor
                join contrib in _context.Contributor on ct.ContributorId equals contrib.ContributorId
                where ct.ContractorId == psc.ContractorId
                select contrib.ContributorName
            ).FirstOrDefaultAsync();

            var workItem = await _context.WorkItem
                .FirstOrDefaultAsync(w => w.WorkItemId == psc.WorkItemId);

            var emails = await _context.StaffProjectEmail
                .Where(s => s.ProjectId == psc.ProjectId
                    && (s.StaffProjectEmailTypeId == 2 || s.StaffProjectEmailTypeId == 3)
                    && s.State && s.Active)
                .Select(s => new { s.Email, s.StaffProjectEmailTypeId })
                .ToListAsync();

            return new Step5EmailDataDto
            {
                ProjectDescription   = projectDescription,
                ContributorName      = contributorName ?? string.Empty,
                WorkItemDescription  = workItem?.WorkItemDescription ?? string.Empty,
                ArrivalObservation   = psc.ArrivalObservation,
                OficinaCentralEmails = emails.Where(e => e.StaffProjectEmailTypeId == 2).Select(e => e.Email).ToList(),
                OficinaTecnicaEmails = emails.Where(e => e.StaffProjectEmailTypeId == 3).Select(e => e.Email).ToList(),
            };
        }

        public async Task SaveArrivalObservationAsync(int projectSubContractorId, string? arrivalObservation, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            psc.ArrivalObservation = string.IsNullOrWhiteSpace(arrivalObservation) ? null : arrivalObservation.Trim();
            psc.UpdatedDateTime    = DateTimeOffset.UtcNow;
            psc.UpdatedUserId      = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<Step8NotificationDataDto> GetStep8NotificationDataAsync(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 8)
                throw new AbrilException("La adjudicación no está en el paso de envío a obra.");

            var projectDescription = await _context.Project
                .Where(p => p.ProjectId == psc.ProjectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync() ?? string.Empty;

            // Proyección solo del nombre — evita SELECT de columnas opcionales del Contributor
            // (sp_password_temp, es_abril, contributor_nombre_comercial) que pueden no existir
            // en todos los entornos.
            var contributorName = await (
                from ct in _context.Contractor
                join contrib in _context.Contributor on ct.ContributorId equals contrib.ContributorId
                where ct.ContractorId == psc.ContractorId
                select contrib.ContributorName
            ).FirstOrDefaultAsync();

            // Paso 8: destinatario = Staff de obra (tipo 1); copia = Oficina Técnica (tipo 3).
            var emails = await _context.StaffProjectEmail
                .Where(s => s.ProjectId == psc.ProjectId
                    && (s.StaffProjectEmailTypeId == 1 || s.StaffProjectEmailTypeId == 3)
                    && s.State && s.Active)
                .Select(s => new { s.Email, s.StaffProjectEmailTypeId })
                .ToListAsync();

            var scannedDocs = await _context.ProjectSubContractorScannedDoc
                .Where(f => f.ProjectSubContractorId == projectSubContractorId && f.State)
                .OrderBy(f => f.Slot)
                .Select(f => new ProjectSubContractorFileDto { FileUrl = f.FileUrl!, OriginalFileName = f.OriginalFileName })
                .ToListAsync();

            return new Step8NotificationDataDto
            {
                ProjectDescription  = projectDescription,
                ContributorName     = contributorName ?? string.Empty,
                StaffObraEmails      = emails.Where(e => e.StaffProjectEmailTypeId == 1).Select(e => e.Email).ToList(),
                OficinaTecnicaEmails = emails.Where(e => e.StaffProjectEmailTypeId == 3).Select(e => e.Email).ToList(),
                ScannedDocs         = scannedDocs
            };
        }

        public async Task SaveDates(int projectSubContractorId, UpdateDatesDTO dto, int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State);

            if (psc is null)
                throw new AbrilException("La adjudicación no existe.");

            // Editable mientras la adjudicación esté en pasos 1–4. Del paso 5 en adelante queda bloqueada.
            if (psc.ProjectSubContractorStatusId >= 5)
                throw new AbrilException("Los datos del contrato ya no se pueden editar a partir del paso 5.");

            if (dto.StartDate != default && dto.EndDate != default && dto.StartDate > dto.EndDate)
                throw new AbrilException("La fecha de inicio no puede ser posterior a la fecha fin del contrato.");

            psc.SigningDate = dto.SigningDate;
            psc.StartDate = dto.StartDate;
            psc.EndDate = dto.EndDate;
            psc.ContractNumber          = dto.ContractNumber;
            psc.PromissoryNoteNumber    = dto.PromissoryNoteNumber;
            psc.GuaranteeFundPercentage = dto.GuaranteeFundPercentage;
            psc.GuaranteeFundDays       = dto.GuaranteeFundDays;
            psc.GuaranteeValidityDays   = dto.GuaranteeValidityDays;
            psc.PaymentDays             = dto.PaymentDays ?? 7;
            psc.TermDays = (dto.StartDate != default && dto.EndDate != default)
                ? (int)(dto.EndDate.ToDateTime(TimeOnly.MinValue) - dto.StartDate.ToDateTime(TimeOnly.MinValue)).TotalDays
                : null;
            // Solo avanza al paso 3 cuando viene desde el paso 2 (progresión normal).
            // Si se está EDITANDO en pasos 3 o 4, se conserva el estado actual (no retrocede ni salta).
            if (psc.ProjectSubContractorStatusId == 2)
                psc.ProjectSubContractorStatusId = 3;
            psc.UpdatedDateTime = DateTimeOffset.UtcNow;
            psc.UpdatedUserId = userId;

            await _context.SaveChangesAsync();
        }

        public async Task<AdjudicacionPathDataDto> GetPathDataAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var data = await (
                from psc in ctx.ProjectSubContractor
                join p      in ctx.Project      on psc.ProjectId    equals p.ProjectId
                join ct     in ctx.Contractor   on psc.ContractorId equals ct.ContractorId
                join contrib in ctx.Contributor on ct.ContributorId equals contrib.ContributorId
                join wi     in ctx.WorkItem     on psc.WorkItemId   equals wi.WorkItemId
                join wsj    in ctx.WorkSpecialty on psc.WorkSpecialtyId equals wsj.WorkSpecialtyId into wsg
                from ws in wsg.DefaultIfEmpty()
                join pafObras in ctx.ProjectAdjudicacionFolder.Where(f =>
                        f.Active && f.State && f.FolderTypeId == AdjudicacionFolderTypes.Obras)
                    on psc.ProjectId equals pafObras.ProjectId into pafObrasG
                from obrasFolder in pafObrasG.DefaultIfEmpty()
                join pafBackup in ctx.ProjectAdjudicacionFolder.Where(f =>
                        f.Active && f.State && f.FolderTypeId == AdjudicacionFolderTypes.BackupProyecto)
                    on psc.ProjectId equals pafBackup.ProjectId into pafBackupG
                from backupFolder in pafBackupG.DefaultIfEmpty()
                where psc.ProjectSubContractorId == projectSubContractorId && psc.State
                select new AdjudicacionPathDataDto
                {
                    ProjectSubContractorId   = psc.ProjectSubContractorId,
                    ProjectId                = psc.ProjectId,
                    ProjectDescription       = p.ProjectDescription,
                    Abbreviation             = p.Abbreviation,
                    ContributorRuc           = contrib.ContributorRuc,
                    ContributorName          = contrib.ContributorName,
                    WorkItemDescription      = wi.WorkItemDescription,
                    WorkSpecialtyDescription = ws != null ? ws.WorkSpecialtyDescription : null,
                    ObrasDriveId             = obrasFolder != null ? obrasFolder.DriveId : null,
                    ObrasFolderId            = obrasFolder != null ? obrasFolder.FolderId : null,
                    BackupDriveId            = backupFolder != null ? backupFolder.DriveId : null,
                    BackupFolderId           = backupFolder != null ? backupFolder.FolderId : null,
                    AdjudicacionFolderName   = psc.AdjudicacionFolderName,
                }
            ).FirstOrDefaultAsync();

            if (data is null)
                throw new AbrilException("La adjudicación no existe.");

            return data;
        }

        public async Task SetAdjudicacionFolderNameAsync(int projectSubContractorId, string folderName)
        {
            using var ctx = _factory.CreateDbContext();
            var psc = await ctx.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            psc.AdjudicacionFolderName = folderName;
            await ctx.SaveChangesAsync();
        }

        public async Task<string?> GetAdjudicacionFolderNameAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.ProjectSubContractor
                .Where(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                .Select(x => x.AdjudicacionFolderName)
                .FirstOrDefaultAsync();
        }

        public async Task SaveDocumentAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            string fileUrl,
            string originalFileName,
            int userId,
            string? sharepointItemId = null)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            var now = DateTimeOffset.UtcNow;

            switch (documentType)
            {
                case AdjudicacionDocumentType.Contract:
                    psc.ProjectSubContractorContractId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorContract,
                        psc.ProjectSubContractorContractId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorContract { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorContractId);
                    break;

                case AdjudicacionDocumentType.SummarySheet:
                    psc.ProjectSubContractorSummarySheetId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorSummarySheet,
                        psc.ProjectSubContractorSummarySheetId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorSummarySheet { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorSummarySheetId);
                    break;

                case AdjudicacionDocumentType.Budget:
                    psc.ProjectSubContractorBudgetId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorBudget,
                        psc.ProjectSubContractorBudgetId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorBudget { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorBudgetId);
                    break;

                case AdjudicacionDocumentType.Schedule:
                    psc.ProjectSubContractorScheduleId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorSchedule,
                        psc.ProjectSubContractorScheduleId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorSchedule { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorScheduleId);
                    break;

                case AdjudicacionDocumentType.AttachedQuotation:
                    psc.ProjectSubContractorAttachedQuotationId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorAttachedQuotation,
                        psc.ProjectSubContractorAttachedQuotationId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorAttachedQuotation { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorAttachedQuotationId);
                    break;

                case AdjudicacionDocumentType.ServiceOrder:
                    psc.ProjectSubContractorServiceOrderId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorServiceOrder,
                        psc.ProjectSubContractorServiceOrderId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorServiceOrder { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorServiceOrderId);
                    break;

                case AdjudicacionDocumentType.PromissoryNote:
                    psc.ProjectSubContractorPromissoryNoteId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorPromissoryNote,
                        psc.ProjectSubContractorPromissoryNoteId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorPromissoryNote { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorPromissoryNoteId);
                    break;

                case AdjudicacionDocumentType.ContractPackage:
                    psc.ProjectSubContractorPackageId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorPackage,
                        psc.ProjectSubContractorPackageId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorPackage { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorPackageId);
                    break;

                case AdjudicacionDocumentType.Instructivo:
                    psc.ProjectSubContractorInstructivoId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorInstructivo,
                        psc.ProjectSubContractorInstructivoId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorInstructivo { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorInstructivoId);
                    break;

                case AdjudicacionDocumentType.NonConformingOutput:
                    psc.ProjectSubContractorNonConformingOutputId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorNonConformingOutput,
                        psc.ProjectSubContractorNonConformingOutputId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorNonConformingOutput { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorNonConformingOutputId);
                    break;

                case AdjudicacionDocumentType.ToleranceChart:
                    psc.ProjectSubContractorToleranceChartId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorToleranceChart,
                        psc.ProjectSubContractorToleranceChartId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorToleranceChart { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorToleranceChartId);
                    break;

                case AdjudicacionDocumentType.FichaTecnica:
                    psc.ProjectSubContractorFichaTecnicaId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorFichaTecnica,
                        psc.ProjectSubContractorFichaTecnicaId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorFichaTecnica { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorFichaTecnicaId);
                    break;

                case AdjudicacionDocumentType.Anexo:
                    psc.ProjectSubContractorAnexoId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorAnexo,
                        psc.ProjectSubContractorAnexoId,
                        e => { e.FileUrl = fileUrl; e.OriginalFileName = originalFileName; e.SharepointItemId = sharepointItemId; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorAnexo { FileUrl = fileUrl, OriginalFileName = originalFileName, SharepointItemId = sharepointItemId, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorAnexoId);
                    break;

                case AdjudicacionDocumentType.ScannedDoc1:
                case AdjudicacionDocumentType.ScannedDoc2:
                case AdjudicacionDocumentType.ScannedDoc3:
                    int scannedSlotSave = documentType switch {
                        AdjudicacionDocumentType.ScannedDoc1 => 1,
                        AdjudicacionDocumentType.ScannedDoc2 => 2,
                        AdjudicacionDocumentType.ScannedDoc3 => 3,
                        _ => throw new AbrilException("Slot inválido.")
                    };
                    var existingScanned = await _context.ProjectSubContractorScannedDoc
                        .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.Slot == scannedSlotSave && x.State);
                    if (existingScanned != null)
                    {
                        existingScanned.FileUrl = fileUrl;
                        existingScanned.OriginalFileName = originalFileName;
                        existingScanned.SharepointItemId = sharepointItemId;
                        existingScanned.UpdatedDatetime = now;
                        existingScanned.UpdatedUserId = userId;
                    }
                    else
                    {
                        _context.ProjectSubContractorScannedDoc.Add(new ProjectSubContractorScannedDoc {
                            ProjectSubContractorId = projectSubContractorId,
                            Slot = scannedSlotSave,
                            FileUrl = fileUrl,
                            OriginalFileName = originalFileName,
                            SharepointItemId = sharepointItemId,
                            CreatedDatetime = now,
                            CreatedUserId = userId,
                            Active = true,
                            State = true
                        });
                    }
                    break;

                default:
                    throw new AbrilException("Tipo de documento no válido.");
            }

            psc.UpdatedDateTime = DateTimeOffset.UtcNow;
            psc.UpdatedUserId = userId;
            await _context.SaveChangesAsync();
        }

        public async Task<AdjudicacionSummarySheetDataDto> GetSummarySheetDataAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var data = await (
                from psc      in ctx.ProjectSubContractor
                join p        in ctx.Project        on psc.ProjectId        equals p.ProjectId
                join ct       in ctx.Contractor     on psc.ContractorId     equals ct.ContractorId
                join contrib  in ctx.Contributor    on ct.ContributorId     equals contrib.ContributorId
                join wi       in ctx.WorkItem       on psc.WorkItemId       equals wi.WorkItemId
                join ctype    in ctx.ContractType   on psc.ContractTypeId   equals ctype.ContractTypeId
                join pm       in ctx.PaymentMethod  on psc.PaymentMethodId  equals pm.PaymentMethodId
                join cur      in ctx.Currency       on psc.CurrencyId       equals cur.CurrencyId
                // modalidad de contrato (opcional)
                join cmJoin   in ctx.ContractModality on psc.ContractModalityId equals cmJoin.ContractModalityId into cmGroup
                from cm in cmGroup.DefaultIfEmpty()
                // forma de pago (opcional)
                join pfJoin   in ctx.PaymentForm on psc.PaymentFormId equals pfJoin.PaymentFormId into pfGroup
                from pf in pfGroup.DefaultIfEmpty()
                // representante legal del contratista (opcional)
                join personJoin in ctx.Person on contrib.LegalRepresentativePersonId equals personJoin.PersonId into personGroup
                from legalRep in personGroup.DefaultIfEmpty()
                // contributor del proyecto (razón social de Abril — opcional)
                join projContribJoin in ctx.Contributor on p.ContributorId equals projContribJoin.ContributorId into projContribGroup
                from projContrib in projContribGroup.DefaultIfEmpty()
                where psc.ProjectSubContractorId == projectSubContractorId && psc.State
                select new AdjudicacionSummarySheetDataDto
                {
                    ProjectSubContractorId    = psc.ProjectSubContractorId,
                    ProjectId                 = psc.ProjectId,
                    ProjectDescription        = p.ProjectDescription,
                    Abbreviation              = p.Abbreviation,
                    ProjectDistrict           = p.ProjectDistrict,
                    ProjectProvince           = p.ProjectProvince,
                    ProjectDepartment         = p.ProjectDepartment,
                    ProjectLocation           = p.ProjectLocation,
                    Niveles                   = p.LevelDescription ?? p.NumNiveles,
                    LevelDescription          = p.LevelDescription,
                    ProjectRazonSocial                  = projContrib != null ? projContrib.ContributorName : null,
                    ProjectContributorRuc               = projContrib != null ? projContrib.ContributorRuc : null,
                    ProjectLegalEntityRegistryNumber    = projContrib != null ? projContrib.LegalEntityRegistryNumber : null,
                    ContributorName           = contrib.ContributorName,
                    ContributorRuc            = contrib.ContributorRuc,
                    ContributorAddress        = contrib.ContributorAddress,
                    ContributorDistrict       = contrib.ContributorDistrict,
                    ContributorProvince       = contrib.ContributorProvince,
                    ContributorDepartment     = contrib.ContributorDepartment,
                    LegalRepresentativeFullName = legalRep != null ? legalRep.FullName : null,
                    LegalRepresentativeDni    = legalRep != null ? legalRep.DocumentIdentityCode : null,
                    LegalEntityRegistryNumber = contrib.LegalEntityRegistryNumber,
                    WorkItemId                = psc.WorkItemId,
                    WorkItemDescription       = wi.WorkItemDescription,
                    ContractWorkItemName      = psc.ContractWorkItemName,
                    ContractTypeDescription   = ctype.ContractTypeDescription,
                    ContractModalityId        = psc.ContractModalityId,
                    PaymentMethodId           = psc.PaymentMethodId,
                    PaymentMethodDescription  = pm.PaymentMethodDescription,
                    PaymentFormDescription    = pf != null ? pf.PaymentFormDescription : null,
                    IncludesCartaFianza       = psc.IncludesCartaFianza,
                    CurrencyCode              = cur.CurrencyCode,
                    Amount                    = psc.Amount,
                    HasIgv                    = psc.HasIgv,
                    AdvancePercentage         = psc.AdvancePercentage,
                    AdvanceAmount             = psc.AdvanceAmount,
                    TermDays                  = psc.TermDays,
                    SigningDate               = psc.SigningDate,
                    StartDate                 = psc.StartDate,
                    EndDate                   = psc.EndDate,
                    ContractNumber            = psc.ContractNumber,
                    PromissoryNoteNumber      = psc.PromissoryNoteNumber,
                    GuaranteeFundPercentage   = psc.GuaranteeFundPercentage,
                    GuaranteeFundDays         = psc.GuaranteeFundDays,
                    GuaranteeValidityDays     = psc.GuaranteeValidityDays,
                    PaymentDays               = psc.PaymentDays,
                }
            ).FirstOrDefaultAsync();

            if (data is null)
                throw new AbrilException("La adjudicación no existe.");

            // Cargar cláusulas especiales de la partida de control asociada
            var wicId = await ctx.ProjectSubContractor
                .Where(p => p.ProjectSubContractorId == projectSubContractorId && p.State)
                .Select(p => p.WorkItemCategoryId)
                .FirstOrDefaultAsync();

            if (wicId > 0)
            {
                // Cláusulas 9.x/7.x de la modalidad de contrato de la adjudicación
                // (1 = Suministro e Instalación, 2 = Suministro, 3 = Instalación). Sin modalidad → todas.
                var clauseQuery = ctx.WorkItemCategoryClause
                    .Where(c => c.WorkItemCategoryId == wicId && c.State);

                if (data.ContractModalityId.HasValue)
                    clauseQuery = clauseQuery.Where(c => c.ContractModalityId == data.ContractModalityId.Value);

                data.SpecialClauses = await clauseQuery
                    .OrderBy(c => c.SortOrder)
                    .Select(c => c.ClauseText)
                    .ToListAsync();

                data.SpecialClausesAnexo3 = await ctx.WorkItemCategoryAnexo3Clause
                    .Where(c => c.WorkItemCategoryId == wicId && c.State)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => c.ClauseText)
                    .ToListAsync();

                data.SpecialClausesAnexo4 = await ctx.WorkItemCategoryAnexo4Clause
                    .Where(c => c.WorkItemCategoryId == wicId && c.State)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => c.ClauseText)
                    .ToListAsync();
            }

            // Formas de valorización (cláusula 5.1) asociadas a la PARTIDA (no a la categoría).
            var forms = await ctx.WorkItemValorizationForm
                .Where(f => f.WorkItemId == data.WorkItemId && f.State)
                .OrderBy(f => f.SortOrder)
                .Select(f => new { f.Percentage, f.Concept })
                .ToListAsync();
            data.ValorizationForms = forms.Select(f => (f.Percentage, f.Concept)).ToList();

            return data;
        }

        public async Task UpdateDocumentStatusAsync(
            int projectSubContractorId,
            AdjudicacionDocumentType documentType,
            int? statusId,
            string? observation,
            int userId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            var now = DateTimeOffset.UtcNow;

            // El estado (y su observación) se puede fijar ANTES de subir el archivo: si el
            // documento todavía no tiene registro se crea uno sin file_url para guardarlo,
            // de modo que el estado no se pierda al cerrar y reabrir el detalle.
            switch (documentType)
            {
                case AdjudicacionDocumentType.Contract:
                    psc.ProjectSubContractorContractId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorContract,
                        psc.ProjectSubContractorContractId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorContract { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorContractId);
                    break;

                case AdjudicacionDocumentType.SummarySheet:
                    psc.ProjectSubContractorSummarySheetId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorSummarySheet,
                        psc.ProjectSubContractorSummarySheetId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorSummarySheet { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorSummarySheetId);
                    break;

                case AdjudicacionDocumentType.Budget:
                    psc.ProjectSubContractorBudgetId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorBudget,
                        psc.ProjectSubContractorBudgetId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorBudget { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorBudgetId);
                    break;

                case AdjudicacionDocumentType.Schedule:
                    psc.ProjectSubContractorScheduleId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorSchedule,
                        psc.ProjectSubContractorScheduleId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorSchedule { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorScheduleId);
                    break;

                case AdjudicacionDocumentType.AttachedQuotation:
                    psc.ProjectSubContractorAttachedQuotationId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorAttachedQuotation,
                        psc.ProjectSubContractorAttachedQuotationId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorAttachedQuotation { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorAttachedQuotationId);
                    break;

                case AdjudicacionDocumentType.ServiceOrder:
                    psc.ProjectSubContractorServiceOrderId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorServiceOrder,
                        psc.ProjectSubContractorServiceOrderId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorServiceOrder { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorServiceOrderId);
                    break;

                case AdjudicacionDocumentType.PromissoryNote:
                    psc.ProjectSubContractorPromissoryNoteId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorPromissoryNote,
                        psc.ProjectSubContractorPromissoryNoteId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorPromissoryNote { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorPromissoryNoteId);
                    break;

                case AdjudicacionDocumentType.Instructivo:
                    psc.ProjectSubContractorInstructivoId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorInstructivo,
                        psc.ProjectSubContractorInstructivoId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorInstructivo { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorInstructivoId);
                    break;

                // Causales de No Conformidad y Cuadro de Tolerancias: documentos de plantilla, sin
                // archivo. El estado se guarda directamente en una columna de la adjudicación
                // (igual que Protección de Acabados).
                case AdjudicacionDocumentType.NonConformingOutput:
                    psc.NonConformingOutputStatusId = statusId;
                    break;

                case AdjudicacionDocumentType.ToleranceChart:
                    psc.ToleranceChartStatusId = statusId;
                    break;

                case AdjudicacionDocumentType.FichaTecnica:
                    psc.ProjectSubContractorFichaTecnicaId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorFichaTecnica,
                        psc.ProjectSubContractorFichaTecnicaId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorFichaTecnica { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorFichaTecnicaId);
                    break;

                case AdjudicacionDocumentType.Anexo:
                    psc.ProjectSubContractorAnexoId = await UpsertDocumentAsync(
                        _context.ProjectSubContractorAnexo,
                        psc.ProjectSubContractorAnexoId,
                        e => { e.ProjectSubContractorFileStatusId = statusId; e.Observation = observation; e.UpdatedDatetime = now; e.UpdatedUserId = userId; },
                        () => new ProjectSubContractorAnexo { ProjectSubContractorFileStatusId = statusId, Observation = observation, CreatedDatetime = now, CreatedUserId = userId, Active = true, State = true },
                        e => e.ProjectSubContractorAnexoId);
                    break;

                // Protección de Acabados: documento de plantilla, sin archivo. El estado se guarda
                // directamente en una columna de la adjudicación.
                case AdjudicacionDocumentType.FinishProtection:
                    psc.FinishProtectionStatusId = statusId;
                    break;

                default:
                    throw new AbrilException("Tipo de documento no válido.");
            }

            psc.UpdatedDateTime = DateTimeOffset.UtcNow;
            psc.UpdatedUserId = userId;
            await _context.SaveChangesAsync();
        }

        public async Task<Step3ApprovalDataDto> GetStep3ApprovalDataAsync(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 3)
                throw new AbrilException("La adjudicación no está en el paso de preparación de documentos.");

            var projectDescription = await _context.Project
                .Where(p => p.ProjectId == psc.ProjectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync() ?? string.Empty;

            var workItemDescription = await _context.WorkItem
                .Where(w => w.WorkItemId == psc.WorkItemId)
                .Select(w => w.WorkItemDescription)
                .FirstOrDefaultAsync() ?? string.Empty;

            // Proyectamos solo el nombre del contributor (evita SELECT de columnas opcionales
            // como sp_password_temp, es_abril, contributor_nombre_comercial que pueden no existir
            // en todos los entornos).
            var contributorName = await (
                from ct in _context.Contractor
                join contrib in _context.Contributor on ct.ContributorId equals contrib.ContributorId
                where ct.ContractorId == psc.ContractorId
                select contrib.ContributorName
            ).FirstOrDefaultAsync() ?? string.Empty;

            var emails = await _context.StaffProjectEmail
                .Where(s => s.ProjectId == psc.ProjectId
                    && (s.StaffProjectEmailTypeId == 1 || s.StaffProjectEmailTypeId == 3)
                    && s.State && s.Active)
                .Select(s => new { s.Email, s.StaffProjectEmailTypeId })
                .ToListAsync();

            return new Step3ApprovalDataDto
            {
                ProjectDescription  = projectDescription,
                ContributorName     = contributorName,
                WorkItemDescription = workItemDescription,
                StaffObraEmails      = emails.Where(e => e.StaffProjectEmailTypeId == 1).Select(e => e.Email).ToList(),
                OficinaTecnicaEmails = emails.Where(e => e.StaffProjectEmailTypeId == 3).Select(e => e.Email).ToList(),
            };
        }

        public async Task<List<DocumentObservationDto>> GetStep3DocumentObservationsAsync(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            if (psc.ProjectSubContractorStatusId != 3)
                throw new AbrilException("La adjudicación no está en el paso de preparación de documentos.");

            const int ObservacionStatusId = 3;
            var result = new List<DocumentObservationDto>();

            // Query each document FK, add to result when status == 3 (Con observaciones)
            if (psc.ProjectSubContractorContractId.HasValue)
            {
                var d = await _context.ProjectSubContractorContract
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorContractId == psc.ProjectSubContractorContractId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Contrato", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorSummarySheetId.HasValue)
            {
                var d = await _context.ProjectSubContractorSummarySheet
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorSummarySheetId == psc.ProjectSubContractorSummarySheetId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Hoja Resumen", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorBudgetId.HasValue)
            {
                var d = await _context.ProjectSubContractorBudget
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorBudgetId == psc.ProjectSubContractorBudgetId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Presupuesto", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorScheduleId.HasValue)
            {
                var d = await _context.ProjectSubContractorSchedule
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorScheduleId == psc.ProjectSubContractorScheduleId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Cronograma", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorAttachedQuotationId.HasValue)
            {
                var d = await _context.ProjectSubContractorAttachedQuotation
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorAttachedQuotationId == psc.ProjectSubContractorAttachedQuotationId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Cotización Adjunta", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorServiceOrderId.HasValue)
            {
                var d = await _context.ProjectSubContractorServiceOrder
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorServiceOrderId == psc.ProjectSubContractorServiceOrderId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Orden de Servicio", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorPromissoryNoteId.HasValue)
            {
                var d = await _context.ProjectSubContractorPromissoryNote
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorPromissoryNoteId == psc.ProjectSubContractorPromissoryNoteId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Pagaré", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorInstructivoId.HasValue)
            {
                var d = await _context.ProjectSubContractorInstructivo
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorInstructivoId == psc.ProjectSubContractorInstructivoId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Instructivo", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorNonConformingOutputId.HasValue)
            {
                var d = await _context.ProjectSubContractorNonConformingOutput
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorNonConformingOutputId == psc.ProjectSubContractorNonConformingOutputId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Salidas No Conforme", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorToleranceChartId.HasValue)
            {
                var d = await _context.ProjectSubContractorToleranceChart
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorToleranceChartId == psc.ProjectSubContractorToleranceChartId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Cuadro de Tolerancias", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorFichaTecnicaId.HasValue)
            {
                var d = await _context.ProjectSubContractorFichaTecnica
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorFichaTecnicaId == psc.ProjectSubContractorFichaTecnicaId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Ficha Técnica", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorAnexoId.HasValue)
            {
                var d = await _context.ProjectSubContractorAnexo
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorAnexoId == psc.ProjectSubContractorAnexoId.Value);
                if (d?.ProjectSubContractorFileStatusId == ObservacionStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Anexos", Observation = d.Observation });
            }

            return result;
        }

        public async Task<List<DocumentObservationDto>> GetLevantamientoDocumentsAsync(int projectSubContractorId)
        {
            var psc = await _context.ProjectSubContractor
                .FirstOrDefaultAsync(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                ?? throw new AbrilException("La adjudicación no existe.");

            const int LevantamientoStatusId = 5;
            var result = new List<DocumentObservationDto>();

            if (psc.ProjectSubContractorContractId.HasValue)
            {
                var d = await _context.ProjectSubContractorContract
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorContractId == psc.ProjectSubContractorContractId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Contrato", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorSummarySheetId.HasValue)
            {
                var d = await _context.ProjectSubContractorSummarySheet
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorSummarySheetId == psc.ProjectSubContractorSummarySheetId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Hoja Resumen", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorScheduleId.HasValue)
            {
                var d = await _context.ProjectSubContractorSchedule
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorScheduleId == psc.ProjectSubContractorScheduleId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Cronograma", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorAttachedQuotationId.HasValue)
            {
                var d = await _context.ProjectSubContractorAttachedQuotation
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorAttachedQuotationId == psc.ProjectSubContractorAttachedQuotationId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Cotización Adjunta", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorServiceOrderId.HasValue)
            {
                var d = await _context.ProjectSubContractorServiceOrder
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorServiceOrderId == psc.ProjectSubContractorServiceOrderId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Orden de Servicio", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorPromissoryNoteId.HasValue)
            {
                var d = await _context.ProjectSubContractorPromissoryNote
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorPromissoryNoteId == psc.ProjectSubContractorPromissoryNoteId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Pagaré", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorInstructivoId.HasValue)
            {
                var d = await _context.ProjectSubContractorInstructivo
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorInstructivoId == psc.ProjectSubContractorInstructivoId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Instructivo", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorNonConformingOutputId.HasValue)
            {
                var d = await _context.ProjectSubContractorNonConformingOutput
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorNonConformingOutputId == psc.ProjectSubContractorNonConformingOutputId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Salidas No Conforme", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorToleranceChartId.HasValue)
            {
                var d = await _context.ProjectSubContractorToleranceChart
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorToleranceChartId == psc.ProjectSubContractorToleranceChartId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Cuadro de Tolerancias", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorFichaTecnicaId.HasValue)
            {
                var d = await _context.ProjectSubContractorFichaTecnica
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorFichaTecnicaId == psc.ProjectSubContractorFichaTecnicaId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Ficha Técnica", Observation = d.Observation });
            }

            if (psc.ProjectSubContractorAnexoId.HasValue)
            {
                var d = await _context.ProjectSubContractorAnexo
                    .FirstOrDefaultAsync(x => x.ProjectSubContractorAnexoId == psc.ProjectSubContractorAnexoId.Value);
                if (d?.ProjectSubContractorFileStatusId == LevantamientoStatusId)
                    result.Add(new DocumentObservationDto { DocumentLabel = "Anexos", Observation = d.Observation });
            }

            return result;
        }

        public async Task<ContractPackageUrlsDto> GetContractPackageUrlsAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var ids = await ctx.ProjectSubContractor
                .Where(x => x.ProjectSubContractorId == projectSubContractorId && x.State)
                .Select(x => new
                {
                    x.ProjectSubContractorSummarySheetId,
                    x.ProjectSubContractorContractId,
                    x.ProjectSubContractorAttachedQuotationId,
                    x.ProjectSubContractorFichaTecnicaId,
                    x.ProjectSubContractorServiceOrderId,
                    x.ProjectSubContractorScheduleId,
                    x.NonConformingOutputStatusId,
                    x.ToleranceChartStatusId,
                    x.FinishProtectionStatusId,
                    x.ProjectSubContractorInstructivoId,
                    x.ProjectSubContractorPromissoryNoteId,
                    x.ProjectSubContractorAnexoId,
                    x.ContractNumber,
                })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("La adjudicación no existe.");

            var summarySheet = ids.ProjectSubContractorSummarySheetId.HasValue
                ? await ctx.ProjectSubContractorSummarySheet
                    .Where(x => x.ProjectSubContractorSummarySheetId == ids.ProjectSubContractorSummarySheetId.Value)
                    .Select(x => new { x.FileUrl, x.SharepointItemId })
                    .FirstOrDefaultAsync()
                : null;

            var contract = ids.ProjectSubContractorContractId.HasValue
                ? await ctx.ProjectSubContractorContract
                    .Where(x => x.ProjectSubContractorContractId == ids.ProjectSubContractorContractId.Value)
                    .Select(x => new { x.FileUrl, x.SharepointItemId })
                    .FirstOrDefaultAsync()
                : null;

            // Documentos del paso 3 que se incrustan DENTRO del contrato (no van como archivos sueltos):
            //   - Cotización adjunta → marcador <<INSERTAR_COTIZACION_AQUI>>
            //   - Ficha técnica      → marcador <<INSERTAR_FICHA_TÉCNICA_AQUI>>
            //   - Orden de servicio  → marcador <<INSERTAR_ORDEN_DE_SERVICIO_AQUI>>
            //   - Cronograma         → marcador <<INSERTAR_CRONOGRAMA_AQUI>>
            // Cada uno trae OriginalFileName para que el service decida si descarga directo (PDF) o vía ?format=pdf.
            var attachedQuotation = ids.ProjectSubContractorAttachedQuotationId.HasValue
                ? await ctx.ProjectSubContractorAttachedQuotation
                    .Where(x => x.ProjectSubContractorAttachedQuotationId == ids.ProjectSubContractorAttachedQuotationId.Value)
                    .Select(x => new { x.FileUrl, x.SharepointItemId, x.OriginalFileName })
                    .FirstOrDefaultAsync()
                : null;

            var fichaTecnica = ids.ProjectSubContractorFichaTecnicaId.HasValue
                ? await ctx.ProjectSubContractorFichaTecnica
                    .Where(x => x.ProjectSubContractorFichaTecnicaId == ids.ProjectSubContractorFichaTecnicaId.Value)
                    .Select(x => new { x.FileUrl, x.SharepointItemId, x.OriginalFileName })
                    .FirstOrDefaultAsync()
                : null;

            var serviceOrder = ids.ProjectSubContractorServiceOrderId.HasValue
                ? await ctx.ProjectSubContractorServiceOrder
                    .Where(x => x.ProjectSubContractorServiceOrderId == ids.ProjectSubContractorServiceOrderId.Value)
                    .Select(x => new { x.FileUrl, x.SharepointItemId, x.OriginalFileName })
                    .FirstOrDefaultAsync()
                : null;

            var schedule = ids.ProjectSubContractorScheduleId.HasValue
                ? await ctx.ProjectSubContractorSchedule
                    .Where(x => x.ProjectSubContractorScheduleId == ids.ProjectSubContractorScheduleId.Value)
                    .Select(x => new { x.FileUrl, x.SharepointItemId, x.OriginalFileName })
                    .FirstOrDefaultAsync()
                : null;

            // Causales de No Conformidad, Cuadro de Tolerancias y Protección de Acabados: documentos
            // de plantilla fija. El estado se guarda en columnas de la adjudicación; el PDF de
            // plantilla se incluye solo cuando el estado es "Sí aplica" (statusId = 4).
            const int AprobadoStatusId = 4;
            var nonConformingApproved   = ids.NonConformingOutputStatusId == AprobadoStatusId;
            var toleranceChartApproved  = ids.ToleranceChartStatusId      == AprobadoStatusId;
            var finishProtectionApproved = ids.FinishProtectionStatusId   == AprobadoStatusId;

            var instructivo = ids.ProjectSubContractorInstructivoId.HasValue
                ? await ctx.ProjectSubContractorInstructivo
                    .Where(x => x.ProjectSubContractorInstructivoId == ids.ProjectSubContractorInstructivoId.Value
                             && x.ProjectSubContractorFileStatusId != 1)
                    .Select(x => new { x.FileUrl, x.SharepointItemId })
                    .FirstOrDefaultAsync()
                : null;

            var promissoryNote = ids.ProjectSubContractorPromissoryNoteId.HasValue
                ? await ctx.ProjectSubContractorPromissoryNote
                    .Where(x => x.ProjectSubContractorPromissoryNoteId == ids.ProjectSubContractorPromissoryNoteId.Value
                             && x.ProjectSubContractorFileStatusId != 1)
                    .Select(x => new { x.FileUrl, x.SharepointItemId })
                    .FirstOrDefaultAsync()
                : null;

            // Anexo del paso 3: en la práctica es la Vigencia de Poder. Va al final del paquete.
            var anexo = ids.ProjectSubContractorAnexoId.HasValue
                ? await ctx.ProjectSubContractorAnexo
                    .Where(x => x.ProjectSubContractorAnexoId == ids.ProjectSubContractorAnexoId.Value
                             && x.ProjectSubContractorFileStatusId != 1)
                    .Select(x => new { x.FileUrl, x.SharepointItemId, x.OriginalFileName })
                    .FirstOrDefaultAsync()
                : null;

            // Si los documentos principales no tienen archivo (se marcaron como "No aplica" en paso 3)
            // simplemente no se incluyen en el paquete; la validación fuerte solo aplica
            // cuando existe un archivo pero le falta el ItemId de SharePoint (registro legacy).
            return new ContractPackageUrlsDto
            {
                SummarySheetUrl             = summarySheet?.FileUrl,
                SummarySheetItemId          = summarySheet?.SharepointItemId,
                ContractUrl                 = contract?.FileUrl,
                ContractItemId              = contract?.SharepointItemId,
                AttachedQuotationUrl        = attachedQuotation?.FileUrl,
                AttachedQuotationItemId     = attachedQuotation?.SharepointItemId,
                AttachedQuotationFileName   = attachedQuotation?.OriginalFileName,
                FichaTecnicaUrl             = fichaTecnica?.FileUrl,
                FichaTecnicaItemId          = fichaTecnica?.SharepointItemId,
                FichaTecnicaFileName        = fichaTecnica?.OriginalFileName,
                ServiceOrderUrl             = serviceOrder?.FileUrl,
                ServiceOrderItemId          = serviceOrder?.SharepointItemId,
                ServiceOrderFileName        = serviceOrder?.OriginalFileName,
                ScheduleUrl                 = schedule?.FileUrl,
                ScheduleItemId              = schedule?.SharepointItemId,
                ScheduleFileName            = schedule?.OriginalFileName,
                NonConformingOutputApproved = nonConformingApproved,
                ToleranceChartApproved      = toleranceChartApproved,
                FinishProtectionApproved    = finishProtectionApproved,
                InstructivoUrl              = instructivo?.FileUrl,
                InstructivoItemId           = instructivo?.SharepointItemId,
                PromissoryNoteUrl           = promissoryNote?.FileUrl,
                PromissoryNoteItemId        = promissoryNote?.SharepointItemId,
                AnexoUrl                    = anexo?.FileUrl,
                AnexoItemId                 = anexo?.SharepointItemId,
                AnexoFileName               = anexo?.OriginalFileName,
                ContractNumber              = ids.ContractNumber,
            };
        }

        public async Task<(string FileUrl, string OriginalFileName)?> GetPackageFileInfoAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var result = await (
                from psc in ctx.ProjectSubContractor
                join pkg in ctx.ProjectSubContractorPackage
                    on psc.ProjectSubContractorPackageId equals pkg.ProjectSubContractorPackageId
                where psc.ProjectSubContractorId == projectSubContractorId && psc.State
                select new { pkg.FileUrl, pkg.OriginalFileName }
            ).FirstOrDefaultAsync();

            return result != null
                ? (result.FileUrl, result.OriginalFileName)
                : null;
        }

        public async Task<(string? FolderId, string? FolderName, int? SyncStatus)?> GetInstructivosFolderAsync(int projectSubContractorId)
        {
            using var ctx = _factory.CreateDbContext();

            var result = await (
                from psc in ctx.ProjectSubContractor
                join wic in ctx.WorkItemCategory
                    on psc.WorkItemCategoryId equals wic.WorkItemCategoryId
                where psc.ProjectSubContractorId == projectSubContractorId && psc.State
                select new { wic.InstructivosFolderId, wic.InstructivosFolderName, wic.InstructivosSyncStatus }
            ).FirstOrDefaultAsync();

            if (result is null) return null;
            return (result.InstructivosFolderId, result.InstructivosFolderName, result.InstructivosSyncStatus);
        }

        /// <summary>
        /// Actualiza el registro existente si ya hay un ID, o crea uno nuevo y devuelve su ID.
        /// </summary>
        /// <summary>
        /// Mapea las columnas de un documento del paso 3 al DTO. Devuelve el DTO cuando el
        /// registro existe aunque todavía NO tenga archivo (el estado/observación se puede
        /// fijar antes de subirlo); solo devuelve null cuando no hay nada que mostrar.
        /// </summary>
        private static ProjectSubContractorFileDto? MapDocDto(
            string? fileUrl,
            string? originalFileName,
            int? statusId,
            string? statusDescription,
            string? observation)
        {
            if (fileUrl is null && statusId is null && observation is null) return null;

            return new ProjectSubContractorFileDto
            {
                FileUrl           = fileUrl!,
                OriginalFileName  = originalFileName,
                StatusId          = statusId,
                StatusDescription = statusDescription,
                Observation       = observation,
            };
        }

        private async Task<int> UpsertDocumentAsync<T>(
            Microsoft.EntityFrameworkCore.DbSet<T> dbSet,
            int? existingId,
            Action<T> update,
            Func<T> create,
            Func<T, int> getId) where T : class
        {
            if (existingId.HasValue)
            {
                var existing = await dbSet.FindAsync(existingId.Value)
                    ?? throw new AbrilException("El documento referenciado no existe.");
                update(existing);
                await _context.SaveChangesAsync();
                return existingId.Value;
            }
            else
            {
                var newDoc = create();
                dbSet.Add(newDoc);
                await _context.SaveChangesAsync();
                return getId(newDoc);
            }
        }
    }
}
