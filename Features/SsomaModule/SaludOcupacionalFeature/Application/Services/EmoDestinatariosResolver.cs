using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    /// <summary>
    /// Resuelve los destinatarios de un correo de EMO a partir de la matriz configurada
    /// en /ssoma/salud-ocupacional/emos/configuracion.
    ///
    /// Para cada trabajador se calcula su perfil (Oficina Central / Staff / Obra /
    /// Contratista) y se aplican las celdas activas de ese correo para ese perfil. Los
    /// destinatarios dinámicos se expanden acá; los buzones de área y los correos
    /// adicionales traen su correo desde la propia configuración — ya no hay ni un solo
    /// correo escrito en el código ni en el appsettings.
    ///
    /// Todo se resuelve por lotes: el número de consultas es fijo, sea para 1 trabajador
    /// o para 500, y solo se consulta lo que algún destinatario activo necesita.
    /// </summary>
    public class EmoDestinatariosResolver : IEmoDestinatariosResolver
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmoCorreoConfigRepository _correoConfig;
        private readonly IJefeRevisorResolver _jefeResolver;
        private readonly ILogger<EmoDestinatariosResolver> _logger;

        public EmoDestinatariosResolver(
            IDbContextFactory<AppDbContext> factory,
            IEmoCorreoConfigRepository correoConfig,
            IJefeRevisorResolver jefeResolver,
            ILogger<EmoDestinatariosResolver> logger)
        {
            _factory      = factory;
            _correoConfig = correoConfig;
            _jefeResolver = jefeResolver;
            _logger       = logger;
        }

        public Task<ProgramacionDestinatariosDto> ResolverAsync(
            string eventoCodigo, int workerId, int? clinicaId)
            => ResolverLoteAsync(eventoCodigo, new[] { workerId }, clinicaId);

        public async Task<ProgramacionDestinatariosDto> ResolverLoteAsync(
            string eventoCodigo, IReadOnlyCollection<int> workerIds, int? clinicaId)
        {
            var resultado = new ProgramacionDestinatariosDto();

            var ids = workerIds.Where(id => id > 0).Distinct().ToList();
            if (ids.Count == 0) return resultado;

            var reglas = await _correoConfig.GetReglasEnvioAsync(eventoCodigo);
            if (reglas.Count == 0)
            {
                _logger.LogWarning(
                    "Correo de EMO {Evento}: no hay ningún destinatario activo en la configuración.",
                    eventoCodigo);
                return resultado;
            }

            var reglasPorPerfil = reglas.ToLookup(r => r.PerfilCodigo, StringComparer.OrdinalIgnoreCase);
            var codigosActivos = new HashSet<string>(
                reglas.Where(r => !string.IsNullOrWhiteSpace(r.DestinatarioCodigo))
                      .Select(r => r.DestinatarioCodigo!),
                StringComparer.OrdinalIgnoreCase);

            using var ctx = _factory.CreateDbContext();

            var workers = await ctx.Worker.AsNoTracking()
                .Where(w => ids.Contains(w.Id))
                .Select(w => new WorkerContexto
                {
                    Id                 = w.Id,
                    ContrataCasa       = w.ContrataCasa,
                    ObraOficinaStaffId = w.ObraOficinaStaffId,
                    EmailCorporativo   = w.EmailCorporativo,
                    ContributorId      = w.ContributorId,
                    Subarea            = w.Subarea,
                })
                .ToListAsync();
            if (workers.Count == 0) return resultado;

            // ── Solo se consulta lo que algún destinatario activo necesita ──
            var necesitaProyecto =
                codigosActivos.Contains(EmoCorreoDestinatarioCodigo.Residente)  ||
                codigosActivos.Contains(EmoCorreoDestinatarioCodigo.CoordAdmin) ||
                codigosActivos.Contains(EmoCorreoDestinatarioCodigo.CoordSsoma) ||
                EmoCorreoDestinatarioCodigo.SoloConArquitecturaComercial.Any(codigosActivos.Contains);

            if (necesitaProyecto)
            {
                var vinculaciones = await ctx.WorkerVinculacion.AsNoTracking()
                    .Where(v => ids.Contains(v.WorkerId) && v.FechaFin == null)
                    .OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id)
                    .Select(v => new { v.WorkerId, v.ProyectoId })
                    .ToListAsync();

                var proyectoPorWorker = vinculaciones
                    .GroupBy(v => v.WorkerId)
                    .ToDictionary(g => g.Key, g => g.First().ProyectoId);

                var proyectoIds = proyectoPorWorker.Values
                    .Where(p => p.HasValue).Select(p => p!.Value).Distinct().ToList();

                // El correo del residente sale de su ficha de trabajador
                // (project.residente_workers_id → workers.email_corporativo), no de una
                // copia guardada en el proyecto: así sigue al dato maestro si cambia.
                var proyectos = proyectoIds.Count == 0
                    ? new Dictionary<int, ProyectoContexto>()
                    : await (
                        from p in ctx.Project.AsNoTracking()
                        where proyectoIds.Contains(p.ProjectId)
                        join rw in ctx.Worker.AsNoTracking() on p.ResidenteWorkersId equals rw.Id into rwj
                        from residente in rwj.DefaultIfEmpty()
                        select new ProyectoContexto
                        {
                            ProjectId       = p.ProjectId,
                            Nombre          = p.ProjectDescription,
                            EmailResidente  = residente != null ? residente.EmailCorporativo : null,
                            EmailCoordAdmin = p.EmailCoordAdmin,
                            EmailCoordSsoma = p.EmailCoordSsoma,
                            TieneArqCom     = p.TieneArquitecturaComercial,
                        })
                        .ToDictionaryAsync(p => p.ProjectId);

                foreach (var w in workers)
                    if (proyectoPorWorker.TryGetValue(w.Id, out var proyectoId) &&
                        proyectoId.HasValue &&
                        proyectos.TryGetValue(proyectoId.Value, out var proyecto))
                        w.Proyecto = proyecto;
            }

            // Un trabajador es de Arquitectura Comercial si es staff con esa subárea, o si
            // es obrero cuyo proyecto actual es, literalmente, el proyecto "Arquitectura
            // Comercial" — mismo criterio que ArquitecturaComercialRepository.GetSupervisoresAc.
            // Project.TieneArquitecturaComercial NO sirve para esto: ese flag solo marca qué
            // proyectos aparecen en Observaciones/Revisiones, no de qué trabajadores son AC
            // (por eso antes le llegaba este correo a cualquier ingreso de un proyecto con el
            // flag activado, sin que el trabajador tuviera nada que ver con AC).
            foreach (var w in workers)
                w.EsArquitecturaComercial =
                    string.Equals(w.Subarea, "Arquitectura Comercial", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(w.Proyecto?.Nombre, "Arquitectura Comercial", StringComparison.OrdinalIgnoreCase);

            if (codigosActivos.Contains(EmoCorreoDestinatarioCodigo.AdminRazonSocial))
            {
                var contributorIds = workers
                    .Where(w => w.ContributorId.HasValue).Select(w => w.ContributorId!.Value)
                    .Distinct().ToList();

                if (contributorIds.Count > 0)
                {
                    var admins = await ctx.Contributor.AsNoTracking()
                        .Where(c => contributorIds.Contains(c.ContributorId))
                        .Select(c => new { c.ContributorId, c.EmailAdministrador })
                        .ToDictionaryAsync(c => c.ContributorId, c => c.EmailAdministrador);

                    foreach (var w in workers)
                        if (w.ContributorId.HasValue &&
                            admins.TryGetValue(w.ContributorId.Value, out var email))
                            w.EmailAdminRazonSocial = email;
                }
            }

            // Clínica de la cita: los correos de contacto del catálogo y, si no hay
            // ninguno, el correo suelto de la ficha de la clínica.
            var clinicaEmails = new List<string>();
            string? clinicaNombre = null;
            if (codigosActivos.Contains(EmoCorreoDestinatarioCodigo.Clinica) && clinicaId is > 0)
            {
                var clinica = await ctx.SsClinica.AsNoTracking()
                    .Where(c => c.Id == clinicaId.Value)
                    .Select(c => new { c.Nombre, c.Email })
                    .FirstOrDefaultAsync();
                clinicaNombre = clinica?.Nombre;

                clinicaEmails = await ctx.SsClinicaEmail.AsNoTracking()
                    .Where(e => e.ClinicaId == clinicaId.Value && e.Activo && e.Email != null)
                    .Select(e => e.Email!)
                    .ToListAsync();

                if (clinicaEmails.Count == 0 && !string.IsNullOrWhiteSpace(clinica?.Email))
                    clinicaEmails.Add(clinica!.Email!);

                if (clinicaEmails.Count == 0)
                    _logger.LogWarning(
                        "Correo de EMO {Evento}: la clínica {ClinicaId} está activa como destinataria pero no tiene ningún correo de contacto.",
                        eventoCodigo, clinicaId);
            }

            // GTH: siempre el correo del área de Gestión del Talento Humano, para que
            // siga a lo que se configure en Configuración → Áreas.
            string? gthEmail = null;
            if (codigosActivos.Contains(EmoCorreoDestinatarioCodigo.Gth))
            {
                gthEmail = await ctx.AreaScope.AsNoTracking()
                    .Where(s => s.AreaScopeId == AreaScopeIds.GestionDelTalentoHumano && s.State)
                    .Select(s => s.Email)
                    .FirstOrDefaultAsync();

                if (string.IsNullOrWhiteSpace(gthEmail))
                    _logger.LogWarning(
                        "Correo de EMO {Evento}: GTH está activo pero el área {AreaScopeId} no tiene correo configurado.",
                        eventoCodigo, AreaScopeIds.GestionDelTalentoHumano);
            }

            var jefes = new Dictionary<int, JefeRevisorResolution>();
            if (codigosActivos.Contains(EmoCorreoDestinatarioCodigo.Jefe))
            {
                try { jefes = await _jefeResolver.ResolveManyAsync(ids); }
                catch (Exception ex)
                {
                    // Best-effort: si la resolución falla, el correo sale igual con el resto.
                    _logger.LogWarning(ex,
                        "Correo de EMO {Evento}: error resolviendo los jefes; el correo sale sin ellos.",
                        eventoCodigo);
                }
            }

            // ── Armado ──
            var para   = new Dictionary<string, ProgramacionDestinatarioDto>(StringComparer.OrdinalIgnoreCase);
            var copias = new Dictionary<string, ProgramacionDestinatarioDto>(StringComparer.OrdinalIgnoreCase);

            foreach (var w in workers)
            {
                var perfil = EmoCorreoPerfilCodigo.Resolver(w.ContrataCasa, w.ObraOficinaStaffId);

                // Trabajador de contratista: Abril no controla su EMO, así que no le
                // corresponde ninguno de estos correos y no tiene columna en la matriz.
                if (perfil == null)
                {
                    _logger.LogInformation(
                        "Correo de EMO {Evento}: el trabajador {WorkerId} es de una contratista, no se le notifica.",
                        eventoCodigo, w.Id);
                    continue;
                }

                foreach (var regla in reglasPorPerfil[perfil])
                {
                    // La clínica es destinataria de este correo para el perfil de este
                    // trabajador, pero su correo puede no poder resolverse todavía. Se
                    // marca acá y no arriba con `codigosActivos` porque eso mira las
                    // reglas de los cuatro perfiles juntos, no las del trabajador.
                    if (string.Equals(regla.DestinatarioCodigo, EmoCorreoDestinatarioCodigo.Clinica,
                                      StringComparison.OrdinalIgnoreCase))
                    {
                        if (clinicaId is not > 0) resultado.ClinicaPendiente = true;
                        else if (clinicaEmails.Count == 0) resultado.ClinicaSinCorreos = true;
                    }

                    var destino = regla.EsCopia ? copias : para;

                    foreach (var (email, nombre) in ExpandirRegla(regla, w, jefes, clinicaEmails, clinicaNombre, gthEmail))
                    {
                        if (string.IsNullOrWhiteSpace(email)) continue;
                        var clave = email.Trim();
                        if (destino.ContainsKey(clave)) continue;

                        destino[clave] = new ProgramacionDestinatarioDto
                        {
                            Email  = clave,
                            Nombre = nombre ?? regla.Nombre,
                            Origen = regla.DestinatarioCodigo ?? ProgramacionDestinatarioOrigen.Adicional,
                        };
                    }
                }
            }

            resultado.Para = para.Values.ToList();
            // Un mismo buzón no puede estar en "Para" y en "CC" a la vez.
            resultado.Copias = copias
                .Where(kv => !para.ContainsKey(kv.Key))
                .Select(kv => kv.Value)
                .ToList();

            return resultado;
        }

        /// <summary>
        /// Correos concretos que aporta una celda de la matriz para un trabajador. Un
        /// destinatario puede aportar 0 (no aplica o no está cargado), 1 o varios
        /// (la clínica suele tener más de un correo de contacto).
        /// </summary>
        private static IEnumerable<(string? Email, string? Nombre)> ExpandirRegla(
            EmoCorreoReglaEnvioDto regla,
            WorkerContexto w,
            IReadOnlyDictionary<int, JefeRevisorResolution> jefes,
            IReadOnlyList<string> clinicaEmails,
            string? clinicaNombre,
            string? gthEmail)
        {
            // Correo adicional o buzón de área: el correo ya viene en la configuración.
            if (string.IsNullOrWhiteSpace(regla.DestinatarioCodigo))
                return new[] { ((string?)regla.Email, (string?)regla.Nombre) };

            switch (regla.DestinatarioCodigo.ToUpperInvariant())
            {
                case EmoCorreoDestinatarioCodigo.Clinica:
                    return clinicaEmails.Select(e => ((string?)e, (string?)(clinicaNombre ?? regla.Nombre)));

                case EmoCorreoDestinatarioCodigo.Jefe:
                    return jefes.TryGetValue(w.Id, out var jefe)
                        ? new[] { ((string?)jefe.Email, (string?)(jefe.Nombre ?? regla.Nombre)) }
                        : Array.Empty<(string?, string?)>();

                case EmoCorreoDestinatarioCodigo.Trabajador:
                    return new[] { ((string?)w.EmailCorporativo, (string?)regla.Nombre) };

                case EmoCorreoDestinatarioCodigo.Residente:
                    return new[] { ((string?)w.Proyecto?.EmailResidente, (string?)regla.Nombre) };

                case EmoCorreoDestinatarioCodigo.CoordAdmin:
                    return new[] { ((string?)w.Proyecto?.EmailCoordAdmin, (string?)regla.Nombre) };

                case EmoCorreoDestinatarioCodigo.CoordSsoma:
                    return new[] { ((string?)w.Proyecto?.EmailCoordSsoma, (string?)regla.Nombre) };

                case EmoCorreoDestinatarioCodigo.AdminRazonSocial:
                    return new[] { ((string?)w.EmailAdminRazonSocial, (string?)regla.Nombre) };

                case EmoCorreoDestinatarioCodigo.Gth:
                    return new[] { ((string?)gthEmail, (string?)regla.Nombre) };

                case EmoCorreoDestinatarioCodigo.ArqComJefe:
                case EmoCorreoDestinatarioCodigo.ArqComPrevencionista:
                    // Solo si el propio trabajador es de Arquitectura Comercial (no basta con
                    // que su proyecto tenga el flag TieneArquitecturaComercial — ver comentario
                    // en el cálculo de w.EsArquitecturaComercial más arriba).
                    if (!w.EsArquitecturaComercial)
                        return Array.Empty<(string?, string?)>();

                    return new[] { ((string?)regla.Email, (string?)regla.Nombre) };

                default:
                    // Buzón de área con condición: Post Venta solo escribe cuando el proyecto
                    // del trabajador tiene arquitectura comercial.
                    if (EmoCorreoDestinatarioCodigo.RequiereArquitecturaComercial(regla.DestinatarioCodigo)
                        && w.Proyecto?.TieneArqCom != true)
                        return Array.Empty<(string?, string?)>();

                    return new[] { ((string?)regla.Email, (string?)regla.Nombre) };
            }
        }

        private sealed class WorkerContexto
        {
            public int Id { get; set; }
            public string? ContrataCasa { get; set; }
            public int? ObraOficinaStaffId { get; set; }
            public string? EmailCorporativo { get; set; }
            public int? ContributorId { get; set; }
            public string? EmailAdminRazonSocial { get; set; }
            public string? Subarea { get; set; }
            public bool EsArquitecturaComercial { get; set; }
            public ProyectoContexto? Proyecto { get; set; }
        }

        private sealed class ProyectoContexto
        {
            public int ProjectId { get; set; }
            public string? Nombre { get; set; }
            public string? EmailResidente { get; set; }
            public string? EmailCoordAdmin { get; set; }
            public string? EmailCoordSsoma { get; set; }
            public bool? TieneArqCom { get; set; }
        }
    }
}
