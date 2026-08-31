using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Revisores.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.Revisores.Services
{
    /// <summary>
    /// Jefe personalizado por trabajador sobre <c>workers_revisores</c>. El formulario de
    /// trabajadores guarda como máximo uno, así que <see cref="SetAsync"/> deja la tabla con
    /// exactamente esa fila viva (prioridad 1) y da de baja el resto — incluidas las varias
    /// que pudo dejar la antigua pantalla "Revisores de Trabajadores".
    ///
    /// Los filtros de <see cref="GetAsync"/> son los mismos que aplica
    /// <see cref="JefeRevisorResolver"/> al elegir a quién notificar (viva, activa, con correo
    /// corporativo y distinta del propio trabajador): así el checkbox del formulario muestra
    /// al jefe que de verdad va a recibir los correos, y no uno que el resolver descartaría.
    /// </summary>
    public class JefePersonalizadoService : IJefePersonalizadoService
    {
        private const string EmailDomainCorp = "@abril.pe";

        private readonly IDbContextFactory<AppDbContext> _factory;

        public JefePersonalizadoService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<JefePersonalizadoDto?> GetAsync(int workerId)
        {
            if (workerId <= 0) return null;

            using var ctx = _factory.CreateDbContext();

            return await (
                from r in ctx.WorkersRevisores.AsNoTracking()
                where r.State && r.Active && r.SolicitanteId == workerId && r.RevisorId != workerId
                join w in ctx.Worker.AsNoTracking() on r.RevisorId equals w.Id
                where w.EmailCorporativo != null
                      && w.EmailCorporativo.Trim().ToLower().EndsWith(EmailDomainCorp)
                      // Nadie es su propio jefe, ni a través de otra ficha suya: se descarta el
                      // revisor cuya persona es la del solicitante (subconsulta para no gastar
                      // un segundo viaje a la base de datos solo por leer su person_id).
                      && !ctx.Worker.Any(s => s.Id == workerId
                                              && s.PersonId != null
                                              && s.PersonId == w.PersonId)
                orderby r.OrdenPrioridad, r.WorkersRevisoresId
                select new JefePersonalizadoDto
                {
                    WorkerId = w.Id,
                    FullName = w.Person != null ? w.Person.FullName : null,
                    Email    = w.EmailCorporativo,
                }
            ).FirstOrDefaultAsync();
        }

        public async Task SetAsync(int workerId, int? revisorWorkerId)
        {
            if (workerId <= 0) return;

            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            var vivos = await ctx.WorkersRevisores
                .Where(r => r.State && r.SolicitanteId == workerId)
                .ToListAsync();

            var revisorId = revisorWorkerId is > 0 ? revisorWorkerId.Value : (int?)null;

            // Sin jefe personalizado: se dan de baja todas y el trabajador vuelve a depender
            // del revisor de su área.
            if (revisorId is null)
            {
                if (vivos.Count == 0) return;
                foreach (var r in vivos)
                {
                    r.State = false;
                    r.UpdatedAt = now;
                }
                await ctx.SaveChangesAsync();
                return;
            }

            if (revisorId == workerId)
                throw new AbrilException("Un trabajador no puede ser su propio jefe.", 400);

            // Las dos fichas en un solo viaje: se valida el correo del revisor y, con las personas,
            // que no sean la misma (un reingreso deja varias fichas para la misma persona, así que
            // comparar solo los ids de ficha permitiría asignarse a uno mismo por la puerta de atrás).
            var fichas = await ctx.Worker.AsNoTracking()
                .Where(w => w.Id == workerId || w.Id == revisorId)
                .Select(w => new { w.Id, w.PersonId, w.EmailCorporativo })
                .ToListAsync();

            var revisor = fichas.FirstOrDefault(f => f.Id == revisorId);
            var esCorreoCorporativo = revisor?.EmailCorporativo?.Trim()
                .ToLowerInvariant().EndsWith(EmailDomainCorp) == true;
            if (!esCorreoCorporativo)
                throw new AbrilException(
                    "El jefe seleccionado no existe o no tiene correo corporativo @abril.pe.", 400);

            var personaSolicitante = fichas.FirstOrDefault(f => f.Id == workerId)?.PersonId;
            if (personaSolicitante != null && revisor!.PersonId == personaSolicitante)
                throw new AbrilException("Un trabajador no puede ser su propio jefe.", 400);

            foreach (var r in vivos.Where(r => r.RevisorId != revisorId))
            {
                r.State = false;
                r.UpdatedAt = now;
            }

            var elegido = vivos.FirstOrDefault(r => r.RevisorId == revisorId);
            if (elegido is null)
            {
                // Si hubo una asignación anterior al mismo jefe quedó con state = false, y el
                // índice único solo cubre las vivas: se inserta una fila nueva y la vieja queda
                // como historial.
                ctx.WorkersRevisores.Add(new WorkersRevisores
                {
                    SolicitanteId  = workerId,
                    RevisorId      = revisorId.Value,
                    OrdenPrioridad = 1,
                    Active         = true,
                    State          = true,
                    CreatedAt      = now,
                });
            }
            else if (elegido.OrdenPrioridad != 1 || !elegido.Active)
            {
                elegido.OrdenPrioridad = 1;
                elegido.Active = true;
                elegido.UpdatedAt = now;
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<List<JefeCandidatoDto>> GetCandidatosAsync()
        {
            using var ctx = _factory.CreateDbContext();

            return await (
                from w in ctx.Worker.AsNoTracking()
                where w.EmailCorporativo != null
                      && w.EmailCorporativo.Trim().ToLower().EndsWith(EmailDomainCorp)
                join p in ctx.Person.AsNoTracking() on w.PersonId equals p.PersonId
                where p.State == true
                orderby p.FullName
                select new JefeCandidatoDto
                {
                    WorkerId = w.Id,
                    PersonId = w.PersonId,
                    FullName = p.FullName,
                    Email    = w.EmailCorporativo,
                }
            ).ToListAsync();
        }
    }
}
