using Abril_Backend.Features.NotificacionesModule.Application.Interfaces;
using Abril_Backend.Features.NotificacionesModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.NotificacionesModule.Application.Services
{
    /// <summary>Motor de notificaciones transversal — CONTEXT_LOGISTICA.md sección 5. Resuelve
    /// destinatarios por evento + rol + scope (nunca un email fijo en código), deja bitácora en
    /// lb_notif_envio, y envía. Aditivo: no reemplaza los correos que Pedidos ya manda directo
    /// (PedidoService.NotificarEvento) — esos flujos ya probados se quedan como están; este motor
    /// lo usan casos nuevos (empezando por la alerta de SLA de pedidos estancados).</summary>
    public class NotificacionService : INotificacionService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IEmailService _emailService;

        public NotificacionService(IDbContextFactory<AppDbContext> factory, IEmailService emailService)
        {
            _factory = factory;
            _emailService = emailService;
        }

        public async Task Dispatch(
            string evento, int proyectoId, long? solicitanteUsuarioSistemaId,
            string asunto, string mensajeHtml, string? referenciaTipo, long? referenciaId)
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);

            var reglas = await ctx.NotifRegla.Where(r => r.Evento == evento && r.Activo).ToListAsync();
            if (reglas.Count == 0) return;

            // (usuarioSistemaId?, email) — usuarioSistemaId null para destinatarios extra sin
            // cuenta de sistema (ej. contabilidad externa).
            var destinatarios = new List<(long? UsuarioSistemaId, string Email)>();

            foreach (var regla in reglas)
            {
                if (regla.RolDestinoId.HasValue)
                {
                    var query = ctx.UsuarioAsignacion
                        .Where(a => a.RolId == regla.RolDestinoId.Value && a.Notificar)
                        .Where(a => a.FechaFin == null || a.FechaFin >= hoy);

                    query = regla.ScopeType switch
                    {
                        "GLOBAL" => query.Where(a => a.ProyectoId == null),
                        "MISMO_PROYECTO" => query.Where(a => a.ProyectoId == proyectoId),
                        "SOLICITANTE" => query.Where(a => false), // resuelto aparte, no por rol
                        _ => query,
                    };

                    if (regla.ScopeType != "SOLICITANTE")
                    {
                        var emails = await query
                            .Join(ctx.UsuarioSistema.Where(u => u.Estado == "ACTIVO"), a => a.UsuarioSistemaId, u => u.Id,
                                (a, u) => new { u.Id, u.EmailLogin })
                            .Distinct()
                            .ToListAsync();
                        destinatarios.AddRange(emails.Select(e => ((long?)e.Id, e.EmailLogin)));
                    }
                }

                if (regla.ScopeType == "SOLICITANTE" && solicitanteUsuarioSistemaId.HasValue)
                {
                    var email = await ctx.UsuarioSistema
                        .Where(u => u.Id == solicitanteUsuarioSistemaId.Value && u.Estado == "ACTIVO")
                        .Select(u => u.EmailLogin)
                        .FirstOrDefaultAsync();
                    if (email is not null) destinatarios.Add((solicitanteUsuarioSistemaId, email));
                }

                var extras = await ctx.NotifDestinatarioExtra
                    .Where(e => e.NotifReglaId == regla.Id)
                    .Select(e => e.Email)
                    .ToListAsync();
                destinatarios.AddRange(extras.Select(email => ((long?)null, email)));

                foreach (var (usuarioSistemaId, email) in destinatarios.Distinct())
                {
                    var envio = new NotifEnvio
                    {
                        NotifReglaId = regla.Id,
                        UsuarioSistemaId = usuarioSistemaId,
                        EmailDestino = email,
                        Canal = regla.Canal,
                        Estado = "PENDIENTE",
                        ReferenciaTipo = referenciaTipo,
                        ReferenciaId = referenciaId,
                        CreadoEn = DateTimeOffset.UtcNow,
                    };
                    ctx.NotifEnvio.Add(envio);
                    await ctx.SaveChangesAsync();

                    try
                    {
                        await _emailService.SendAsync(new List<string> { email }, asunto, mensajeHtml, true);
                        envio.Estado = "ENVIADO";
                        envio.EnviadoEn = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        envio.Estado = "FALLIDO";
                        envio.Error = ex.Message;
                    }
                    await ctx.SaveChangesAsync();
                }
            }
        }

        public async Task<bool> YaSeNotificoHoy(string evento, string referenciaTipo, long referenciaId)
        {
            using var ctx = _factory.CreateDbContext();
            var desde = DateTimeOffset.UtcNow.AddHours(-24);
            return await ctx.NotifEnvio
                .Include(e => e.NotifRegla)
                .AnyAsync(e => e.NotifRegla!.Evento == evento
                    && e.ReferenciaTipo == referenciaTipo && e.ReferenciaId == referenciaId
                    && e.CreadoEn >= desde);
        }
    }
}
