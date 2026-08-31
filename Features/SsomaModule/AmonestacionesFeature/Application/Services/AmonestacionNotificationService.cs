using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;
using Abril_Backend.Infrastructure.Interfaces;
using Dapper;
using Npgsql;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;

public class AmonestacionNotificationService
{
    private readonly IEmailService _email;
    private readonly IConfiguration _config;
    private readonly ILogger<AmonestacionNotificationService> _logger;

    public AmonestacionNotificationService(
        IEmailService email,
        IConfiguration config,
        ILogger<AmonestacionNotificationService> logger)
    {
        _email  = email;
        _config = config;
        _logger = logger;
    }

    private NpgsqlConnection Conn() => new(_config["Database:PostgreSQL"]);

    public async Task NotificarAmonestacionAsync(AmonestacionDetalleDto a, byte[] pdfBytes)
    {
        try
        {
            var destinatarios = await ObtenerDestinatariosAsync(a);
            if (destinatarios.Count == 0) return;

            var gravedad = a.NivelGravedad;
            var colorGravedad = gravedad switch
            {
                "CRITICO" => "#c0392b",
                "ALTO"    => "#e67e22",
                "MEDIO"   => "#f1c40f",
                _         => "#27ae60"
            };

            var cuerpo = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px">
                  <div style="background:{colorGravedad};padding:12px 16px">
                    <h2 style="color:#fff;margin:0">Amonestación registrada — {a.Codigo}</h2>
                  </div>
                  <div style="padding:16px;border:1px solid #ddd">
                    <table cellpadding="6" cellspacing="0" style="width:100%;font-size:13px">
                      <tr><td style="font-weight:bold;width:40%">Trabajador:</td><td>{a.WorkerNombre}</td></tr>
                      <tr><td style="font-weight:bold">DNI:</td><td>{a.WorkerDni}</td></tr>
                      <tr><td style="font-weight:bold">Empresa:</td><td>{a.EmpresaNombre}</td></tr>
                      <tr><td style="font-weight:bold">Proyecto:</td><td>{a.ProyectoNombre}</td></tr>
                      <tr><td style="font-weight:bold">Tipo de sanción:</td><td>{a.TipoSancionNombre}</td></tr>
                      <tr><td style="font-weight:bold">Infracción:</td><td>{a.InfraccionTipoNombre}</td></tr>
                      <tr><td style="font-weight:bold">Puntos esta amonestación:</td><td>{a.PuntosInfraccion}</td></tr>
                      <tr style="background:#fff3cd"><td style="font-weight:bold">Puntaje acumulado:</td>
                          <td><b>{a.PuntosAcumulados}/10</b></td></tr>
                      {(a.AplicaPenalizacion ? $"<tr><td style=\"font-weight:bold\">Monto penalización:</td><td>S/ {a.MontoCalculado:N2}</td></tr>" : "")}
                      {(a.Inhabilitado ? "<tr style=\"background:#f8d7da\"><td colspan=\"2\"><b>⚠ TRABAJADOR INHABILITADO — ha acumulado 10 o más puntos.</b></td></tr>" : "")}
                    </table>
                    <p style="font-size:13px;margin-top:12px"><b>Descripción:</b><br>{a.Descripcion}</p>
                    <p style="font-size:11px;color:#888;margin-top:16px">La papeleta se adjunta en este correo.</p>
                  </div>
                </div>
                """;

            await _email.SendAsync(
                to:          destinatarios,
                subject:     $"[AMONESTACIÓN] {a.Codigo} — {a.WorkerNombre} — {a.TipoSancionNombre}",
                body:        cuerpo,
                isHtml:      true,
                attachments: new List<EmailAttachment>
                {
                    new EmailAttachment
                    {
                        FileName    = $"Amonestacion_{a.Codigo}.pdf",
                        Content     = pdfBytes,
                        ContentType = "application/pdf"
                    }
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error enviando notificación amonestación {Codigo}", a.Codigo);
        }
    }

    private async Task<List<string>> ObtenerDestinatariosAsync(AmonestacionDetalleDto a)
    {
        // Obtiene emails de usuarios del sistema con acceso a la feature ssoma,
        // filtrando por rol según el nivel de gravedad y si es contratista o Abril
        var destinatarios = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        const string sqlBase = """
            SELECT DISTINCT au.email
            FROM app_user au
            JOIN user_role ur ON ur.user_id = au.user_id
            JOIN role r ON r.role_id = ur.role_id
            WHERE au.active = true AND au.email IS NOT NULL AND au.email <> ''
            AND r.role_name = ANY(@roles);
            """;

        string[] roles;

        if (!a.EsEmpresaAbril)
        {
            // Contratista
            roles = a.NivelGravedad is "ALTO" or "CRITICO"
                ? ["ADMINISTRADOR DEL SISTEMA", "COORDINADOR SSOMA", "ADMINISTRADOR DE SSOMA"]
                : ["COORDINADOR SSOMA", "ADMINISTRADOR DE SSOMA"];

            // Añadir emails de la contratista (contractor_email table)
            if (!string.IsNullOrEmpty(a.EmpresaNombre))
            {
                const string sqlContratista = """
                    SELECT COALESCE(email_administrador, '') FROM contributor
                    WHERE contributor_name = @nombre AND active = true LIMIT 1;
                    SELECT ce.email FROM contractor_email ce
                    JOIN contractor ct ON ct.contractor_id = ce.contractor_id
                    JOIN contributor c ON c.contributor_id = ct.contractor_id
                    WHERE c.contributor_name = @nombre AND ct.active = true;
                    """;
                await using var conn = Conn();
                await conn.OpenAsync();
                await using var multi = await conn.QueryMultipleAsync(sqlContratista, new { nombre = a.EmpresaNombre });
                var emailAdmin = await multi.ReadFirstOrDefaultAsync<string>();
                if (!string.IsNullOrEmpty(emailAdmin))
                    destinatarios.Add(emailAdmin);
                var emailsContrata = await multi.ReadAsync<string>();
                foreach (var e in emailsContrata)
                    if (!string.IsNullOrWhiteSpace(e))
                        destinatarios.Add(e);
            }
        }
        else
        {
            // Personal Abril
            roles = ["ADMINISTRADOR DEL SISTEMA", "COORDINADOR SSOMA", "RESIDENTE DE OBRA",
                     "ADMINISTRADOR DE PROYECTO"];
        }

        await using var conn2 = Conn();
        await conn2.OpenAsync();
        var emails = await conn2.QueryAsync<string>(sqlBase, new { roles });
        foreach (var e in emails)
            if (!string.IsNullOrWhiteSpace(e))
                destinatarios.Add(e);

        return destinatarios.ToList();
    }
}
