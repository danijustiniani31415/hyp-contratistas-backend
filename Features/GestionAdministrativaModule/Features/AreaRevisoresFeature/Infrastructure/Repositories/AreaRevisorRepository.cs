using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using AreaRevisoresModel = Abril_Backend.Features.GestionAdministrativa.Shared.Models.AreaRevisores;

namespace Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Infrastructure.Repositories
{
    /// <summary>
    /// Lectura/escritura de los revisores de salidas por área (area_revisores):
    /// n revisores por nodo area_scope, ordenados por prioridad (1 = primero).
    /// Se configuran las áreas de tipo "Área de Gerencia" y "Área Estándar" que son el
    /// primer nodo de SU MISMO tipo en su rama (si un Área Estándar cuelga de otra Área
    /// Estándar, la hija no se lista, para no confundir a los usuarios con subáreas; lo
    /// mismo entre gerencias). Solo se listan los tipos configurables (Gerencia/Estándar). Estos
    /// revisores aplican a los trabajadores del subárbol del nodo que no tengan revisores
    /// propios en workers_revisores; sin revisores de área, el fallback es GTH.
    ///
    /// Una gerencia y las áreas estándar que cuelgan de ella coexisten en la lista aunque
    /// una sea padre de la otra (ej. "Gerencia de Proyectos" y "Unidad de Proyectos"): el
    /// resolver toma siempre el nodo más cercano al trabajador, así que la gerencia es el
    /// área propia de los gerentes (que cuelgan directamente de ella y antes caían al
    /// fallback de GTH) y el respaldo del resto de su rama.
    ///
    /// Visibilidad: los roles ADMINISTRADOR DE SOLICITUD DE SALIDAS y USUARIO DE GTH ven
    /// todas las áreas y pueden editarlas; un trabajador de las categorías
    /// <see cref="CategoriaIds.ConVistaDeSuArea"/> (Jefe, Coordinador o Gerente) ve solo el
    /// área listada a la que pertenece (subiendo el árbol desde su workers.area_scope_id) y
    /// sin poder editarla; el resto no ve ninguna.
    /// </summary>
    public class AreaRevisorRepository : IAreaRevisorRepository
    {
        private const string EmailDomainCorp = "@abril.pe";
        private const string AreaTypeEstandar = "Área Estándar";
        private const string AreaTypeGerencia = "Área de Gerencia";

        /// <summary>Tipos de área que admiten revisores, en el orden en que se listan.</summary>
        private static readonly string[] TiposConfigurables = { AreaTypeGerencia, AreaTypeEstandar };

        private readonly IDbContextFactory<AppDbContext> _factory;

        public AreaRevisorRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<AreaRevisorInicialDto> GetInitialDataAsync(int userId, bool verTodas)
        {
            // Tabla + opciones en una sola conexión.
            using var ctx = _factory.CreateDbContext();

            // 1) Árbol completo de áreas vivas (lista plana) para resolver en memoria
            //    qué nodos son el primero de su tipo en su rama.
            var nodos = await LoadNodosAsync(ctx);
            var elegibles = ConfigurableNodes(nodos);

            if (!verTodas)
            {
                // Jefe/Coordinador/Gerente: solo el área listada a la que pertenece
                // su worker. Cualquier otro usuario (sin rol de admin ni de GTH): ninguna.
                var areaVisible = await GetAreaVisibleDelUsuarioAsync(ctx, userId, nodos, elegibles);
                if (areaVisible == null)
                    return new AreaRevisorInicialDto();
                elegibles = elegibles.Where(n => n.AreaScopeId == areaVisible.Value).ToList();
            }

            // Las gerencias primero (raíz de cada rama) y luego las áreas estándar,
            // cada bloque en orden alfabético.
            var areas = elegibles
                .OrderBy(n => Array.IndexOf(TiposConfigurables, n.AreaTypeName))
                .ThenBy(n => n.AreaItemName)
                .Select(n => new AreaRevisorItemDto
                {
                    AreaScopeId = n.AreaScopeId,
                    AreaName = n.AreaItemName,
                    AreaTypeName = n.AreaTypeName,
                    ParentName = n.AreaScopeParentId != null
                        ? nodos.FirstOrDefault(p => p.AreaScopeId == n.AreaScopeParentId)?.AreaItemName
                        : null,
                })
                .ToList();

            // 2) Revisores vivos de las áreas listadas, con los datos del revisor resueltos (una sola query).
            //    Se trae también el project_id: NULL = revisor a nivel de área; con valor = revisor
            //    de ese proyecto dentro del área (áreas "filtradas por proyecto").
            var areaIds = areas.Select(a => a.AreaScopeId).ToList();
            var asignaciones = await (
                from r in ctx.AreaRevisores
                where r.State && areaIds.Contains(r.AreaScopeId)
                join w in ctx.Worker on r.RevisorId equals w.Id
                join p in ctx.Person on w.PersonId equals p.PersonId into pj
                from p in pj.DefaultIfEmpty()
                join pu in ctx.Puesto on w.PuestoId equals pu.PuestoId into puj
                from pu in puj.DefaultIfEmpty()
                join c in ctx.Categoria on pu.CategoriaId equals c.CategoriaId into cj
                from c in cj.DefaultIfEmpty()
                orderby r.AreaScopeId, r.OrdenPrioridad, r.AreaRevisoresId
                select new
                {
                    r.AreaScopeId,
                    r.ProjectId,
                    Dto = new AreaRevisorAsignadoDto
                    {
                        Id = r.AreaRevisoresId,
                        RevisorWorkerId = r.RevisorId,
                        RevisorFullName = p != null ? p.FullName : null,
                        RevisorEmail = w.EmailCorporativo,
                        RevisorCategory = c != null ? c.Nombre : null,
                        OrdenPrioridad = r.OrdenPrioridad,
                        Active = r.Active,
                    }
                }
            ).ToListAsync();

            // Revisores a nivel de área (project_id NULL).
            var porArea = asignaciones
                .Where(a => a.ProjectId == null)
                .GroupBy(a => a.AreaScopeId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            // Revisores por proyecto (project_id con valor).
            var porAreaProyecto = asignaciones
                .Where(a => a.ProjectId != null)
                .GroupBy(a => (a.AreaScopeId, ProjectId: a.ProjectId!.Value))
                .ToDictionary(g => g.Key, g => g.Select(x => x.Dto).ToList());

            // 3) Flags "filtrar por proyecto" (ga_salidas_area_config) de las áreas listadas.
            var flags = await ctx.GaSalidasAreaConfig
                .Where(f => f.State && areaIds.Contains(f.AreaScopeId))
                .Select(f => new { f.AreaScopeId, f.FiltraPorProyecto })
                .ToListAsync();
            var flagByArea = flags
                .GroupBy(f => f.AreaScopeId)
                .ToDictionary(g => g.Key, g => g.First().FiltraPorProyecto);

            // 4) Catálogo de proyectos activos (para subfilas y selector de proyecto).
            var proyectos = await (
                from pr in ctx.Project
                where pr.State && pr.Active
                orderby pr.ProjectDescription
                select new ProyectoOptionDto { ProjectId = pr.ProjectId, ProjectName = pr.ProjectDescription }
            ).ToListAsync();

            foreach (var a in areas)
            {
                if (porArea.TryGetValue(a.AreaScopeId, out var revs)) a.Revisores = revs;
                a.FiltraPorProyecto = flagByArea.TryGetValue(a.AreaScopeId, out var f) && f;

                if (a.FiltraPorProyecto)
                {
                    // Solo los proyectos que ya tienen algún revisor asignado en esta área;
                    // el frontend arma la subfila de cada proyecto desde la lista global.
                    a.Proyectos = proyectos
                        .Where(pr => porAreaProyecto.ContainsKey((a.AreaScopeId, pr.ProjectId)))
                        .Select(pr => new AreaProyectoRevisoresDto
                        {
                            ProjectId = pr.ProjectId,
                            ProjectName = pr.ProjectName,
                            Revisores = porAreaProyecto[(a.AreaScopeId, pr.ProjectId)],
                        })
                        .ToList();
                }
            }

            // 5) Opciones del selector de revisor (mismo criterio que Revisores de Trabajadores).
            //    Solo quien ve todas las áreas puede editarlas, así que solo esos las necesitan.
            var options = !verTodas
                ? new List<AreaRevisorOptionDto>()
                : await (
                    from w in ctx.Worker
                    where w.EmailCorporativo != null && w.EmailCorporativo.ToLower().Contains(EmailDomainCorp)
                    join p in ctx.Person on w.PersonId equals p.PersonId
                    where p.State == true
                    orderby p.FullName
                    select new AreaRevisorOptionDto
                    {
                        WorkerId = w.Id,
                        FullName = p.FullName,
                        Email = w.EmailCorporativo
                    }
                ).ToListAsync();

            return new AreaRevisorInicialDto
            {
                Areas = areas,
                Options = options,
                // El frontend arma con este catálogo las subfilas de proyecto de las áreas
                // filtradas, así que lo necesita todo el que ve la lista completa.
                Proyectos = verTodas ? proyectos : new List<ProyectoOptionDto>(),
            };
        }

        public async Task UpdateAreaRevisoresAsync(int areaScopeId, int? projectId, List<AreaRevisorAsignacionDto> revisores)
        {
            using var ctx = _factory.CreateDbContext();

            // El área debe existir, estar viva y ser el primer nodo de su tipo en su rama
            // (los mismos nodos que lista la pantalla).
            var nodos = await LoadNodosAsync(ctx);
            var area = ConfigurableNodes(nodos).FirstOrDefault(n => n.AreaScopeId == areaScopeId);
            if (area == null)
                throw new AbrilException("El área no existe o no admite revisores (solo áreas de tipo Área de Gerencia o Área Estándar).", 404);

            // Si se especifica proyecto, debe existir y estar vivo.
            if (projectId != null)
            {
                var proyectoValido = await ctx.Project.AnyAsync(p => p.ProjectId == projectId.Value && p.State);
                if (!proyectoValido)
                    throw new AbrilException("El proyecto no existe.", 404);
            }

            var deseados = revisores ?? new List<AreaRevisorAsignacionDto>();

            // ── Validaciones (mismo criterio que workers_revisores) ─────────
            if (deseados.GroupBy(r => r.RevisorWorkerId).Any(g => g.Count() > 1))
                throw new AbrilException("No se puede asignar dos veces al mismo revisor.", 400);

            if (deseados.Any(r => r.OrdenPrioridad < 1))
                throw new AbrilException("La prioridad debe ser 1 o mayor.", 400);

            if (deseados.GroupBy(r => r.OrdenPrioridad).Any(g => g.Count() > 1))
                throw new AbrilException("No puede haber dos revisores con la misma prioridad.", 400);

            if (deseados.Count > 0)
            {
                var ids = deseados.Select(r => r.RevisorWorkerId).ToList();
                var validos = await ctx.Worker
                    .Where(w => ids.Contains(w.Id)
                                && w.EmailCorporativo != null
                                && w.EmailCorporativo.Trim().ToLower().EndsWith(EmailDomainCorp))
                    .Select(w => w.Id)
                    .ToListAsync();
                var faltantes = ids.Except(validos).ToList();
                if (faltantes.Count > 0)
                    throw new AbrilException("Uno o más revisores no existen o no tienen correo corporativo @abril.pe.", 400);
            }

            // ── Diff con las filas vivas del mismo alcance (área o área+proyecto) ─
            var now = DateTimeOffset.UtcNow;
            var vivos = await ctx.AreaRevisores
                .Where(r => r.State && r.AreaScopeId == areaScopeId && r.ProjectId == projectId)
                .ToListAsync();
            var vivosByRevisor = vivos.ToDictionary(r => r.RevisorId);

            foreach (var d in deseados)
            {
                if (vivosByRevisor.TryGetValue(d.RevisorWorkerId, out var row))
                {
                    if (row.OrdenPrioridad != d.OrdenPrioridad || row.Active != d.Active)
                    {
                        row.OrdenPrioridad = d.OrdenPrioridad;
                        row.Active = d.Active;
                        row.UpdatedAt = now;
                    }
                }
                else
                {
                    ctx.AreaRevisores.Add(new AreaRevisoresModel
                    {
                        AreaScopeId = areaScopeId,
                        ProjectId = projectId,
                        RevisorId = d.RevisorWorkerId,
                        OrdenPrioridad = d.OrdenPrioridad,
                        Active = d.Active,
                        State = true,
                        CreatedAt = now,
                    });
                }
            }

            var deseadosIds = deseados.Select(d => d.RevisorWorkerId).ToHashSet();
            foreach (var row in vivos)
            {
                if (!deseadosIds.Contains(row.RevisorId))
                {
                    row.State = false;
                    row.UpdatedAt = now;
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task SetFiltroProyectoAsync(int areaScopeId, bool filtraPorProyecto)
        {
            using var ctx = _factory.CreateDbContext();

            // El área debe ser un nodo elegible (primero de su tipo en su rama).
            var nodos = await LoadNodosAsync(ctx);
            var area = ConfigurableNodes(nodos).FirstOrDefault(n => n.AreaScopeId == areaScopeId);
            if (area == null)
                throw new AbrilException("El área no existe o no admite configuración (solo áreas de tipo Área de Gerencia o Área Estándar).", 404);

            var now = DateTimeOffset.UtcNow;
            var config = await ctx.GaSalidasAreaConfig
                .FirstOrDefaultAsync(f => f.State && f.AreaScopeId == areaScopeId);

            if (config == null)
            {
                ctx.GaSalidasAreaConfig.Add(new Shared.Models.GaSalidasAreaConfig
                {
                    AreaScopeId = areaScopeId,
                    FiltraPorProyecto = filtraPorProyecto,
                    State = true,
                    Active = true,
                    CreatedAt = now,
                });
            }
            else if (config.FiltraPorProyecto != filtraPorProyecto)
            {
                config.FiltraPorProyecto = filtraPorProyecto;
                config.UpdatedAt = now;
            }

            await ctx.SaveChangesAsync();
        }

        // ── Visibilidad por usuario ─────────────────────────────────────────

        /// <summary>
        /// area_scope_id (de los nodos elegibles) que el usuario puede ver, o null si
        /// no puede ver ninguno. Solo ven su área los trabajadores de las categorías
        /// <see cref="CategoriaIds.ConVistaDeSuArea"/> (Jefe, Coordinador o Gerente): se
        /// parte del workers.area_scope_id del trabajador y se sube el árbol hasta el primer
        /// nodo listado en la pantalla (un gerente colgado directamente de su gerencia
        /// resuelve a esa gerencia, que ahora es un nodo listado).
        /// </summary>
        private static async Task<int?> GetAreaVisibleDelUsuarioAsync(
            AppDbContext ctx, int userId, List<NodoArea> nodos, List<NodoArea> elegibles)
        {
            var areaScopeWorker = await (
                from w in ctx.Worker
                where w.Person != null && w.Person.UserId == userId && w.AreaScopeId != null
                    && w.PuestoCatalogo != null
                    && CategoriaIds.ConVistaDeSuArea.Contains(w.PuestoCatalogo.CategoriaId)
                select w.AreaScopeId
            ).FirstOrDefaultAsync();
            if (areaScopeWorker == null) return null;

            var byId = nodos.ToDictionary(n => n.AreaScopeId);
            var elegiblesIds = elegibles.Select(n => n.AreaScopeId).ToHashSet();
            var visitados = new HashSet<int>();
            int? actual = areaScopeWorker;
            while (actual != null && visitados.Add(actual.Value))
            {
                if (elegiblesIds.Contains(actual.Value)) return actual.Value;
                actual = byId.TryGetValue(actual.Value, out var nodo) ? nodo.AreaScopeParentId : null;
            }
            return null;
        }

        // ── Árbol de áreas ──────────────────────────────────────────────────

        private sealed record NodoArea(int AreaScopeId, int? AreaScopeParentId, string AreaItemName, string AreaTypeName);

        private static async Task<List<NodoArea>> LoadNodosAsync(AppDbContext ctx)
        {
            return await (
                from s in ctx.AreaScope
                join ai in ctx.AreaItem on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State && at.State
                select new NodoArea(s.AreaScopeId, s.AreaScopeParentId, ai.AreaItemName, at.AreaTypeName)
            ).ToListAsync();
        }

        /// <summary>
        /// Nodos que admiten revisores: los de un tipo de <see cref="TiposConfigurables"/>
        /// que son el primero de SU MISMO tipo en su rama, es decir que ningún ancestro
        /// comparte su tipo. Así, de "Gerencia de Proyectos" → "Unidad de Proyectos" →
        /// "Ingeniería BIM" se devuelven la gerencia (única de su tipo en la rama) y
        /// "Unidad de Proyectos" (primera estándar), pero no "Ingeniería BIM", que cuelga
        /// de otra estándar.
        /// </summary>
        private static List<NodoArea> ConfigurableNodes(List<NodoArea> nodos)
        {
            var byId = nodos.ToDictionary(n => n.AreaScopeId);
            return nodos
                .Where(n => TiposConfigurables.Contains(n.AreaTypeName) && !TieneAncestroDelMismoTipo(n, byId))
                .ToList();
        }

        private static bool TieneAncestroDelMismoTipo(NodoArea nodo, Dictionary<int, NodoArea> byId)
        {
            var visitados = new HashSet<int>();
            var parentId = nodo.AreaScopeParentId;
            while (parentId != null && visitados.Add(parentId.Value))
            {
                if (!byId.TryGetValue(parentId.Value, out var parent)) break;
                if (parent.AreaTypeName == nodo.AreaTypeName) return true;
                parentId = parent.AreaScopeParentId;
            }
            return false;
        }
    }
}
