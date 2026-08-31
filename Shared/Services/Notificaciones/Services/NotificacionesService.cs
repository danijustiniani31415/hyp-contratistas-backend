using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Models;
using Abril_Backend.Shared.Services.Notificaciones.Dtos;
using Abril_Backend.Shared.Services.Notificaciones.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services.Notificaciones.Services
{
    public class NotificacionesService : INotificacionesService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        /// <summary>Máximo de notificaciones que devuelve el panel (las más recientes).</summary>
        private const int MaxNotificacionesPanel = 50;

        public NotificacionesService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task CrearPorCorreosAsync(
            string tipoCodigo,
            IReadOnlyCollection<string> destinatarioEmails,
            int? origenUserId,
            IReadOnlyCollection<NuevaNotificacionDto> items)
        {
            if (items == null || items.Count == 0) return;

            var emails = (destinatarioEmails ?? Array.Empty<string>())
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Select(e => e.Trim().ToLowerInvariant())
                .Distinct()
                .ToList();
            if (emails.Count == 0) return;

            using var ctx = _factory.CreateDbContext();

            var tipoId = await ctx.NotificacionTipo
                .Where(t => t.Codigo == tipoCodigo && t.State && t.Active)
                .Select(t => t.NotificacionTipoId)
                .FirstOrDefaultAsync();
            if (tipoId == 0)
                throw new AbrilException($"El tipo de notificación «{tipoCodigo}» no está configurado.", 500);

            // Correos configurados → usuarios del sistema. Los que no tienen usuario
            // (p.ej. buzones grupales como gth@abril.pe) solo reciben el correo.
            var userIds = await ctx.User
                .Where(u => u.State && u.Active && u.Email != null && emails.Contains(u.Email.ToLower()))
                .Select(u => u.UserId)
                .ToListAsync();
            if (userIds.Count == 0) return;

            // Snapshot del nombre de quien generó el evento (para las iniciales del avatar).
            string? origenNombre = null;
            if (origenUserId.HasValue)
            {
                origenNombre = await ctx.Person
                    .Where(p => p.UserId == origenUserId.Value && p.State)
                    .Select(p => p.FullName)
                    .FirstOrDefaultAsync();
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var userId in userIds)
            {
                foreach (var item in items)
                {
                    ctx.Notificacion.Add(new Notificacion
                    {
                        UserId             = userId,
                        NotificacionTipoId = tipoId,
                        Titulo             = item.Titulo,
                        Subtitulo          = item.Subtitulo,
                        Descripcion        = item.Descripcion,
                        Referencia         = item.Referencia,
                        OrigenUserId       = origenUserId,
                        OrigenNombre       = origenNombre,
                        Leida              = false,
                        CreatedDateTime    = now,
                        CreatedUserId      = origenUserId,
                        Active             = true,
                        State              = true,
                    });
                }
            }

            await ctx.SaveChangesAsync();
        }

        public async Task<NotificacionesDto> GetMisNotificaciones(int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var raw = await ctx.Notificacion
                .Where(n => n.UserId == userId && n.State && n.Active)
                .OrderByDescending(n => n.CreatedDateTime).ThenByDescending(n => n.NotificacionId)
                .Take(MaxNotificacionesPanel)
                .Select(n => new
                {
                    n.NotificacionId,
                    n.Titulo,
                    n.Subtitulo,
                    n.Descripcion,
                    n.Referencia,
                    n.OrigenNombre,
                    n.Leida,
                    n.CreatedDateTime,
                })
                .ToListAsync();

            // Contador aparte: puede haber no leídas más antiguas que las 50 del panel.
            var noLeidas = await ctx.Notificacion
                .CountAsync(n => n.UserId == userId && n.State && n.Active && !n.Leida);

            return new NotificacionesDto
            {
                NoLeidas = noLeidas,
                Notificaciones = raw.Select(n => new NotificacionItemDto
                {
                    Id           = n.NotificacionId,
                    Titulo       = n.Titulo,
                    Subtitulo    = n.Subtitulo,
                    Descripcion  = n.Descripcion,
                    Referencia   = n.Referencia,
                    OrigenNombre = n.OrigenNombre,
                    Leida        = n.Leida,
                    Fecha        = n.CreatedDateTime.ToOffset(TimeSpan.FromHours(-5)).DateTime,
                }).ToList(),
            };
        }

        public async Task MarcarLeida(int notificacionId, int userId)
        {
            using var ctx = _factory.CreateDbContext();

            var notificacion = await ctx.Notificacion
                .FirstOrDefaultAsync(n => n.NotificacionId == notificacionId && n.UserId == userId && n.State);
            if (notificacion == null)
                throw new AbrilException("Notificación no encontrada.", 404);

            if (!notificacion.Leida)
            {
                var now = DateTimeOffset.UtcNow;
                notificacion.Leida           = true;
                notificacion.LeidaDateTime   = now;
                notificacion.UpdatedDateTime = now;
                notificacion.UpdatedUserId   = userId;
                await ctx.SaveChangesAsync();
            }
        }

        public async Task MarcarTodasLeidas(int userId)
        {
            using var ctx = _factory.CreateDbContext();
            var now = DateTimeOffset.UtcNow;

            // Un solo UPDATE en BD (sin traer las filas a memoria).
            await ctx.Notificacion
                .Where(n => n.UserId == userId && n.State && n.Active && !n.Leida)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(n => n.Leida, true)
                    .SetProperty(n => n.LeidaDateTime, now)
                    .SetProperty(n => n.UpdatedDateTime, now)
                    .SetProperty(n => n.UpdatedUserId, userId));
        }
    }
}
