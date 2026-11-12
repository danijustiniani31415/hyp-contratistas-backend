using System.Net;
using Abril_Backend.Features.AlmacenModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.AlmacenModule.Application.Services
{
    /// <summary>Correos del inventario físico: cierre con diferencias, cierre pendiente de aprobación y decisión de gerencia.</summary>
    public class InventarioNotificador
    {
        private readonly IEmailService _email;
        private readonly FrontendSettings _front;
        private readonly ILogger<InventarioNotificador> _log;

        public InventarioNotificador(IEmailService email, IOptions<FrontendSettings> front, ILogger<InventarioNotificador> log)
        {
            _email = email;
            _front = front.Value;
            _log = log;
        }

        private static string Soles(decimal v) => "S/ " + v.ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("es-PE"));

        private static async Task<(Inventario Inv, string Almacen, int? Proyecto, string Quien)> Datos(AppDbContext ctx, long id)
        {
            var inv = await ctx.Inventario.AsNoTracking().FirstAsync(i => i.Id == id);
            var alm = await ctx.Almacen.AsNoTracking().Where(a => a.Id == inv.AlmacenId).Select(a => new { a.Nombre, a.ProyectoId }).FirstAsync();
            var quienId = inv.SolicitadoPor ?? inv.CerradoPor ?? inv.CreadoPor;
            var quien = quienId.HasValue
                ? await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == quienId).Select(u => u.Persona!.Apellidos + " " + u.Persona.Nombres).FirstOrDefaultAsync() ?? "Alguien"
                : "Alguien";
            return (inv, alm.Nombre, alm.ProyectoId, quien);
        }

        public async Task Pendiente(AppDbContext ctx, long id, decimal faltante, decimal sobrante)
        {
            var (inv, almacen, proyecto, quien) = await Datos(ctx, id);
            var cuerpo = $"<p><b>{WebUtility.HtmlEncode(quien)}</b> cerró el conteo del almacén <b>{WebUtility.HtmlEncode(almacen)}</b> con diferencias que superan el monto de aprobación ({Soles(inv.UmbralAplicado ?? 0)}):</p>" +
                         $"<ul><li>Faltan: <b>{Soles(faltante)}</b></li><li>Sobran: <b>{Soles(sobrante)}</b></li></ul>" +
                         "<p>El stock <b>no se ha tocado</b>. Entra a Kardex e Inventario para aprobar o rechazar el ajuste.</p>";
            await Enviar(await Destinatarios(ctx, proyecto, null, new[] { "GERENTE_GENERAL", "GERENTE_OPERACIONES" }),
                $"Inventario de {almacen}: ajuste por aprobar ({Soles(faltante + sobrante)})", cuerpo);
        }

        public async Task Cerrado(AppDbContext ctx, long id, decimal faltante, decimal sobrante)
        {
            var (inv, almacen, proyecto, quien) = await Datos(ctx, id);
            var cuerpo = $"<p>El conteo del almacén <b>{WebUtility.HtmlEncode(almacen)}</b> se cerró con <b>{inv.CantidadAjustes}</b> ajuste(s) de stock (registrado por {WebUtility.HtmlEncode(quien)}):</p>" +
                         $"<ul><li>Faltaron: <b>{Soles(faltante)}</b></li><li>Sobraron: <b>{Soles(sobrante)}</b></li></ul>";
            await Enviar(await Destinatarios(ctx, proyecto, null, new[] { "GERENTE_GENERAL", "CONTADORA" }),
                $"Inventario de {almacen}: cerrado con diferencias", cuerpo);
        }

        public async Task Decision(AppDbContext ctx, long id, bool aprobado)
        {
            var (inv, almacen, proyecto, _) = await Datos(ctx, id);
            var decide = inv.DecididoPor.HasValue
                ? await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == inv.DecididoPor).Select(u => u.Persona!.Apellidos + " " + u.Persona.Nombres).FirstOrDefaultAsync() ?? "Gerencia"
                : "Gerencia";
            var cuerpo = aprobado
                ? $"<p><b>{WebUtility.HtmlEncode(decide)}</b> aprobó el ajuste del inventario de <b>{WebUtility.HtmlEncode(almacen)}</b>. El stock ya se actualizó (faltaron {Soles(inv.ValorFaltante)}, sobraron {Soles(inv.ValorSobrante)}).</p>"
                : $"<p><b>{WebUtility.HtmlEncode(decide)}</b> rechazó el ajuste del inventario de <b>{WebUtility.HtmlEncode(almacen)}</b>. El stock no cambió; el conteo se reabrió para corregirlo.</p><p>Motivo: <i>{WebUtility.HtmlEncode(inv.MotivoDecision ?? "")}</i></p>";
            await Enviar(await Destinatarios(ctx, proyecto, inv.SolicitadoPor ?? inv.CreadoPor, aprobado ? new[] { "CONTADORA", "GERENTE_GENERAL" } : new[] { "CONTADORA" }),
                $"Inventario de {almacen}: ajuste {(aprobado ? "aprobado" : "rechazado")}", cuerpo);
        }

        private static async Task<List<string>> Destinatarios(AppDbContext ctx, int? proyectoId, long? usuarioExtraId, string[] roles)
        {
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            var correos = await (from a in ctx.UsuarioAsignacion.AsNoTracking()
                                 join r in ctx.Rol.AsNoTracking() on a.RolId equals r.Id
                                 join u in ctx.UsuarioSistema.AsNoTracking() on a.UsuarioSistemaId equals u.Id
                                 where u.Estado == "ACTIVO" && a.Notificar && (a.FechaFin == null || a.FechaFin >= hoy) && roles.Contains(r.Codigo)
                                       && (a.ProyectoId == null || proyectoId == null || a.ProyectoId == proyectoId)
                                 select u.EmailLogin).Distinct().ToListAsync();
            if (usuarioExtraId.HasValue)
            {
                var extra = await ctx.UsuarioSistema.AsNoTracking().Where(u => u.Id == usuarioExtraId && u.Estado == "ACTIVO").Select(u => u.EmailLogin).FirstOrDefaultAsync();
                if (extra != null && !correos.Contains(extra)) correos.Add(extra);
            }
            return correos;
        }

        private async Task Enviar(List<string> destinatarios, string asunto, string cuerpoHtml)
        {
            if (destinatarios.Count == 0) return;
            var link = string.IsNullOrWhiteSpace(_front.LbAppUrl) ? "" : $"<a href='{_front.LbAppUrl.TrimEnd('/')}/inventario' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Abrir en el sistema</a>";
            try
            {
                await _email.SendAsync(to: destinatarios, subject: asunto, body: $"<h2 style='font-size:17px'>{WebUtility.HtmlEncode(asunto)}</h2>{cuerpoHtml}{link}", isHtml: true);
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "[Inventario] No se pudo enviar el correo «{Asunto}» a {Destinatarios}", asunto, string.Join(", ", destinatarios));
            }
        }
    }
}
