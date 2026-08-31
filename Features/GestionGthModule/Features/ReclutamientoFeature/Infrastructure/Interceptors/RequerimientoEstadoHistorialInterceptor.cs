using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Security.Claims;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interceptors
{
    /// <summary>
    /// Escribe la bitácora de fases del requerimiento (<see cref="GthRequerimientoEstadoHistorial"/>):
    /// cada vez que un <see cref="GthRequerimiento"/> nace o cambia de
    /// <c>gth_estado_requerimiento_id</c>, deja una fila con la fase de la que salió, a la que
    /// entró, quién lo movió y cuándo.
    ///
    /// Va como interceptor y no como una llamada en cada repositorio a propósito: el estado se
    /// mueve desde una docena de sitios repartidos en cuatro archivos —la decisión de Gerencia
    /// General, la del gerente del área, la de GTH, la publicación, la revisión de CV, el envío y
    /// la decisión de la long list, las entrevistas, el envío del finalista, su aprobación, el
    /// retomar a un rechazado, los dos saltos del flujo FFT y el resultado del EMO de ingreso— y
    /// el pipeline sigue creciendo. Acá no hay nada que recordar: el que agregue la fase número
    /// diez la ve registrada sin escribir una línea.
    ///
    /// Corre dentro del mismo <c>SaveChanges</c> (y de la misma transacción) que el cambio, así
    /// que o quedan los dos o no queda ninguno.
    /// </summary>
    public class RequerimientoEstadoHistorialInterceptor : SaveChangesInterceptor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequerimientoEstadoHistorialInterceptor(IHttpContextAccessor httpContextAccessor)
            => _httpContextAccessor = httpContextAccessor;

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null)
                Registrar(eventData.Context);

            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        /// <summary>El mismo trabajo para el <c>SaveChanges</c> síncrono.</summary>
        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData, InterceptionResult<int> result)
        {
            if (eventData.Context is not null)
                Registrar(eventData.Context);

            return base.SavingChanges(eventData, result);
        }

        private void Registrar(DbContext ctx)
        {
            var requerimientos = ctx.ChangeTracker.Entries<GthRequerimiento>()
                .Where(e => e.State is EntityState.Added or EntityState.Modified)
                .ToList();
            if (requerimientos.Count == 0) return;

            // Lo que este mismo SaveChanges ya iba a registrar. La execution strategy reintenta el
            // bloque cuando la conexión falla, y sin este guardia el segundo intento dejaría la
            // transición grabada dos veces sobre el contexto que sobrevivió al primero.
            var pendientes = ctx.ChangeTracker.Entries<GthRequerimientoEstadoHistorial>()
                .Where(e => e.State == EntityState.Added)
                .Select(e => e.Entity)
                .ToList();

            var ahora     = DateTimeOffset.UtcNow;
            var userSesion = UserIdDeLaSesion();

            foreach (var entry in requerimientos)
            {
                var req      = entry.Entity;
                var estadoId = entry.Property(r => r.GthEstadoRequerimientoId);

                int? estadoAnterior;
                DateTimeOffset? cuando;
                int? quien;

                if (entry.State == EntityState.Added)
                {
                    // Alta de la vacante: no viene de ninguna fase, así que la primera fila marca
                    // en qué fase arrancó. Sin ella el primer tramo del proceso no se puede medir.
                    estadoAnterior = null;
                    cuando         = req.CreatedDateTime;
                    quien          = req.CreatedUserId;
                }
                else
                {
                    // Un update que no toca la fase (la prioridad, el responsable, la razón social)
                    // no es una transición. `IsModified` sola no alcanza: EF marca la propiedad como
                    // modificada aunque se le reasigne el mismo valor.
                    if (!estadoId.IsModified) continue;
                    if (Equals(estadoId.OriginalValue, estadoId.CurrentValue)) continue;

                    estadoAnterior = estadoId.OriginalValue;

                    // Solo si este mismo guardado las escribió. Un `updated_date_time` que quedó de
                    // un update anterior fecharía la transición en el pasado, y eso es peor que no
                    // tenerlo: los tiempos por fase saldrían mal sin que nada avise.
                    cuando = entry.Property(r => r.UpdatedDateTime).IsModified ? req.UpdatedDateTime : null;
                    quien  = entry.Property(r => r.UpdatedUserId).IsModified   ? req.UpdatedUserId   : null;
                }

                var estadoNuevo = estadoId.CurrentValue;

                if (pendientes.Any(h => MismoRequerimiento(h, req)
                                     && h.GthEstadoRequerimientoId == estadoNuevo))
                    continue;

                var fila = new GthRequerimientoEstadoHistorial
                {
                    // En el alta la vacante todavía no tiene id: la navegación es el único camino
                    // para que EF lo propague al insertar. Cuando ya lo tiene se escribe la FK
                    // directo, que es lo mismo con menos indirección.
                    GthRequerimientoId       = req.GthRequerimientoId,
                    Requerimiento            = req.GthRequerimientoId == 0 ? req : null,
                    EstadoAnteriorId         = estadoAnterior,
                    GthEstadoRequerimientoId = estadoNuevo,
                    CambioDateTime           = cuando ?? ahora,
                    // El usuario que el propio flujo se anotó. El de la sesión es el respaldo para
                    // que un cambio nuevo que se olvide de escribirlo no pierda el dato.
                    CambioUserId             = quien ?? userSesion,
                    Reconstruido             = false,
                    CreatedDateTime          = ahora,
                    Active                   = true,
                    State                    = true,
                };

                ctx.Add(fila);
                pendientes.Add(fila);
            }
        }

        /// <summary>
        /// Si una fila ya pendiente es de este requerimiento. Por id cuando lo tiene y por
        /// referencia cuando todavía no (la vacante que se está creando en este mismo guardado).
        /// </summary>
        private static bool MismoRequerimiento(GthRequerimientoEstadoHistorial fila, GthRequerimiento req)
            => req.GthRequerimientoId != 0
                ? fila.GthRequerimientoId == req.GthRequerimientoId
                : ReferenceEquals(fila.Requerimiento, req);

        private int? UserIdDeLaSesion()
        {
            var claim = _httpContextAccessor.HttpContext?.User
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(claim, out var id) ? id : null;
        }
    }
}
