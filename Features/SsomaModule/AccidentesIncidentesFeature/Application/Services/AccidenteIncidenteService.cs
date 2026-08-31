using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Interfaces;
using Abril_Backend.Infrastructure.Interfaces;
using Microsoft.AspNetCore.Hosting;

namespace Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Services;

public class AccidenteIncidenteService : IAccidenteIncidenteService
{
    private readonly IAccidenteIncidenteRepository _repo;
    private readonly ISharePointHabService _sp;
    private readonly IEmailService _email;
    private readonly ILogger<AccidenteIncidenteService> _logger;
    private readonly IConfiguration _config;
    public AccidenteIncidenteService(
        IAccidenteIncidenteRepository repo,
        ISharePointHabService sp,
        IEmailService email,
        ILogger<AccidenteIncidenteService> logger,
        IConfiguration config)
    {
        _repo = repo;
        _sp = sp;
        _email = email;
        _logger = logger;
        _config = config;
    }

    public Task<FlashReportInicializarDto> GetInicializarAsync() => _repo.GetInicializarAsync();

    public async Task<object> GetListAsync(int? proyectoId, int? tipoId, string? estado,
        DateTime? fechaDesde, DateTime? fechaHasta, bool? soloEnviados, string? areaOrigen, int page, int pageSize)
    {
        var (items, total) = await _repo.GetListAsync(proyectoId, tipoId, estado, fechaDesde, fechaHasta, soloEnviados, areaOrigen, page, pageSize);
        return new { items, total, page, pageSize, totalPages = (int)Math.Ceiling((double)total / pageSize) };
    }

    public async Task<FlashReportDetalleDto> GetDetalleAsync(int id)
    {
        return await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);
    }

    public async Task<int> CrearAsync(CrearFlashReportRequest request, int? usuarioId)
    {
        if (request.Trabajadores.Count > 1)
            throw new AbrilException("Un Flash Report solo admite un trabajador afectado. Regístrese un reporte independiente por cada accidentado.", 400);

        request.ElaboradoPorEmail = ValidarCorreoElaborador(request.ElaboradoPorEmail);

        // Obtener código del tipo
        var init = await _repo.GetInicializarAsync();
        var tipo = init.Tipos.FirstOrDefault(t => t.Id == request.TipoId)
            ?? throw new AbrilException("Tipo de flash report no válido.", 400);

        var codigo = await _repo.GenerarCodigoAsync(request.ProyectoId, tipo.Codigo ?? "XX");

        var urlFoto1 = await SubirFotoAsync(request.Foto1Base64, codigo, "foto1");
        var urlFoto2 = await SubirFotoAsync(request.Foto2Base64, codigo, "foto2");

        // Generar entregables solo si: no es incidente, O es incidente con potencial N5/N6 (Alto Riesgo)
        var esIncidente = (tipo.Codigo ?? "").Equals("IN", StringComparison.OrdinalIgnoreCase);
        var potencial = request.ConsecuenciaPotencialPersonal ?? 0;
        var generarEntregables = !esIncidente || potencial >= 5;

        return await _repo.CrearAsync(request, codigo, urlFoto1, urlFoto2, usuarioId, generarEntregables);
    }

    public async Task ActualizarAsync(int id, ActualizarFlashReportRequest request)
    {
        if (request.Trabajadores.Count > 1)
            throw new AbrilException("Un Flash Report solo admite un trabajador afectado. Regístrese un reporte independiente por cada accidentado.", 400);

        request.ElaboradoPorEmail = ValidarCorreoElaborador(request.ElaboradoPorEmail);

        var existente = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        var urlFoto1 = request.Foto1Base64 != null
            ? await SubirFotoAsync(request.Foto1Base64, existente.Codigo, "foto1")
            : null;
        var urlFoto2 = request.Foto2Base64 != null
            ? await SubirFotoAsync(request.Foto2Base64, existente.Codigo, "foto2")
            : null;

        await _repo.ActualizarAsync(id, request, urlFoto1, urlFoto2);
    }

    public Task ActualizarSeveridadAsync(int id, int? consecuenciaRealPersonal, int? consecuenciaPotencialPersonal)
        => _repo.ActualizarSeveridadAsync(id, consecuenciaRealPersonal, consecuenciaPotencialPersonal);

    public async Task EnviarFlashReportAsync(int id, bool enviarEmail = true)
    {
        var fr = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        if (fr.Enviado)
            throw new AbrilException("El Flash Report ya fue enviado.", 400);

        var requiereSeveridad = fr.TipoCodigo.Equals("AC", StringComparison.OrdinalIgnoreCase)
            || fr.TipoCodigo.Equals("IN", StringComparison.OrdinalIgnoreCase);
        if (requiereSeveridad && (fr.ConsecuenciaRealPersonal == null || fr.ConsecuenciaPotencialPersonal == null))
            throw new AbrilException("Debe registrar la consecuencia real y potencial antes de enviar el Flash Report.", 400);

        // Descargar fotos si existen
        byte[]? foto1Bytes = null;
        byte[]? foto2Bytes = null;
        if (!string.IsNullOrEmpty(fr.UrlFoto1))
            foto1Bytes = await DescargarArchivoAsync(fr.UrlFoto1);
        if (!string.IsNullOrEmpty(fr.UrlFoto2))
            foto2Bytes = await DescargarArchivoAsync(fr.UrlFoto2);

        // Generar PDF (el logo se carga internamente en FlashReportPdfService)
        var pdfBytes = FlashReportPdfService.Generar(fr, foto1Bytes, foto2Bytes);

        // Subir PDF a SharePoint
        var pdfNombre = $"FlashReport_{fr.Codigo}_{DateTime.UtcNow:yyyyMMdd}.pdf";
        await using var pdfStream = new MemoryStream(pdfBytes);
        var urlPdf = await _sp.SubirArchivoEnRutaAsync(
            pdfStream, pdfNombre, "flash-report", $"flash-reports/{fr.Codigo}");

        // Marcar como enviado
        await _repo.MarcarEnviadoAsync(id, urlPdf ?? "");

        // Auto-crear Accidente de Trabajo vinculado si tipo = Accidente (AC)
        if (fr.TipoCodigo.Equals("AC", StringComparison.OrdinalIgnoreCase))
        {
            var usuarioId = int.TryParse(_config["System:BotUserId"], out var uid) ? uid : 1;
            await _repo.CrearAccidenteTrabajoVinculadoAsync(fr, usuarioId);
        }

        // Enviar email (se puede omitir para regularizaciones de flash reports atrasados)
        if (enviarEmail)
            await EnviarEmailAsync(fr, pdfBytes, pdfNombre);
    }

    public async Task EliminarAsync(int id)
    {
        var fr = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);
        if (fr.Enviado)
            throw new AbrilException("No se puede eliminar un Flash Report ya enviado.", 400);
        await _repo.EliminarAsync(id);
    }

    // ── PDF y fotos on-demand ────────────────────────────────────────────────

    public async Task<byte[]> GenerarPdfAsync(int id)
    {
        var fr = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        byte[]? foto1 = null;
        byte[]? foto2 = null;
        if (!string.IsNullOrEmpty(fr.UrlFoto1)) foto1 = await DescargarArchivoAsync(fr.UrlFoto1);
        if (!string.IsNullOrEmpty(fr.UrlFoto2)) foto2 = await DescargarArchivoAsync(fr.UrlFoto2);

        return FlashReportPdfService.Generar(fr, foto1, foto2);
    }

    public async Task<(byte[] Bytes, string ContentType, string FileName)> ObtenerFotoAsync(int id, int slot)
    {
        var fr = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        var url = slot == 1 ? fr.UrlFoto1 : fr.UrlFoto2;
        if (string.IsNullOrEmpty(url))
            throw new AbrilException("Foto no disponible.", 404);

        var bytes = await DescargarArchivoAsync(url);
        if (bytes == null || bytes.Length == 0)
            throw new AbrilException("No se pudo obtener la foto.", 502);

        var ext = Path.GetExtension(url).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png"  => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
        var fileName = $"foto{slot}_{fr.Codigo}{ext}";
        return (bytes, contentType, fileName);
    }

    // ── Acciones vencidas ────────────────────────────────────────────────────

    public async Task<List<AccionCorrectivaVencidaDto>> GetAccionesVencidasAsync()
    {
        return await _repo.GetAccionesVencidasAsync();
    }

    // ── Medidas de control ────────────────────────────────────────────────────

    public async Task<List<AccionCorrectivaDto>> GetMedidasAsync(int accidenteId)
        => await _repo.GetMedidasAsync(accidenteId);

    public async Task<int> AddMedidaAsync(int accidenteId, GuardarAccionCorrectivaRequest req)
        => await _repo.AddMedidaAsync(accidenteId, req);

    public async Task UpdateMedidaAsync(int accionId, GuardarAccionCorrectivaRequest req)
        => await _repo.UpdateMedidaAsync(accionId, req);

    public async Task DeleteMedidaAsync(int accionId)
        => await _repo.DeleteMedidaAsync(accionId);

    // ── MINTRA ────────────────────────────────────────────────────────────────

    public async Task<byte[]> GenerarMintraAsync(int id)
    {
        var fr = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);
        return MintraPdfService.Generar(fr);
    }

    // ── Privados ─────────────────────────────────────────────────────────────

    private async Task<string?> SubirFotoAsync(string? base64, string codigo, string nombreFoto)
    {
        if (string.IsNullOrWhiteSpace(base64)) return null;
        try
        {
            var bytes = Convert.FromBase64String(base64.Contains(',')
                ? base64[(base64.IndexOf(',') + 1)..]
                : base64);
            await using var stream = new MemoryStream(bytes);
            var ext = DetectarExtension(bytes);
            var fileName = $"{nombreFoto}_{DateTime.UtcNow:yyyyMMddHHmmss}.{ext}";
            return await _sp.SubirArchivoEnRutaAsync(stream, fileName, "flash-report", $"flash-reports/{codigo}/fotos");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo subir {Foto} del Flash Report {Codigo}", nombreFoto, codigo);
            return null;
        }
    }

    private async Task<byte[]?> DescargarArchivoAsync(string url)
    {
        try
        {
            // Usar DescargarContenidoAsync que usa Graph API con token directamente
            return await _sp.DescargarContenidoAsync(url, "flash-report");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo descargar archivo {Url}", url);
            return null;
        }
    }

    private async Task EnviarEmailAsync(FlashReportDetalleDto fr, byte[] pdfBytes, string pdfNombre)
    {
        var overrideEmail = _config["FlashReport:TestEmail"];
        List<string> destinatarios;
        if (!string.IsNullOrWhiteSpace(overrideEmail))
        {
            destinatarios = [overrideEmail];
        }
        else
        {
            // Fijos siempre
            destinatarios = ["rvera@abril.pe", "fftoratto@abril.pe"];

            // Dinámicos: área Proyectos + GTH + Médico Ocupacional
            var dinamicos = await _repo.GetDestinatariosFlashReportAsync();
            destinatarios.AddRange(dinamicos);

            if (!string.IsNullOrWhiteSpace(fr.ElaboradoPorEmail))
                destinatarios.Add(fr.ElaboradoPorEmail);

            // Saneo obligatorio: Power Automate rechaza TODO el campo "To" si un solo
            // destinatario no tiene formato de email válido. Normalizamos, descartamos
            // los inválidos (logueándolos) y quitamos duplicados case-insensitive para
            // que un correo mal escrito nunca vuelva a tumbar el envío completo.
            var invalidos = destinatarios.Where(e => !EsCorreoAbrilValido(e)).ToList();
            if (invalidos.Count > 0)
                _logger.LogWarning(
                    "Flash Report {Codigo}: se descartaron {N} destinatarios inválidos: {Lista}",
                    fr.Codigo, invalidos.Count, string.Join(", ", invalidos));

            destinatarios = destinatarios
                .Select(NormalizarCorreo)
                .Where(EsCorreoAbrilValido)
                .Distinct()
                .ToList();
        }

        var asunto = $"Flash Report {fr.Codigo} - {fr.ProyectoNombre} - {fr.Fecha:dd/MM/yyyy}";
        var nivel = fr.ConsecuenciaRealPersonal.HasValue && fr.ConsecuenciaRealPersonal > 0
            ? $"Consecuencia real: Nivel {fr.ConsecuenciaRealPersonal}" : "";

        var cuerpo = $"""
            <p>Se ha generado y enviado el <strong>Flash Report {fr.Codigo}</strong>.</p>
            <table style="border-collapse:collapse;font-family:Arial;font-size:13px;">
              <tr><td style="padding:4px 12px;font-weight:bold;">Proyecto</td><td>{fr.ProyectoNombre}</td></tr>
              <tr><td style="padding:4px 12px;font-weight:bold;">Tipo</td><td>{fr.TipoNombre}</td></tr>
              <tr><td style="padding:4px 12px;font-weight:bold;">Fecha</td><td>{fr.Fecha:dd/MM/yyyy}</td></tr>
              <tr><td style="padding:4px 12px;font-weight:bold;">Trabajador</td><td>{fr.TrabajadorNombre ?? "—"}</td></tr>
              <tr><td style="padding:4px 12px;font-weight:bold;">Descripción</td><td>{fr.Descripcion}</td></tr>
              {(string.IsNullOrEmpty(nivel) ? "" : $"<tr><td style='padding:4px 12px;font-weight:bold;'>Severidad</td><td>{nivel}</td></tr>")}
              <tr><td style="padding:4px 12px;font-weight:bold;">Elaborado por</td><td>{fr.ElaboradoPorNombre ?? "—"}</td></tr>
            </table>
            <p style="color:#666;font-size:11px;margin-top:16px;">Adjunto encontrará el Flash Report en formato PDF.<br>Sistema SSOMA - Abril</p>
            """;

        await _email.SendAsync(
            destinatarios,
            asunto,
            cuerpo,
            isHtml: true,
            attachments: [new EmailAttachment { FileName = pdfNombre, Content = pdfBytes, ContentType = "application/pdf" }]
        );
    }

    private static string DetectarExtension(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "jpg";
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "png";
        return "jpg";
    }

    // ── Correo del elaborador ────────────────────────────────────────────────
    // Correo corporativo @abril.pe sin espacios, tildes ni símbolos raros.
    private static readonly System.Text.RegularExpressions.Regex _correoAbrilRegex =
        new(@"^[a-z0-9._-]+@abril\.pe$", System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Recorta, elimina espacios internos y pasa a minúsculas.</summary>
    private static string NormalizarCorreo(string? correo)
        => (correo ?? string.Empty).Trim().Replace(" ", "").ToLowerInvariant();

    /// <summary>True si, tras normalizar, es un correo @abril.pe válido.</summary>
    private static bool EsCorreoAbrilValido(string? correo)
        => _correoAbrilRegex.IsMatch(NormalizarCorreo(correo));

    /// <summary>
    /// Valida y normaliza el correo del elaborador. Es obligatorio y debe ser un
    /// @abril.pe limpio: es el correo que se usa como destinatario del Flash Report.
    /// </summary>
    private static string ValidarCorreoElaborador(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo))
            throw new AbrilException("El correo del elaborador es obligatorio.", 400);
        if (!EsCorreoAbrilValido(correo))
            throw new AbrilException(
                "El correo del elaborador debe ser un correo @abril.pe válido (sin espacios ni símbolos).", 400);
        return NormalizarCorreo(correo);
    }

    // ── Entregables ───────────────────────────────────────────────────────────

    public Task<List<EntregableDto>> GetEntregablesAsync(int accidenteId)
        => _repo.GetEntregablesAsync(accidenteId);

    public Task ActualizarEntregableAsync(int entregableId, ActualizarEntregableRequest req)
        => _repo.ActualizarEntregableAsync(entregableId, req);

    public async Task<string> SubirArchivoEntregableAsync(int entregableId, IFormFile archivo)
    {
        await using var stream = archivo.OpenReadStream();
        var ext = Path.GetExtension(archivo.FileName);
        var nombre = $"{Path.GetFileNameWithoutExtension(archivo.FileName)}_{DateTime.UtcNow:yyyyMMddHHmmss}{ext}";
        var url = await _sp.SubirArchivoEnRutaAsync(stream, nombre, "flash-report", "flash-reports/entregables");
        await _repo.SubirArchivoEntregableAsync(entregableId, url, archivo.FileName);
        return url;
    }

    public Task EliminarArchivoEntregableAsync(int archivoId)
        => _repo.EliminarArchivoEntregableAsync(archivoId);

    // ── RM-050 ────────────────────────────────────────────────────────────────

    public async Task<Rm050Dto> GetRm050Async(int accidenteId)
    {
        var rm = await _repo.GetRm050Async(accidenteId);
        return rm ?? new Rm050Dto();
    }

    public Task GuardarRm050Async(int accidenteId, GuardarRm050Request req)
        => _repo.GuardarRm050Async(accidenteId, req);

    // ── Reclasificar ─────────────────────────────────────────────────────────

    public async Task<int> ReclasificarComoAccidenteAsync(int id, int? usuarioId)
    {
        var fr = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Flash Report no encontrado.", 404);

        if (fr.TipoCodigo.Equals("AC", StringComparison.OrdinalIgnoreCase))
            throw new AbrilException("El Flash Report ya está clasificado como Accidente.", 400);

        // Obtener el tipo "AC" del catálogo
        var init = await _repo.GetInicializarAsync();
        var tipoAC = init.Tipos.FirstOrDefault(t => (t.Codigo ?? "").Equals("AC", StringComparison.OrdinalIgnoreCase))
            ?? throw new AbrilException("Tipo de Accidente (AC) no encontrado en catálogo.", 500);

        // Cambiar el tipo del flash report
        await _repo.ReclasificarTipoAsync(id, tipoAC.Id, tipoAC.Codigo ?? "AC", tipoAC.Nombre);

        // Si ya fue enviado, generar entregables de accidente y crear AccidenteTrabajo vinculado
        if (fr.Enviado)
        {
            var uid = usuarioId ?? (int.TryParse(_config["System:BotUserId"], out var uid2) ? uid2 : 1);
            var frActualizado = await _repo.GetDetalleAsync(id)!;
            var accidenteId = await _repo.CrearAccidenteTrabajoVinculadoAsync(frActualizado!, uid);
            return accidenteId ?? 0;
        }

        return 0;
    }
}
