using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Shared.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Infrastructure.Repositories
{
    /// <inheritdoc cref="IReclutadoresRepository"/>
    public class ReclutadoresRepository : IReclutadoresRepository
    {
        /// <summary>
        /// Nodo "Gestión del Talento Humano" del árbol de áreas: de ahí sale, sola, la lista de
        /// candidatos a reclutador. Se usa el id porque el árbol es administrable por UI y
        /// renombrar el nodo no puede apagar la pantalla (mismo criterio que el resto del módulo,
        /// que ya resuelve el correo del área de GTH con esta constante).
        /// </summary>
        private const int AreaGth = AreaScopeIds.GestionDelTalentoHumano;

        private readonly IDbContextFactory<AppDbContext> _factory;

        public ReclutadoresRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<List<ReclutadorDto>> GetReclutadoresAsync()
        {
            using var ctx = _factory.CreateDbContext();

            // Una sola consulta: los trabajadores del área de GTH (los candidatos naturales) más
            // los que ya tienen fila viva en la tabla filtro aunque hoy estén fuera del área o
            // retirados — si no aparecieran, un reclutador prendido que se mudó de área quedaría
            // saliendo en el desplegable sin manera de apagarlo desde la pantalla.
            var filas = await (
                from w in ctx.Worker.AsNoTracking()
                join rp in ctx.GthResponsableProceso.Where(r => r.State)
                    on w.Id equals rp.WorkerId into rpJoin
                from rp in rpJoin.DefaultIfEmpty()
                // El área se trae para explicar las filas de fuera del equipo (y solo para eso).
                join s in ctx.AreaScope on w.AreaScopeId equals (int?)s.AreaScopeId into areaJoin
                from s in areaJoin.DefaultIfEmpty()
                where rp != null
                   || (w.AreaScopeId == AreaGth
                       && (w.WorkersEstadoId == WorkersEstadoIds.Activo
                        || w.WorkersEstadoId == WorkersEstadoIds.InhabilitadoSsoma))
                select new
                {
                    w.Id,
                    w.PersonId,
                    w.AreaScopeId,
                    w.WorkersEstadoId,
                    Nombre = w.Person!.FullName ?? w.ApellidoNombre ?? "",
                    Puesto = w.PuestoCatalogo != null ? w.PuestoCatalogo.Nombre : null,
                    Activo = rp != null && rp.Active,
                    Area   = s != null && s.AreaItem != null ? s.AreaItem.AreaItemName : null,
                })
                .ToListAsync();

            // Una persona puede tener más de una ficha en workers (reingreso). Si dos siguen
            // vivas, la pantalla mostraría a la misma persona dos veces con interruptores
            // distintos: se queda la ficha más reciente, salvo que sea la vieja la que tiene el
            // interruptor prendido (esa es la que hay que poder apagar).
            return filas
                .GroupBy(f => f.PersonId.HasValue ? $"P{f.PersonId}" : $"W{f.Id}")
                .Select(g => g.OrderByDescending(f => f.Activo).ThenByDescending(f => f.Id).First())
                .Select(f => new ReclutadorDto
                {
                    WorkerId       = f.Id,
                    Nombre         = f.Nombre,
                    Puesto         = f.Puesto,
                    Activo         = f.Activo,
                    FueraDelEquipo = f.AreaScopeId != AreaGth
                                     || !WorkersEstadoIds.EstanAdentro.Contains(f.WorkersEstadoId),
                    Area           = f.Area,
                })
                .OrderBy(f => f.Nombre, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public async Task<ReclutadorToggleResultDto> ToggleAsync(int workerId, bool activo, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var worker = await ctx.Worker.AsNoTracking()
                .Where(w => w.Id == workerId)
                .Select(w => new
                {
                    w.Id,
                    w.AreaScopeId,
                    w.WorkersEstadoId,
                    Nombre = w.Person!.FullName ?? w.ApellidoNombre,
                })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException("No encontramos la ficha de ese trabajador.", 404);

            var fila = await ctx.GthResponsableProceso
                .FirstOrDefaultAsync(r => r.WorkerId == workerId && r.State);

            if (fila == null)
            {
                // Sin fila ya está apagado: no se crea una solo para dejarla en false.
                if (!activo)
                    return new ReclutadorToggleResultDto
                    {
                        WorkerId = workerId,
                        Activo   = false,
                        Message  = "El reclutador ya estaba desactivado.",
                    };

                // Alta perezosa: la fila recién nace cuando a alguien lo prenden por primera vez.
                // Solo se permite sobre gente del equipo de GTH — a los de fuera únicamente se
                // les puede apagar la fila que ya tienen.
                var esDelEquipo = worker.AreaScopeId == AreaGth
                               && WorkersEstadoIds.EstanAdentro.Contains(worker.WorkersEstadoId);
                if (!esDelEquipo)
                    throw new AbrilException(
                        "Solo se puede activar como reclutador a un trabajador vigente del área de Gestión del Talento Humano.", 400);

                ctx.GthResponsableProceso.Add(new GthResponsableProceso
                {
                    WorkerId        = workerId,
                    Orden           = 0,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId   = userId,
                    Active          = true,
                    State           = true,
                });
            }
            else
            {
                if (fila.Active == activo)
                    return new ReclutadorToggleResultDto
                    {
                        WorkerId = workerId,
                        Activo   = activo,
                        Message  = activo
                            ? "El reclutador ya estaba activo."
                            : "El reclutador ya estaba desactivado.",
                    };

                fila.Active            = activo;
                fila.UpdatedDateTime   = DateTimeOffset.UtcNow;
                fila.UpdatedUserId     = userId;
            }

            await ctx.SaveChangesAsync();

            var nombre = string.IsNullOrWhiteSpace(worker.Nombre) ? "El trabajador" : worker.Nombre;
            return new ReclutadorToggleResultDto
            {
                WorkerId = workerId,
                Activo   = activo,
                Message  = activo
                    ? $"{nombre} ya sale en el desplegable de responsable del proceso."
                    : $"{nombre} dejó de salir en el desplegable. Los procesos que ya tiene asignados no cambian.",
            };
        }
    }
}
