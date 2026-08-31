using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.AreaScope.Interfaces;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Repositories
{
    public class CatalogosHabilitacionRepository : ICatalogosHabilitacionRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IAreaScopeLegacyResolver _legacyResolver;
        private readonly IJefeRevisorResolver _revisorResolver;

        public CatalogosHabilitacionRepository(
            IDbContextFactory<AppDbContext> factory,
            IAreaScopeLegacyResolver legacyResolver,
            IJefeRevisorResolver revisorResolver)
        {
            _factory = factory;
            _legacyResolver = legacyResolver;
            _revisorResolver = revisorResolver;
        }

        /// <summary>
        /// Árbol de áreas para los desplegables del formulario de trabajadores, con la equivalencia
        /// legacy y el revisor ya resueltos por nodo. Una sola petición alimenta toda la cascada y
        /// el campo de revisor, sin ir al servidor cada vez que se cambia de área.
        /// </summary>
        public async Task<List<AreaArbolNodoDto>> GetAreaArbolAsync()
        {
            using var ctx = _factory.CreateDbContext();

            var nodos = await (
                from s in ctx.AreaScope.AsNoTracking()
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                join at in ctx.AreaType.AsNoTracking() on ai.AreaTypeId equals at.AreaTypeId
                where s.State && ai.State
                orderby s.DisplayOrder, ai.AreaItemName
                select new AreaArbolNodoDto
                {
                    AreaScopeId = s.AreaScopeId,
                    AreaScopeParentId = s.AreaScopeParentId,
                    AreaItemName = ai.AreaItemName,
                    AreaTypeName = at.AreaTypeName,
                    DisplayOrder = s.DisplayOrder,
                }
            ).ToListAsync();

            if (nodos.Count == 0) return nodos;

            var ids = nodos.Select(n => n.AreaScopeId).ToList();
            var legacy = await _legacyResolver.ResolveTodosAsync();
            var revisores = await _revisorResolver.ResolveByAreaScopeManyAsync(ids);

            foreach (var nodo in nodos)
            {
                if (legacy.TryGetValue(nodo.AreaScopeId, out var eq))
                {
                    nodo.Area = eq.Area;
                    nodo.Subarea = eq.Subarea;
                    nodo.Jefatura = eq.Jefatura;
                }

                if (!revisores.TryGetValue(nodo.AreaScopeId, out var rev)) continue;

                nodo.Revisores = rev.Area.Select(MapRevisor).ToList();
                nodo.RevisoresPorProyecto = rev.PorProyecto
                    .Select(kv => new AreaArbolRevisorProyectoDto
                    {
                        ProyectoId = kv.Key,
                        Revisores = kv.Value.Select(MapRevisor).ToList(),
                    })
                    .ToList();
            }

            return nodos;
        }

        private static AreaArbolRevisorDto MapRevisor(JefeRevisorResolution r) => new()
        {
            WorkerId = r.WorkerId,
            PersonId = r.PersonId,
            Nombre = r.Nombre,
            Email = r.Email,
        };

        public async Task<List<SsItemTrabajador>> GetItemsTrabajadorAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsItemTrabajador
                .Where(x => x.Activo)
                .OrderBy(x => x.Orden)
                .ToListAsync();
        }

        public async Task<List<SsItemEmpresa>> GetItemsEmpresaAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsItemEmpresa
                .Where(x => x.Activo)
                .OrderBy(x => x.Orden)
                .ToListAsync();
        }

        public async Task<List<SsItemEquipo>> GetItemsEquipoAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsItemEquipo
                .Where(x => x.Activo)
                .OrderBy(x => x.Orden)
                .ToListAsync();
        }

        public async Task<List<SsItemEquipo>> GetItemsEquipoTodosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsItemEquipo
                .Include(x => x.TipoEquipo)
                .OrderBy(x => x.TipoEquipoId == null ? 0 : 1) // genéricos primero
                .ThenBy(x => x.TipoEquipo != null ? x.TipoEquipo.Nombre : null)
                .ThenBy(x => x.Orden)
                .ToListAsync();
        }

        public async Task<SsItemEquipo> CrearItemEquipoAsync(string nombre, bool requiereVigencia, int? tipoEquipoId)
        {
            using var ctx = _factory.CreateDbContext();
            var nombreNorm = nombre.Trim();
            if (await ctx.SsItemEquipo.AnyAsync(x => x.Nombre == nombreNorm && x.TipoEquipoId == tipoEquipoId))
                throw new AbrilException("Ya existe un ítem con ese nombre para ese tipo de equipo.", 400);

            var maxOrden = await ctx.SsItemEquipo.MaxAsync(x => (int?)x.Orden) ?? 0;
            var item = new SsItemEquipo
            {
                Nombre = nombreNorm,
                RequiereVigencia = requiereVigencia,
                TipoEquipoId = tipoEquipoId,
                Orden = maxOrden + 1,
                Activo = true,
            };
            ctx.SsItemEquipo.Add(item);
            await ctx.SaveChangesAsync();
            return item;
        }

        public async Task<SsItemEquipo> ActualizarItemEquipoAsync(int id, string nombre, bool requiereVigencia, int? tipoEquipoId)
        {
            using var ctx = _factory.CreateDbContext();
            var item = await ctx.SsItemEquipo.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Ítem de equipo no encontrado.", 404);

            var nombreNorm = nombre.Trim();
            if (await ctx.SsItemEquipo.AnyAsync(x => x.Nombre == nombreNorm && x.TipoEquipoId == tipoEquipoId && x.Id != id))
                throw new AbrilException("Ya existe un ítem con ese nombre para ese tipo de equipo.", 400);

            item.Nombre = nombreNorm;
            item.RequiereVigencia = requiereVigencia;
            item.TipoEquipoId = tipoEquipoId;
            await ctx.SaveChangesAsync();
            return item;
        }

        public async Task ToggleItemEquipoAsync(int id, bool activo)
        {
            using var ctx = _factory.CreateDbContext();
            var item = await ctx.SsItemEquipo.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Ítem de equipo no encontrado.", 404);
            item.Activo = activo;
            await ctx.SaveChangesAsync();
        }

        public async Task<List<SsCriterioEvaluacion>> GetCriteriosEvaluacionAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsCriterioEvaluacion
                .Where(x => x.Activo)
                .OrderBy(x => x.Orden)
                .ToListAsync();
        }

        public async Task<List<ObraOficinaStaffDto>> GetObraOficinaStaffAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.WorkersObraOficinaStaff
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.DisplayOrder)
                .Select(x => new ObraOficinaStaffDto
                {
                    ObraOficinaStaffId = x.WorkersObraOficinaStaffId,
                    Name = x.Name
                })
                .ToListAsync();
        }

        public async Task<List<string>> GetAreasAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.CatSubarea
                .Where(x => x.Activo)
                .Select(x => x.Area)
                .Distinct()
                .OrderBy(a => a)
                .ToListAsync();
        }

        public async Task<List<CatSubarea>> GetSubareasAsync(string? area)
        {
            using var ctx = _factory.CreateDbContext();
            var query = ctx.CatSubarea.Where(x => x.Activo);
            if (!string.IsNullOrWhiteSpace(area))
                query = query.Where(x => x.Area == area);
            return await query
                .OrderBy(x => x.Subarea)
                .ToListAsync();
        }

        public async Task<List<Categoria>> GetCategoriasAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Categoria
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<List<Puesto>> GetPuestosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Puesto
                .Where(x => x.State && x.Active)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }

        // ── Categorías CRUD ──────────────────────────────────────────
        public async Task<List<Categoria>> GetCategoriasTodasAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Categoria
                .Where(x => x.State)
                .OrderBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<Categoria> CrearCategoriaAsync(string nombre)
        {
            using var ctx = _factory.CreateDbContext();
            var nombreNorm = NormalizarNombre(nombre);
            if (await ctx.Categoria.AnyAsync(x => x.State && x.Nombre == nombreNorm))
                throw new AbrilException("Ya existe una categoría con ese nombre.", 400);

            var maxOrden = await ctx.Categoria.MaxAsync(x => (int?)x.Orden) ?? 0;
            var cat = new Categoria { Nombre = nombreNorm, Orden = maxOrden + 1, CreatedDateTime = DateTime.UtcNow };
            ctx.Categoria.Add(cat);
            await ctx.SaveChangesAsync();
            return cat;
        }

        public async Task<Categoria> ActualizarCategoriaAsync(int id, string nombre)
        {
            using var ctx = _factory.CreateDbContext();
            var cat = await ctx.Categoria.FirstOrDefaultAsync(x => x.CategoriaId == id && x.State)
                ?? throw new AbrilException("Categoría no encontrada.", 404);

            var nombreNorm = NormalizarNombre(nombre);
            if (await ctx.Categoria.AnyAsync(x => x.State && x.Nombre == nombreNorm && x.CategoriaId != id))
                throw new AbrilException("Ya existe una categoría con ese nombre.", 400);

            cat.Nombre = nombreNorm;
            cat.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
            return cat;
        }

        public async Task ToggleCategoriaAsync(int id, bool activo)
        {
            using var ctx = _factory.CreateDbContext();
            var cat = await ctx.Categoria.FirstOrDefaultAsync(x => x.CategoriaId == id && x.State)
                ?? throw new AbrilException("Categoría no encontrada.", 404);
            cat.Active = activo;
            cat.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        // ── Tipos de equipo CRUD ─────────────────────────────────────

        public async Task<List<SsTipoEquipo>> GetTiposEquipoAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsTipoEquipo
                .Where(x => x.Activo)
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<List<SsTipoEquipo>> GetTiposEquipoTodosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsTipoEquipo
                .OrderBy(x => x.Orden)
                .ThenBy(x => x.Nombre)
                .ToListAsync();
        }

        public async Task<SsTipoEquipo> CrearTipoEquipoAsync(string nombre)
        {
            using var ctx = _factory.CreateDbContext();
            var nombreNorm = nombre.Trim();
            if (await ctx.SsTipoEquipo.AnyAsync(x => x.Nombre == nombreNorm))
                throw new AbrilException("Ya existe un tipo de equipo con ese nombre.", 400);

            var maxOrden = await ctx.SsTipoEquipo.MaxAsync(x => (int?)x.Orden) ?? 0;
            var tipo = new SsTipoEquipo
            {
                Nombre = nombreNorm,
                Orden = maxOrden + 1,
                Activo = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            ctx.SsTipoEquipo.Add(tipo);
            await ctx.SaveChangesAsync();
            return tipo;
        }

        public async Task<SsTipoEquipo> ActualizarTipoEquipoAsync(int id, string nombre)
        {
            using var ctx = _factory.CreateDbContext();
            var tipo = await ctx.SsTipoEquipo.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Tipo de equipo no encontrado.", 404);

            var nombreNorm = nombre.Trim();
            if (await ctx.SsTipoEquipo.AnyAsync(x => x.Nombre == nombreNorm && x.Id != id))
                throw new AbrilException("Ya existe un tipo de equipo con ese nombre.", 400);

            tipo.Nombre = nombreNorm;
            tipo.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
            return tipo;
        }

        public async Task ToggleTipoEquipoAsync(int id, bool activo)
        {
            using var ctx = _factory.CreateDbContext();
            var tipo = await ctx.SsTipoEquipo.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new AbrilException("Tipo de equipo no encontrado.", 404);
            tipo.Activo = activo;
            tipo.UpdatedAt = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        // ── Puestos CRUD ─────────────────────────────────────────────

        /// <summary>
        /// Puestos vivos con su categoría y el uso real (fichas de <c>workers</c> que
        /// apuntan al puesto) resueltos en una sola consulta: el conteo va como subconsulta
        /// correlacionada en vez de un segundo viaje a la base de datos.
        /// El uso se cuenta sobre TODAS las fichas del trabajador, sin filtrar por estado:
        /// <c>workers</c> no tiene soft delete, así que cada fila es un uso real del puesto.
        /// </summary>
        public async Task<List<PuestoAdminDto>> GetPuestosTodosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Puesto
                .AsNoTracking()
                .Where(x => x.State)
                .OrderBy(x => x.Nombre)
                .Select(x => new PuestoAdminDto
                {
                    Id = x.PuestoId,
                    Nombre = x.Nombre,
                    CategoriaId = x.CategoriaId,
                    CategoriaNombre = x.Categoria == null ? null : x.Categoria.Nombre,
                    // Las dos áreas salen de la propia fila del puesto, así que entran en la
                    // misma consulta: antes vivían en la intermedia `puesto_area_scope` y
                    // costaban un segundo viaje para agrupar los vínculos en memoria.
                    AreaSolicitanteScopeId = x.AreaSolicitanteScope != null && x.AreaSolicitanteScope.State
                                                 ? x.AreaSolicitanteScopeId
                                                 : null,
                    AreaSolicitanteNombre = x.AreaSolicitanteScope != null && x.AreaSolicitanteScope.State
                                            && x.AreaSolicitanteScope.AreaItem != null
                                            && x.AreaSolicitanteScope.AreaItem.State
                                                ? x.AreaSolicitanteScope.AreaItem.AreaItemName
                                                : null,
                    AreaDestinoScopeId = x.AreaDestinoScope != null && x.AreaDestinoScope.State
                                             ? x.AreaDestinoScopeId
                                             : null,
                    AreaDestinoNombre = x.AreaDestinoScope != null && x.AreaDestinoScope.State
                                        && x.AreaDestinoScope.AreaItem != null
                                        && x.AreaDestinoScope.AreaItem.State
                                            ? x.AreaDestinoScope.AreaItem.AreaItemName
                                            : null,
                    Orden = x.Orden,
                    Activo = x.Active,
                    CantidadTrabajadores = ctx.Worker.Count(w => w.PuestoId == x.PuestoId)
                })
                .ToListAsync();
        }

        public async Task<List<PuestoAreaNodoDto>> GetAreaTreePuestosAsync()
        {
            using var ctx = _factory.CreateDbContext();
            return await (
                from s in ctx.AreaScope.AsNoTracking()
                join ai in ctx.AreaItem.AsNoTracking() on s.AreaItemId equals ai.AreaItemId
                where s.State && ai.State
                orderby s.DisplayOrder, ai.AreaItemName
                select new PuestoAreaNodoDto
                {
                    AreaScopeId = s.AreaScopeId,
                    AreaScopeParentId = s.AreaScopeParentId,
                    AreaItemName = ai.AreaItemName,
                    DisplayOrder = s.DisplayOrder,
                }
            ).ToListAsync();
        }

        /// <summary>
        /// Fichas de <c>workers</c> que apuntan al puesto, para el detalle que abre la fila
        /// de la tabla. Se listan con el mismo criterio con el que
        /// <see cref="GetPuestosTodosAsync"/> las cuenta (todas las fichas, sin filtrar por
        /// estado): así la lista siempre cuadra con el número que muestra la fila.
        ///
        /// El nombre se lee de <c>person.full_name</c>; se cae a <c>workers.apellido_nombre</c>
        /// solo si la ficha no tiene persona (la FK es nullable) o la persona no lo tiene.
        /// El join va por la navegación (LEFT JOIN) justamente para no perder esas fichas y
        /// desalinear el conteo.
        /// </summary>
        public async Task<List<PuestoTrabajadorDto>> GetTrabajadoresPorPuestoAsync(int puestoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Worker
                .AsNoTracking()
                .Where(w => w.PuestoId == puestoId)
                .OrderBy(w => w.Person!.FullName ?? w.ApellidoNombre)
                .ThenBy(w => w.Id)
                .Select(w => new PuestoTrabajadorDto
                {
                    WorkerId = w.Id,
                    NombreCompleto = (w.Person!.FullName ?? w.ApellidoNombre) ?? "",
                    EmailCorporativo = w.EmailCorporativo
                })
                .ToListAsync();
        }

        public async Task<Puesto> CrearPuestoAsync(
            string nombre, int categoriaId, int? areaSolicitanteScopeId, int? areaDestinoScopeId)
        {
            using var ctx = _factory.CreateDbContext();
            var nombreNorm = NormalizarNombre(nombre);
            await ValidarCategoriaAsync(ctx, categoriaId);
            var solicitante = await ValidarAreaAsync(ctx, areaSolicitanteScopeId, "que puede pedir el puesto");
            var destino = await ValidarAreaAsync(ctx, areaDestinoScopeId, "de destino");
            await ValidarNombreLibreAsync(ctx, nombreNorm, solicitante, null);

            var maxOrden = await ctx.Puesto.MaxAsync(x => (int?)x.Orden) ?? 0;
            var puesto = new Puesto
            {
                Nombre = nombreNorm,
                CategoriaId = categoriaId,
                AreaSolicitanteScopeId = solicitante,
                AreaDestinoScopeId = destino,
                Orden = maxOrden + 1,
                CreatedDateTime = DateTime.UtcNow
            };
            ctx.Puesto.Add(puesto);
            await ctx.SaveChangesAsync();

            return puesto;
        }

        public async Task<Puesto> ActualizarPuestoAsync(
            int id, string nombre, int categoriaId, int? areaSolicitanteScopeId, int? areaDestinoScopeId)
        {
            using var ctx = _factory.CreateDbContext();
            var puesto = await ctx.Puesto.FirstOrDefaultAsync(x => x.PuestoId == id && x.State)
                ?? throw new AbrilException("Puesto no encontrado.", 404);

            var nombreNorm = NormalizarNombre(nombre);
            await ValidarCategoriaAsync(ctx, categoriaId);
            var solicitante = await ValidarAreaAsync(ctx, areaSolicitanteScopeId, "que puede pedir el puesto");
            var destino = await ValidarAreaAsync(ctx, areaDestinoScopeId, "de destino");
            await ValidarNombreLibreAsync(ctx, nombreNorm, solicitante, id);

            puesto.Nombre = nombreNorm;
            puesto.CategoriaId = categoriaId;
            puesto.AreaSolicitanteScopeId = solicitante;
            puesto.AreaDestinoScopeId = destino;
            puesto.UpdatedDateTime = DateTime.UtcNow;

            await ctx.SaveChangesAsync();
            return puesto;
        }

        /// <summary>
        /// El nombre del puesto es único DENTRO del área que puede pedirlo, no en todo el
        /// catálogo: un mismo cargo que existe en dos áreas son dos puestos con el mismo
        /// nombre (CHOFER en Logística y CHOFER en Gerencia General).
        ///
        /// El área de DESTINO no entra en la regla: dos puestos distintos pueden mandar a la
        /// misma área (INGENIERO y ASISTENTE DE PRODUCCIÓN van los dos a Producción), y lo que
        /// desambigua un nombre repetido es quién lo pide, que es lo que ve el solicitante.
        ///
        /// Los puestos sin área solicitante cuentan como un área más para esto: el índice
        /// <c>ux_puesto_nombre_area_solicitante_vivo</c> es <c>NULLS NOT DISTINCT</c>, así que
        /// tampoco deja dos «ALMACENERO» sueltos. Sin el <c>== null</c> explícito acá, la
        /// comparación en SQL sería <c>NULL = NULL</c> y la validación dejaría pasar justo ese
        /// caso, para que después reventara el índice con un 23505 ilegible.
        /// </summary>
        private static async Task ValidarNombreLibreAsync(AppDbContext ctx, string nombreNorm, int? areaSolicitanteScopeId, int? puestoIdActual)
        {
            var repetido = await ctx.Puesto.AnyAsync(x => x.State
                                                       && x.Nombre == nombreNorm
                                                       && (areaSolicitanteScopeId == null
                                                               ? x.AreaSolicitanteScopeId == null
                                                               : x.AreaSolicitanteScopeId == areaSolicitanteScopeId)
                                                       && (puestoIdActual == null || x.PuestoId != puestoIdActual));
            if (repetido)
                throw new AbrilException(
                    areaSolicitanteScopeId == null
                        ? "Ya existe un puesto con ese nombre sin área que lo pida."
                        : "Ya existe un puesto con ese nombre en esa área.",
                    400);
        }

        /// <summary>
        /// Valida que un área del formulario exista viva en el árbol. Null es válido en las
        /// dos: los puestos de obra no tienen ninguna, y sin destino el finalista se cae al
        /// área del solicitante.
        /// </summary>
        private static async Task<int?> ValidarAreaAsync(AppDbContext ctx, int? areaScopeId, string cual)
        {
            if (areaScopeId is not > 0) return null;

            var existe = await ctx.AreaScope.AnyAsync(s => s.AreaScopeId == areaScopeId && s.State);
            if (!existe)
                throw new AbrilException($"El área {cual} no existe.", 400);

            return areaScopeId;
        }

        public async Task TogglePuestoAsync(int id, bool activo)
        {
            using var ctx = _factory.CreateDbContext();
            var puesto = await ctx.Puesto.FirstOrDefaultAsync(x => x.PuestoId == id && x.State)
                ?? throw new AbrilException("Puesto no encontrado.", 404);
            puesto.Active = activo;
            puesto.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Soft delete del puesto: se marca <c>state = false</c> (y se desactiva, para que
        /// desaparezca de los desplegables aunque una consulta filtre solo por <c>active</c>).
        /// La fila se conserva para el histórico y el índice único de nombre solo aplica a
        /// los vivos, así que el nombre queda libre para volver a usarse.
        ///
        /// Un puesto en uso no se puede eliminar: las fichas que lo apuntan seguirían
        /// mostrándolo pero ya no existiría en el desplegable del formulario de trabajadores,
        /// y la siguiente edición de esas fichas lo perdería. Para sacarlo de circulación sin
        /// romper nada está "Desactivar".
        /// </summary>
        public async Task EliminarPuestoAsync(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var puesto = await ctx.Puesto.FirstOrDefaultAsync(x => x.PuestoId == id && x.State)
                ?? throw new AbrilException("Puesto no encontrado.", 404);

            var enUso = await ctx.Worker.CountAsync(w => w.PuestoId == id);
            if (enUso > 0)
                throw new AbrilException(
                    $"No se puede eliminar: {enUso} trabajador(es) usan este puesto. " +
                    "Si solo quieres que deje de aparecer en los desplegables, desactívalo.", 400);

            // El área no se limpia: vive en la propia fila y se va de baja con ella. Se
            // conserva para que el histórico diga de qué área era el puesto que se eliminó.
            puesto.State = false;
            puesto.Active = false;
            puesto.UpdatedDateTime = DateTime.UtcNow;
            await ctx.SaveChangesAsync();
        }

        /// <summary>
        /// Soft delete en bloque de la selección de la tabla, en tres viajes a la base de
        /// datos para todo el lote (traer los puestos, ver cuáles están en uso, guardar).
        ///
        /// A diferencia del borrado de una sola fila, acá los puestos en uso se omiten en
        /// vez de tumbar el lote entero: la pantalla ya filtró la selección con el conteo
        /// que trajo, así que un puesto en uso solo puede aparecer si alguien lo asignó
        /// mientras tanto — y en ese caso lo correcto es eliminar el resto e informarlo.
        /// </summary>
        public async Task<PuestosEliminarResultDto> EliminarPuestosAsync(IReadOnlyCollection<int> ids)
        {
            if (ids.Count == 0)
                throw new AbrilException("No se recibió ningún puesto para eliminar.", 400);

            using var ctx = _factory.CreateDbContext();
            var puestos = await ctx.Puesto
                .Where(x => ids.Contains(x.PuestoId) && x.State)
                .ToListAsync();
            if (puestos.Count == 0)
                throw new AbrilException("Los puestos seleccionados ya no existen.", 404);

            var enUso = await ctx.Worker
                .Where(w => w.PuestoId != null && ids.Contains(w.PuestoId.Value))
                .Select(w => w.PuestoId!.Value)
                .Distinct()
                .ToListAsync();

            // El área ya no se da de baja aparte: vive en la propia fila del puesto, así que
            // se va con él. Todas las consultas que la leen filtran por `puesto.state`.
            var now = DateTime.UtcNow;
            var eliminados = 0;
            foreach (var puesto in puestos)
            {
                if (enUso.Contains(puesto.PuestoId)) continue;
                puesto.State = false;
                puesto.Active = false;
                puesto.UpdatedDateTime = now;
                eliminados++;
            }

            if (eliminados > 0) await ctx.SaveChangesAsync();

            return new PuestosEliminarResultDto
            {
                Eliminados = eliminados,
                Omitidos = ids.Count - eliminados
            };
        }

        /// <summary>Categorías y puestos se guardan siempre en MAYÚSCULAS.</summary>
        private static string NormalizarNombre(string nombre) =>
            nombre.Trim().ToUpperInvariant();

        /// <summary>
        /// Un puesto sin categoría dejaría a sus trabajadores sin categoría — o sea fuera de
        /// todo filtro y de toda regla —, así que la categoría es obligatoria y tiene que
        /// existir viva en el catálogo.
        /// </summary>
        private static async Task ValidarCategoriaAsync(AppDbContext ctx, int categoriaId)
        {
            if (!await ctx.Categoria.AnyAsync(c => c.CategoriaId == categoriaId && c.State))
                throw new AbrilException("La categoría indicada no existe.", 400);
        }
    }
}
