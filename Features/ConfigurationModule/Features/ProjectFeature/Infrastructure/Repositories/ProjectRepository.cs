using Microsoft.EntityFrameworkCore;
using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Shared.Models;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Interfaces;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Infrastructure.Repositories
{
    public class ProjectRepository : IProjectRepository
    {
        private readonly AppDbContext _context;
        private readonly IChecklistRepository _checklist;

        public ProjectRepository(AppDbContext context, IChecklistRepository checklist)
        {
            _context  = context;
            _checklist = checklist;
        }

        public async Task<PagedResult<ProjectDto>> GetPaged(
            int page, int pageSize, string? ruc = null, string? razonSocial = null, string? projectDescription = null, bool? active = null)
        {
            var query = _context.Project.Where(p => p.State);

            if (active.HasValue)
                query = query.Where(p => p.Active == active.Value);

            if (!string.IsNullOrWhiteSpace(ruc))
                query = query.Where(p => p.Contributor != null && p.Contributor.ContributorRuc.Contains(ruc));

            // Búsqueda por palabras en cualquier orden (compatible con el matcher de app-search-input):
            // cada palabra debe estar contenida en el texto, sin distinguir mayúsculas.
            foreach (var palabra in SplitBusqueda(razonSocial))
            {
                var token = palabra;
                query = query.Where(p => p.Contributor != null && p.Contributor.ContributorName.ToLower().Contains(token));
            }

            foreach (var palabra in SplitBusqueda(projectDescription))
            {
                var token = palabra;
                query = query.Where(p => p.ProjectDescription.ToLower().Contains(token));
            }

            query = query.OrderByDescending(p => p.ProjectId);

            var totalRecords = await query.CountAsync();

            var data = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new ProjectDto
                {
                    ProjectId          = p.ProjectId,
                    ProjectDescription = p.ProjectDescription,
                    Codigo             = p.Codigo,
                    Abbreviation       = p.Abbreviation,
                    LevelDescription   = p.LevelDescription,
                    Estado             = p.Estado,
                    CicloVida          = p.Activo,

                    ContributorId                        = p.ContributorId,
                    ContributorRuc                       = p.Contributor != null ? p.Contributor.ContributorRuc           : null,
                    ContributorName                      = p.Contributor != null ? p.Contributor.ContributorName          : null,
                    ContributorAddress                   = p.Contributor != null ? p.Contributor.ContributorAddress       : null,
                    ContributorDistrict                  = p.Contributor != null ? p.Contributor.ContributorDistrict      : null,
                    ContributorProvince                  = p.Contributor != null ? p.Contributor.ContributorProvince      : null,
                    ContributorDepartment                = p.Contributor != null ? p.Contributor.ContributorDepartment    : null,
                    ContributorLegalEntityRegistryNumber = p.Contributor != null ? p.Contributor.LegalEntityRegistryNumber : null,

                    ProjectDistrict   = p.ProjectDistrict,
                    ProjectProvince   = p.ProjectProvince,
                    ProjectDepartment = p.ProjectDepartment,
                    ProjectLocation   = p.ProjectLocation,

                    ResponsableArqCom   = p.ResponsableArqCom,
                    ResponsableArqComId = p.ResponsableArqComId,
                    ResponsableUdp      = p.ResponsableUdp,
                    ResponsableUdpId    = p.ResponsableUdpId,

                    FechaInicio = p.FechaInicio,
                    FechaFin    = p.FechaFin,
                    InicioObra  = p.InicioObra,
                    FinObra     = p.FinObra,

                    NumNiveles           = p.NumNiveles,
                    NumSotanos           = p.NumSotanos,
                    Pisos                = p.Pisos,
                    TiempoConstruccion   = p.TiempoConstruccion,
                    AreaM2               = p.AreaM2,
                    AreaTechadaM2        = p.AreaTechadaM2,
                    HhTotalCasa          = p.HhTotalCasa,
                    CantTrabajadoresCasa = p.CantTrabajadoresCasa,

                    TieneArquitecturaComercial = p.TieneArquitecturaComercial,

                    Lat = p.Lat,
                    Lng = p.Lng,
                    RadioGeofenceMetros = p.RadioGeofenceMetros,

                    Active = p.Active
                })
                .ToListAsync();

            return new PagedResult<ProjectDto>
            {
                Page         = page,
                PageSize     = pageSize,
                TotalRecords = totalRecords,
                TotalPages   = (int)Math.Ceiling(totalRecords / (double)pageSize),
                Data         = data
            };
        }

        public async Task Create(ProjectCreateDto dto, int userId)
        {
            var existing = await _context.Project
                .FirstOrDefaultAsync(p => p.ProjectDescription.ToLower() == dto.ProjectDescription.Trim().ToLower());

            if (existing != null && existing.State)
                throw new AbrilException("Ya existe un proyecto con esa descripción.");

            if (existing != null && !existing.State)
            {
                ApplyDtoToEntity(existing, dto);
                existing.State           = true;
                existing.UpdatedDateTime = DateTime.UtcNow;
                existing.UpdatedUserId   = userId;
                await _context.SaveChangesAsync();
                await UpdateContributorLegalEntityRegistryNumberAsync(dto.ContributorId, dto.LegalEntityRegistryNumber, userId);
                await _checklist.SeedChecklistsObligatoriosAsync(existing.ProjectId, userId);
                return;
            }

            var project = new Project
            {
                ProjectDescription = dto.ProjectDescription.Trim(),
                State              = true,
                CreatedDateTime    = DateTime.UtcNow,
                CreatedUserId      = userId
            };
            ApplyDtoToEntity(project, dto);

            _context.Project.Add(project);
            await _context.SaveChangesAsync();
            await UpdateContributorLegalEntityRegistryNumberAsync(dto.ContributorId, dto.LegalEntityRegistryNumber, userId);
            await _checklist.SeedChecklistsObligatoriosAsync(project.ProjectId, userId);
        }

        public async Task Update(ProjectEditDto dto, int userId)
        {
            var project = await _context.Project
                .FirstOrDefaultAsync(p => p.ProjectId == dto.ProjectId);

            if (project == null)
                throw new AbrilException("El proyecto no existe.");

            var duplicate = await _context.Project
                .FirstOrDefaultAsync(p =>
                    p.ProjectDescription.ToLower() == dto.ProjectDescription.Trim().ToLower() &&
                    p.ProjectId != dto.ProjectId &&
                    p.State);

            if (duplicate != null)
                throw new AbrilException("Ya existe otro proyecto con la misma descripción.");

            ApplyDtoToEntity(project, dto);
            project.UpdatedDateTime = DateTime.UtcNow;
            project.UpdatedUserId   = userId;

            await _context.SaveChangesAsync();
            await UpdateContributorLegalEntityRegistryNumberAsync(dto.ContributorId, dto.LegalEntityRegistryNumber, userId);
        }

        public async Task<bool> DeleteSoftAsync(int projectId, int userId)
        {
            var project = await _context.Project
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.State);

            if (project == null)
                return false;

            project.State           = false;
            project.Active          = false;
            project.UpdatedDateTime = DateTime.UtcNow;
            project.UpdatedUserId   = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Contributor?> FindContributorByRuc(string ruc)
        {
            return await _context.Contributor
                .FirstOrDefaultAsync(c => c.ContributorRuc == ruc && c.State);
        }

        public async Task<Contributor> CreateContributor(string ruc, string name, string address, string economicActivity, string? district, string? province, string? department, int userId)
        {
            var contributor = new Contributor
            {
                ContributorRuc                         = ruc,
                ContributorName                        = name,
                ContributorAddress                     = address,
                ContributorEconomicActivityDescription = economicActivity,
                ContributorDistrict                    = district,
                ContributorProvince                    = province,
                ContributorDepartment                  = department,
                Active                                 = true,
                State                                  = true,
                // Este método solo lo llama ProjectController.CompanyLookup: la razón social
                // que resulta es siempre la SPE dueña del proyecto (nunca una contratista), así
                // que corresponde marcarla de una vez como empresa del grupo.
                EsAbril                                = true,
                CreatedDateTime                        = DateTimeOffset.UtcNow,
                CreatedUserId                          = userId
            };

            _context.Contributor.Add(contributor);
            await _context.SaveChangesAsync();
            return contributor;
        }

        public async Task<ProjectEmailsDto?> GetEmails(int projectId)
        {
            var emails = await _context.Project
                .Where(p => p.ProjectId == projectId && p.State)
                .Select(p => new ProjectEmailsDto
                {
                    ResidenteWorkersId = p.ResidenteWorkersId,
                    EmailResponsable   = p.EmailResponsable,
                    EmailRrhh          = p.EmailRrhh,
                    EmailCoordSsoma    = p.EmailCoordSsoma,
                    EmailCoordAdmin    = p.EmailCoordAdmin,
                })
                .FirstOrDefaultAsync();

            if (emails == null) return null;

            // Trabajadores elegibles como residente: los que tienen correo corporativo.
            // Van en la misma respuesta para que el formulario se arme con una sola petición.
            emails.Residentes = await _context.Worker
                .Where(w => w.EmailCorporativo != null && w.EmailCorporativo.Trim() != "")
                .Select(w => new ResidenteOptionDto
                {
                    WorkerId       = w.Id,
                    NombreCompleto = w.Person != null ? w.Person.FullName! : "",
                    Email          = w.EmailCorporativo!,
                })
                .AsNoTracking()
                .ToListAsync();

            emails.Residentes = emails.Residentes
                .OrderBy(r => r.NombreCompleto, StringComparer.CurrentCulture)
                .ToList();

            var actual = emails.Residentes.FirstOrDefault(r => r.WorkerId == emails.ResidenteWorkersId);
            emails.ResidenteNombre = actual?.NombreCompleto;
            emails.ResidenteEmail  = actual?.Email;

            return emails;
        }

        public async Task UpdateEmails(int id, ProjectEmailsUpdateDto dto)
        {
            var project = await _context.Project.FirstOrDefaultAsync(p => p.ProjectId == id);
            if (project == null)
                throw new AbrilException("El proyecto no existe.");

            // El residente es una FK, no un texto: null significa "sin residente" y limpia
            // el valor. El formulario siempre manda el objeto completo.
            if (dto.ResidenteWorkersId.HasValue)
            {
                var existe = await _context.Worker.AnyAsync(w => w.Id == dto.ResidenteWorkersId.Value);
                if (!existe)
                    throw new AbrilException("El trabajador seleccionado como residente no existe.");
            }
            project.ResidenteWorkersId = dto.ResidenteWorkersId;

            // Los correos de texto conservan su semántica: null es "no tocar" y string
            // vacío es "vaciar".
            if (dto.EmailResponsable != null) project.EmailResponsable = string.IsNullOrWhiteSpace(dto.EmailResponsable) ? null : dto.EmailResponsable.Trim();
            if (dto.EmailRrhh        != null) project.EmailRrhh        = string.IsNullOrWhiteSpace(dto.EmailRrhh)        ? null : dto.EmailRrhh.Trim();
            if (dto.EmailCoordSsoma  != null) project.EmailCoordSsoma  = string.IsNullOrWhiteSpace(dto.EmailCoordSsoma)  ? null : dto.EmailCoordSsoma.Trim();
            if (dto.EmailCoordAdmin  != null) project.EmailCoordAdmin  = string.IsNullOrWhiteSpace(dto.EmailCoordAdmin)  ? null : dto.EmailCoordAdmin.Trim();

            project.UpdatedDateTime = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task<List<ResponsableLookupDto>> GetResponsables(string tipo)
        {
            var subarea = tipo switch
            {
                "ARQ_COMERCIAL" => "Arquitectura Comercial",
                "UDP"           => "Unidad de Proyectos",
                _ => throw new AbrilException("Tipo de responsable inválido. Use ARQ_COMERCIAL o UDP.", 400)
            };

            return await _context.Worker
                .Where(w => w.WorkersEstadoId == WorkersEstadoIds.Activo && w.Subarea == subarea)
                .OrderBy(w => w.Person != null ? w.Person.FullName : null)
                .Select(w => new ResponsableLookupDto
                {
                    Id             = w.Id,
                    ApellidoNombre = (w.Person != null ? w.Person.FullName : null) ?? string.Empty
                })
                .ToListAsync();
        }

        /// <summary>
        /// Proyecto(s) "actuales" del usuario logueado, para preseleccionar el proyecto en
        /// dashboards que hoy arrancan vacíos. Dos fuentes, en orden:
        /// 1) `user_project` (misma tabla que usa Evaluaciones para "evaluadores
        ///    potenciales") — asignación explícita, hoy solo la puebla Lecciones Aprendidas.
        /// 2) Si el usuario es personal Casa (tiene Worker propio vía Person.UserId), su
        ///    vinculación activa (worker_vinculaciones.fecha_fin IS NULL) — el "obra actual"
        ///    que ya se ve en la ficha del trabajador. Es la fuente real para SSOMA/Habilitación
        ///    (caso Albines Caballero: sin fila en user_project, pero vinculación activa a
        ///    Gran Manzano).
        /// </summary>
        public async Task<List<int>> GetMyProjectIds(int userId)
        {
            var asignados = await _context.UserProject
                .Where(up => up.UserId == userId && up.State && up.Active)
                .Select(up => up.ProjectId)
                .Distinct()
                .ToListAsync();

            if (asignados.Count > 0) return asignados;

            return await _context.Worker
                .Where(w => w.Person != null && w.Person.UserId == userId)
                .SelectMany(w => _context.WorkerVinculacion
                    .Where(v => v.WorkerId == w.Id && v.FechaFin == null && v.ProyectoId != null)
                    .OrderByDescending(v => v.CreatedAt)
                    .Select(v => v.ProyectoId!.Value))
                .Distinct()
                .ToListAsync();
        }

        public async Task<bool?> ToggleArquitecturaComercial(int projectId)
        {
            var project = await _context.Project.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.State);
            if (project == null) return null;

            project.TieneArquitecturaComercial = !project.TieneArquitecturaComercial;
            project.UpdatedDateTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return project.TieneArquitecturaComercial;
        }

        public async Task UpdateContributorLocationAsync(int contributorId, string? district, string? province, string? department)
        {
            var contributor = await _context.Contributor.FindAsync(contributorId);
            if (contributor == null) return;

            contributor.ContributorDistrict   = district;
            contributor.ContributorProvince   = province;
            contributor.ContributorDepartment = department;
            contributor.UpdatedDateTime       = DateTimeOffset.UtcNow;

            await _context.SaveChangesAsync();
        }

        // ----- helpers privados -----

        /// <summary>
        /// Divide el término de búsqueda en palabras (en minúsculas) para permitir coincidencias
        /// por palabras en cualquier orden, igual que el matcher del componente app-search-input.
        /// </summary>
        private static IEnumerable<string> SplitBusqueda(string? busqueda)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
                return Enumerable.Empty<string>();

            return busqueda
                .ToLower()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>Aplica los campos editables del DTO Create a la entidad Project.</summary>
        private static void ApplyDtoToEntity(Project project, ProjectCreateDto dto)
        {
            project.ProjectDescription = dto.ProjectDescription.Trim();
            project.Codigo             = string.IsNullOrWhiteSpace(dto.Codigo)        ? null : dto.Codigo.Trim();
            project.Abbreviation       = string.IsNullOrWhiteSpace(dto.Abbreviation)  ? null : dto.Abbreviation.Trim();
            project.LevelDescription   = dto.LevelDescription?.Trim();
            project.Estado             = string.IsNullOrWhiteSpace(dto.Estado) ? null : dto.Estado.Trim();

            project.ContributorId      = dto.ContributorId;

            project.ProjectDistrict    = dto.ProjectDistrict?.Trim();
            project.ProjectProvince    = dto.ProjectProvince?.Trim();
            project.ProjectDepartment  = dto.ProjectDepartment?.Trim();
            project.ProjectLocation    = dto.ProjectLocation?.Trim();

            project.ResponsableArqCom    = dto.ResponsableArqCom?.Trim();
            project.ResponsableArqComId  = dto.ResponsableArqComId;
            project.ResponsableUdp       = dto.ResponsableUdp?.Trim();
            project.ResponsableUdpId     = dto.ResponsableUdpId;

            project.FechaInicio = dto.FechaInicio;
            project.FechaFin    = dto.FechaFin;
            project.InicioObra  = dto.InicioObra;
            project.FinObra     = dto.FinObra;

            project.NumNiveles           = string.IsNullOrWhiteSpace(dto.NumNiveles)           ? null : dto.NumNiveles.Trim();
            project.NumSotanos           = string.IsNullOrWhiteSpace(dto.NumSotanos)           ? null : dto.NumSotanos.Trim();
            project.Pisos                = string.IsNullOrWhiteSpace(dto.Pisos)                ? null : dto.Pisos.Trim();
            project.TiempoConstruccion   = dto.TiempoConstruccion;
            project.AreaM2               = dto.AreaM2;
            project.AreaTechadaM2        = dto.AreaTechadaM2;
            project.HhTotalCasa          = dto.HhTotalCasa;
            project.CantTrabajadoresCasa = string.IsNullOrWhiteSpace(dto.CantTrabajadoresCasa) ? null : dto.CantTrabajadoresCasa.Trim();

            project.TieneArquitecturaComercial = dto.TieneArquitecturaComercial ?? false;

            project.Active = dto.Active;
        }

        /// <summary>Aplica los campos editables del DTO Edit a la entidad Project.</summary>
        private static void ApplyDtoToEntity(Project project, ProjectEditDto dto)
        {
            ApplyGeolocalizacion(project, dto);
            project.ProjectDescription = dto.ProjectDescription.Trim();
            project.Codigo             = string.IsNullOrWhiteSpace(dto.Codigo)        ? null : dto.Codigo.Trim();
            project.Abbreviation       = string.IsNullOrWhiteSpace(dto.Abbreviation)  ? null : dto.Abbreviation.Trim();
            project.LevelDescription   = dto.LevelDescription?.Trim();
            project.Estado             = string.IsNullOrWhiteSpace(dto.Estado) ? null : dto.Estado.Trim();
            project.Activo             = string.IsNullOrWhiteSpace(dto.CicloVida) ? null : dto.CicloVida.Trim();

            project.ContributorId      = dto.ContributorId;

            project.ProjectDistrict    = dto.ProjectDistrict?.Trim();
            project.ProjectProvince    = dto.ProjectProvince?.Trim();
            project.ProjectDepartment  = dto.ProjectDepartment?.Trim();
            project.ProjectLocation    = dto.ProjectLocation?.Trim();

            project.ResponsableArqCom    = dto.ResponsableArqCom?.Trim();
            project.ResponsableArqComId  = dto.ResponsableArqComId;
            project.ResponsableUdp       = dto.ResponsableUdp?.Trim();
            project.ResponsableUdpId     = dto.ResponsableUdpId;

            project.FechaInicio = dto.FechaInicio;
            project.FechaFin    = dto.FechaFin;
            project.InicioObra  = dto.InicioObra;
            project.FinObra     = dto.FinObra;

            project.NumNiveles           = string.IsNullOrWhiteSpace(dto.NumNiveles)           ? null : dto.NumNiveles.Trim();
            project.NumSotanos           = string.IsNullOrWhiteSpace(dto.NumSotanos)           ? null : dto.NumSotanos.Trim();
            project.Pisos                = string.IsNullOrWhiteSpace(dto.Pisos)                ? null : dto.Pisos.Trim();
            project.TiempoConstruccion   = dto.TiempoConstruccion;
            project.AreaM2               = dto.AreaM2;
            project.AreaTechadaM2        = dto.AreaTechadaM2;
            project.HhTotalCasa          = dto.HhTotalCasa;
            project.CantTrabajadoresCasa = string.IsNullOrWhiteSpace(dto.CantTrabajadoresCasa) ? null : dto.CantTrabajadoresCasa.Trim();

            project.TieneArquitecturaComercial = dto.TieneArquitecturaComercial ?? false;

            project.Active = dto.Active;
        }

        /// <summary>Lat/Lng/RadioGeofenceMetros habilitan el geofencing de Tareo (Arquitectura
        /// Comercial) — sin esto, Marcar tareo siempre cae en REVISAR por "ningún proyecto activo
        /// tiene geolocalización configurada".</summary>
        private static void ApplyGeolocalizacion(Project project, ProjectEditDto dto)
        {
            project.Lat = dto.Lat;
            project.Lng = dto.Lng;
            if (dto.RadioGeofenceMetros.HasValue)
                project.RadioGeofenceMetros = dto.RadioGeofenceMetros.Value;
        }

        private async Task UpdateContributorLegalEntityRegistryNumberAsync(int? contributorId, string? legalEntityRegistryNumber, int userId)
        {
            if (contributorId == null) return;

            var contributor = await _context.Contributor.FindAsync(contributorId.Value);
            if (contributor == null) return;

            contributor.LegalEntityRegistryNumber = string.IsNullOrWhiteSpace(legalEntityRegistryNumber)
                ? null
                : legalEntityRegistryNumber.Trim();
            contributor.UpdatedDateTime = DateTimeOffset.UtcNow;
            contributor.UpdatedUserId   = userId;

            await _context.SaveChangesAsync();
        }
    }
}
