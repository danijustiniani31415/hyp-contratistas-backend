using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Implementación de <see cref="ICorreoSalidaRecipientResolver"/>. Lee la configuración de
    /// destinatarios (ga_correo_evento / ga_correo_tipo_destinatario / ga_correo_regla) en un
    /// contexto propio y de corta vida. Ver <see cref="ResolveEnvioAsync"/> para la lógica.
    /// </summary>
    public class CorreoSalidaRecipientResolver : ICorreoSalidaRecipientResolver
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly ILogger<CorreoSalidaRecipientResolver> _logger;

        // Códigos del catálogo ga_correo_tipo_destinatario (ver CorreoTipoCodigos).
        private const string TipoTrabajador = CorreoTipoCodigos.Trabajador;
        private const string TipoArea = CorreoTipoCodigos.Area;
        private const string TipoCorreo = CorreoTipoCodigos.Correo;

        public CorreoSalidaRecipientResolver(
            IDbContextFactory<AppDbContext> factory,
            ILogger<CorreoSalidaRecipientResolver> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<CorreoSalidaEnvioDto> ResolveEnvioAsync(
            string eventoCodigo,
            IEnumerable<string>? destinatarioPrincipal = null,
            IEnumerable<string>? baseCc = null)
        {
            var principal = (destinatarioPrincipal ?? Enumerable.Empty<string>()).ToList();
            var copiaBase = (baseCc ?? Enumerable.Empty<string>()).ToList();

            try
            {
                using var ctx = _factory.CreateDbContext();

                // 1) Los dos interruptores del correo (1 query).
                var evento = await ctx.GaCorreoEvento
                    .Where(e => e.Codigo == eventoCodigo && e.State)
                    .Select(e => new { e.Id, e.Active, e.DestinatarioPrincipalActivo })
                    .FirstOrDefaultAsync();

                if (evento == null)
                {
                    _logger.LogWarning(
                        "El correo {Evento} no está en ga_correo_evento; se envía solo con los destinatarios base.",
                        eventoCodigo);
                    return Armar(principal, copiaBase, null, null, true);
                }

                // Apagado desde la configuración: no se envía y no hace falta leer sus reglas.
                if (!evento.Active)
                    return new CorreoSalidaEnvioDto { Enviar = false };

                // 2) Reglas vivas + activas del correo, con el código de su tipo (1 query).
                var reglas = await (
                    from r in ctx.GaCorreoRegla
                    join t in ctx.GaCorreoTipoDestinatario on r.TipoId equals t.Id
                    where r.EventoId == evento.Id && r.State && r.Active
                    select new
                    {
                        r.EsExclusion,
                        TipoCodigo = t.Codigo,
                        r.WorkerId,
                        r.AreaScopeId,
                        r.Correo,
                        r.IncluirDescendientes,
                    }
                ).ToListAsync();

                if (reglas.Count == 0)
                    return Armar(principal, copiaBase, null, null, evento.DestinatarioPrincipalActivo);

                // 3) Correos de los trabajadores referenciados (1 query).
                var workerIds = reglas
                    .Where(r => r.TipoCodigo == TipoTrabajador && r.WorkerId.HasValue)
                    .Select(r => r.WorkerId!.Value)
                    .Distinct()
                    .ToList();

                var workerEmailById = workerIds.Count == 0
                    ? new Dictionary<int, string>()
                    : await ctx.Worker
                        .Where(w => workerIds.Contains(w.Id)
                                    && w.EmailCorporativo != null && w.EmailCorporativo != "")
                        .Select(w => new { w.Id, Email = w.EmailCorporativo! })
                        .ToDictionaryAsync(x => x.Id, x => x.Email);

                // 4) Expansión de áreas → correos de sus miembros (árbol + workers, 2 queries).
                var emailsByAreaId = new Dictionary<int, List<string>>();
                Dictionary<int, List<int>> childrenByParent = new();
                var areaRules = reglas.Where(r => r.TipoCodigo == TipoArea && r.AreaScopeId.HasValue).ToList();
                if (areaRules.Count > 0)
                {
                    var tree = await GaAreaTreeLoader.LoadAsync(ctx);
                    childrenByParent = tree
                        .Where(n => n.AreaScopeParentId.HasValue)
                        .GroupBy(n => n.AreaScopeParentId!.Value)
                        .ToDictionary(g => g.Key, g => g.Select(n => n.AreaScopeId).ToList());

                    var todosLosNodos = new HashSet<int>();
                    foreach (var r in areaRules)
                        foreach (var id in Expand(r.AreaScopeId!.Value, r.IncluirDescendientes, childrenByParent))
                            todosLosNodos.Add(id);

                    var miembros = await ctx.Worker
                        .Where(w => w.AreaScopeId != null && todosLosNodos.Contains(w.AreaScopeId.Value)
                                    && w.EmailCorporativo != null && w.EmailCorporativo != "")
                        .Select(w => new { AreaScopeId = w.AreaScopeId!.Value, Email = w.EmailCorporativo! })
                        .ToListAsync();

                    emailsByAreaId = miembros
                        .GroupBy(m => m.AreaScopeId)
                        .ToDictionary(g => g.Key, g => g.Select(m => m.Email).ToList());
                }

                // 5) Armar inclusiones / exclusiones.
                var includes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var excludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var r in reglas)
                {
                    var target = r.EsExclusion ? excludes : includes;
                    switch (r.TipoCodigo)
                    {
                        case TipoTrabajador:
                            if (r.WorkerId.HasValue && workerEmailById.TryGetValue(r.WorkerId.Value, out var wEmail))
                                target.Add(wEmail.Trim());
                            break;
                        case TipoArea:
                            if (r.AreaScopeId.HasValue)
                                foreach (var nodoId in Expand(r.AreaScopeId.Value, r.IncluirDescendientes, childrenByParent))
                                    if (emailsByAreaId.TryGetValue(nodoId, out var areaEmails))
                                        foreach (var em in areaEmails) target.Add(em.Trim());
                            break;
                        case TipoCorreo:
                            if (!string.IsNullOrWhiteSpace(r.Correo))
                                target.Add(r.Correo.Trim());
                            break;
                    }
                }

                return Armar(principal, copiaBase, includes, excludes, evento.DestinatarioPrincipalActivo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error resolviendo destinatarios configurados del correo {Evento}; se usa solo la base.",
                    eventoCodigo);
                return Armar(principal, copiaBase, null, null, true);
            }
        }

        /// <summary>
        /// Reparte los correos entre "Para" y "Copia": el principal va al Para (salvo que su
        /// interruptor esté apagado) y las exclusiones solo recortan las copias. Si el Para
        /// queda vacío, las copias lo ocupan — así apagar al principal deja el correo en manos
        /// de los destinatarios configurados en vez de mandarlo sin nadie en el Para.
        /// </summary>
        private static CorreoSalidaEnvioDto Armar(
            List<string> principal,
            List<string> copiaBase,
            HashSet<string>? includes,
            HashSet<string>? excludes,
            bool principalActivo)
        {
            var para = principalActivo ? Limpiar(principal, null) : new List<string>();
            var yaEnPara = new HashSet<string>(para, StringComparer.OrdinalIgnoreCase);

            var fuera = new HashSet<string>(yaEnPara, StringComparer.OrdinalIgnoreCase);
            if (excludes != null) foreach (var e in excludes) fuera.Add(e);

            var copia = Limpiar(copiaBase.Concat(includes ?? Enumerable.Empty<string>()), fuera);

            // Sin destinatario principal, las copias pasan a ser el "Para".
            if (para.Count == 0)
            {
                para = copia;
                copia = new List<string>();
            }

            return new CorreoSalidaEnvioDto
            {
                Enviar = para.Count > 0,
                Para = para,
                Copia = copia,
            };
        }

        /// <summary>Normaliza una lista de correos: sin vacíos, sin duplicados (case-insensitive) y sin los descartados.</summary>
        private static List<string> Limpiar(IEnumerable<string> correos, HashSet<string>? descartar)
        {
            var result = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var raw in correos)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var email = raw.Trim();
                if (descartar != null && descartar.Contains(email)) continue;
                if (seen.Add(email)) result.Add(email);
            }
            return result;
        }

        /// <summary>Devuelve el nodo y, si <paramref name="incluirDescendientes"/>, todos sus descendientes.</summary>
        private static IEnumerable<int> Expand(int areaScopeId, bool incluirDescendientes, Dictionary<int, List<int>> childrenByParent)
        {
            var resultado = new HashSet<int> { areaScopeId };
            if (!incluirDescendientes) return resultado;

            var cola = new Queue<int>();
            cola.Enqueue(areaScopeId);
            while (cola.Count > 0)
            {
                var actual = cola.Dequeue();
                if (childrenByParent.TryGetValue(actual, out var hijos))
                    foreach (var h in hijos)
                        if (resultado.Add(h)) cola.Enqueue(h);
            }
            return resultado;
        }
    }
}
