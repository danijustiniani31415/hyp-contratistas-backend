using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Asigna el aprobador REAL de una solicitud de salida al momento de la decisión
    /// (aprobar/rechazar). El aprobador puede ser un trabajador (aprobador_worker_id)
    /// o un área (aprobador_area_scope_id, ej. GTH) — nunca ambos: lo garantiza el
    /// CHECK chk_ga_solicitud_salida_aprobador_unico en la BD.
    /// No hace SaveChanges: muta la entidad y el caller persiste.
    /// </summary>
    public static class SalidaAprobadorHelper
    {
        /// <summary>
        /// Decisión tomada desde la web (gestión de salidas): el aprobador real es el
        /// trabajador del usuario logueado. Si el usuario no tiene worker asociado se
        /// intenta atribuir por el correo al que se envió la solicitud.
        /// </summary>
        public static async Task AsignarPorUsuarioAsync(AppDbContext ctx, GaSolicitudSalida s, int reviewerUserId)
        {
            var workerId = await (
                from w in ctx.Worker
                join p in ctx.Person on w.PersonId equals (int?)p.PersonId
                where p.UserId == reviewerUserId
                select (int?)w.Id
            ).FirstOrDefaultAsync();

            if (workerId.HasValue)
            {
                s.AprobadorWorkerId = workerId.Value;
                s.AprobadorAreaScopeId = null;
                return;
            }

            await AsignarPorCorreoEnviadoAsync(ctx, s);
        }

        /// <summary>
        /// Decisión tomada desde el email (token): se atribuye al dueño del correo al que
        /// se envió la solicitud — un worker (email_corporativo) o un área (area_scope.email,
        /// ej. gthnm@abril.pe → área GTH). En solicitudes antiguas sin enviado_a_correo se
        /// conserva el aprobador_worker_id que se guardó al crearlas.
        /// </summary>
        public static async Task AsignarPorCorreoEnviadoAsync(AppDbContext ctx, GaSolicitudSalida s)
        {
            if (string.IsNullOrWhiteSpace(s.EnviadoACorreo)) return;
            var correo = s.EnviadoACorreo.Trim().ToLower();

            var workerId = await ctx.Worker
                .Where(w => w.EmailCorporativo != null && w.EmailCorporativo.Trim().ToLower() == correo)
                .OrderBy(w => w.Id)
                .Select(w => (int?)w.Id)
                .FirstOrDefaultAsync();
            if (workerId.HasValue)
            {
                s.AprobadorWorkerId = workerId.Value;
                s.AprobadorAreaScopeId = null;
                return;
            }

            var areaScopeId = await ctx.AreaScope
                .Where(a => a.State && a.Email != null && a.Email.Trim().ToLower() == correo)
                .OrderBy(a => a.AreaScopeId)
                .Select(a => (int?)a.AreaScopeId)
                .FirstOrDefaultAsync();
            if (areaScopeId.HasValue)
            {
                s.AprobadorWorkerId = null;
                s.AprobadorAreaScopeId = areaScopeId.Value;
            }
        }
    }
}
