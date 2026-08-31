using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Models;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;

public class AmonestacionService : IAmonestacionService
{
    // Id del tipo sanción "Retiro definitivo del proyecto" en ssoma_amonestacion_tipo_sanciones
    private const int TIPO_SANCION_RETIRO_DEFINITIVO = 4;

    private readonly IAmonestacionRepository _repo;
    private readonly AmonestacionNotificationService _notif;
    private readonly ISharePointHabService _sharePoint;
    private readonly SsomaInhabilitacionService _inhabilitacion;
    private readonly ITrabajadorRestringidoService _restringido;
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly ILogger<AmonestacionService> _logger;
    private readonly string _logoPath;

    public AmonestacionService(
        IAmonestacionRepository repo,
        AmonestacionNotificationService notif,
        ISharePointHabService sharePoint,
        SsomaInhabilitacionService inhabilitacion,
        ITrabajadorRestringidoService restringido,
        IDbContextFactory<AppDbContext> factory,
        ILogger<AmonestacionService> logger,
        IWebHostEnvironment env)
    {
        _repo           = repo;
        _notif          = notif;
        _sharePoint     = sharePoint;
        _inhabilitacion = inhabilitacion;
        _restringido    = restringido;
        _factory        = factory;
        _logger         = logger;
        _logoPath   = new[] {
            Path.Combine(env.WebRootPath, "images", "abril-logo.png"),
            Path.Combine(env.WebRootPath, "images", "logo-abril.jpg"),
            Path.Combine(env.ContentRootPath, "Templates", "logo-abril.jpg"),
        }.FirstOrDefault(File.Exists) ?? "";
    }

    public Task<AmonestacionInitDto> GetInitAsync() => _repo.GetInitAsync();

    public async Task<AmonestacionCreadaDto> CrearAsync(AmonestacionCreateRequest req, int userId)
    {
        if (req.PuntosInfraccion < 0 || req.PuntosInfraccion > 10)
            throw new AbrilException("Los puntos por infracción deben estar entre 0 y 10.", 400);

        if (req.AplicaPenalizacion && req.SancionInfraccionId is null)
            throw new AbrilException("Debe seleccionar una sanción si aplica penalización.", 400);

        // Defensa en profundidad: el frontend ya exige descripción no vacía, pero acá se valida
        // igual porque este campo es el único lugar donde queda la razón REAL de la sanción —
        // sin esto, un retiro definitivo puede terminar en la lista negra sin más motivo que
        // la clasificación genérica ("Retiro definitivo del proyecto por sanción CRÍTICA").
        if (string.IsNullOrWhiteSpace(req.Descripcion) || req.Descripcion.Trim().Length < 15)
            throw new AbrilException("La descripción debe explicar la razón real de la sanción (mínimo 15 caracteres).", 400);

        // Retiro definitivo del proyecto: siempre son 10 puntos,
        // sin importar lo que se haya seleccionado/enviado en el formulario.
        if (req.TipoSancionId == TIPO_SANCION_RETIRO_DEFINITIVO)
            req.PuntosInfraccion = 10;

        var codigo = await _repo.GenerarCodigoAsync(req.ProyectoId);

        // Calcular monto
        decimal monto = 0m;
        if (req.AplicaPenalizacion && req.SancionInfraccionId.HasValue)
        {
            using var ctx = _factory.CreateDbContext();
            var infraccion = await ctx.SsomaRacInfracciones
                .Where(i => i.Id == req.SancionInfraccionId.Value)
                .FirstOrDefaultAsync();
            if (infraccion is not null)
            {
                if (infraccion.MontoFijo.HasValue && infraccion.MontoFijo > 0)
                    monto = infraccion.MontoFijo.Value;
                else if (infraccion.FactorUit.HasValue && infraccion.FactorUit > 0)
                {
                    var uit = await ctx.SsomaUitAnios
                        .Where(u => u.Anio == DateTime.UtcNow.Year && u.Activo)
                        .Select(u => u.Valor)
                        .FirstOrDefaultAsync();
                    monto = infraccion.FactorUit.Value * uit;
                }
            }
        }

        // Decodificar fotos de base64
        var fotosBytes = new List<(byte[] Bytes, string Nombre)>();
        foreach (var foto in req.Fotos)
        {
            try
            {
                var base64 = foto.Base64.Contains(',')
                    ? foto.Base64.Split(',')[1]
                    : foto.Base64;
                fotosBytes.Add((Convert.FromBase64String(base64), foto.NombreArchivo));
            }
            catch
            {
                // foto inválida, ignorar
            }
        }

        var esBorrador = req.Estado == "Borrador";

        // Construir DTO de detalle para crear
        var detalle = new AmonestacionDetalleDto
        {
            Codigo              = codigo,
            PersonaReportaId    = userId > 0 ? userId : null,
            Estado              = esBorrador ? "Borrador" : "Registrada",
            ProyectoId          = req.ProyectoId,
            Fecha               = DateTime.TryParse(req.Fecha, out var fd)
                                    ? DateTime.SpecifyKind(fd, DateTimeKind.Utc)
                                    : DateTime.UtcNow,
            WorkerId            = req.WorkerId,
            PartidaId           = req.PartidaId,
            TipoSancionId       = req.TipoSancionId,
            InfraccionTipoId    = req.InfraccionTipoId,
            Descripcion         = req.Descripcion,
            AplicaPenalizacion  = req.AplicaPenalizacion,
            SancionInfraccionId = req.SancionInfraccionId,
            MontoCalculado      = monto,
            PuntosInfraccion    = req.PuntosInfraccion,
            DiasSuspension      = req.DiasSuspension,
            FechaInicioSuspension = req.FechaInicioSuspension != null
                ? DateOnly.TryParse(req.FechaInicioSuspension, out var fi) ? fi : null
                : null,
            FechaFinSuspension  = req.FechaFinSuspension != null
                ? DateOnly.TryParse(req.FechaFinSuspension, out var ff) ? ff : null
                : null,
        };

        List<(string Base64, string Nombre, string Url)> fotasParaRepo;

        if (esBorrador)
        {
            // Borrador: guardar base64 en BD, sin SharePoint, sin PDF, sin correo
            fotasParaRepo = fotosBytes
                .Select(f => (Convert.ToBase64String(f.Bytes), f.Nombre, ""))
                .ToList();
            var idBorrador = await _repo.CrearAsync(detalle, fotasParaRepo);
            return new AmonestacionCreadaDto { Id = idBorrador, Codigo = codigo };
        }

        // Registrada: subir fotos a SharePoint y obtener URLs
        var fotosConUrl = new List<(string Base64, string Nombre, string Url)>();
        foreach (var (bytes, nombre) in fotosBytes)
        {
            var url = "";
            try
            {
                var ext = Path.GetExtension(nombre).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var fileName = $"{codigo}_foto_{DateTime.UtcNow:HHmmssffff}{ext}";
                using var stream = new MemoryStream(bytes);
                url = await _sharePoint.SubirArchivoEnRutaAsync(
                    stream, fileName, "amonestacion-pdf",
                    $"Amonestaciones/{DateTime.UtcNow:yyyy}/Fotos");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo subir foto de amonestacion");
            }
            fotosConUrl.Add((Convert.ToBase64String(bytes), nombre, url));
        }

        fotasParaRepo = fotosConUrl;
        var id = await _repo.CrearAsync(detalle, fotasParaRepo);

        await GenerarPdfYNotificarAsync(id, codigo, fotosBytes.Select(f => f.Bytes).ToList());

        // Evaluar inhabilitación SSOMA
        await _inhabilitacion.EvaluarTrasBmonestacionAsync(req.WorkerId, req.TipoSancionId, userId);

        // Retiro definitivo del proyecto: pasa también a la lista negra real (Habilitación)
        if (req.TipoSancionId == TIPO_SANCION_RETIRO_DEFINITIVO)
        {
            var detalleCreado = await _repo.GetDetalleAsync(id);
            if (detalleCreado is not null)
                await RegistrarEnListaNegraAsync(detalleCreado, userId);
        }

        return new AmonestacionCreadaDto { Id = id, Codigo = codigo };
    }

    private async Task RegistrarEnListaNegraAsync(AmonestacionDetalleDto detalle, int userId)
    {
        try
        {
            // La razón real de la sanción (Descripcion) es obligatoria al crear la amonestación
            // (validado en el frontend y, ahora, también en CrearAsync), así que nunca debería
            // llegar vacía acá — pero si pasa, no se registra en la lista negra con un motivo
            // genérico: se loguea para revisión manual en vez de crear un registro sin sustento.
            if (string.IsNullOrWhiteSpace(detalle.Descripcion))
            {
                _logger.LogError(
                    "Amonestacion {Id} (retiro definitivo) sin Descripcion — no se registra en lista negra automáticamente, requiere revisión manual.",
                    detalle.Id);
                return;
            }

            await _restringido.CreateAsync(new TrabajadorRestringidoCreateDto
            {
                WorkerId         = detalle.WorkerId,
                Dni              = detalle.WorkerDni,
                ApellidoNombre   = detalle.WorkerNombre,
                Motivo           = $"Retiro definitivo del proyecto — {detalle.Descripcion}",
                ProyectoOrigen   = detalle.ProyectoNombre,
                FechaRestriccion = DateOnly.FromDateTime(detalle.Fecha),
                Tipo             = "SANCION",
            }, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registrando trabajador restringido para amonestacion {Id}", detalle.Id);
            throw;
        }
    }

    public async Task<AmonestacionCreadaDto> ConfirmarAsync(int id, int userId)
    {
        var detalle = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Amonestación no encontrada.", 404);

        if (detalle.Estado != "Borrador")
            throw new AbrilException("Solo se puede confirmar un borrador.", 400);

        // Leer bytes de fotos desde base64 guardado en BD
        var fotosBytes = await _repo.GetFotosBytesAsync(id);

        // Subir fotos a SharePoint ahora que tenemos los bytes
        using var ctx = _factory.CreateDbContext();
        var fotosEntidad = await ctx.SsomaAmonestacionFotos
            .Where(f => f.AmonestacionId == id)
            .OrderBy(f => f.Orden)
            .ToListAsync();

        for (int i = 0; i < fotosEntidad.Count && i < fotosBytes.Count; i++)
        {
            try
            {
                var (bytes, nombre) = fotosBytes[i];
                var ext = Path.GetExtension(nombre).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var fileName = $"{detalle.Codigo}_foto_{i + 1}{ext}";
                using var stream = new MemoryStream(bytes);
                var url = await _sharePoint.SubirArchivoEnRutaAsync(
                    stream, fileName, "amonestacion-pdf",
                    $"Amonestaciones/{DateTime.UtcNow:yyyy}/Fotos");
                fotosEntidad[i].Url = url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error subiendo foto al confirmar amonestacion {Id}", id);
            }
        }
        await ctx.SaveChangesAsync();

        // Cambiar estado a Registrada
        await _repo.ConfirmarEstadoAsync(id);

        // Generar PDF y notificar
        await GenerarPdfYNotificarAsync(id, detalle.Codigo, fotosBytes.Select(f => f.Bytes).ToList());

        // Evaluar inhabilitación SSOMA al confirmar
        var detalleConf = await _repo.GetDetalleAsync(id);
        if (detalleConf is not null)
        {
            await _inhabilitacion.EvaluarTrasBmonestacionAsync(detalleConf.WorkerId, detalleConf.TipoSancionId, userId);

            // Retiro definitivo del proyecto: pasa también a la lista negra real (Habilitación)
            if (detalleConf.TipoSancionId == TIPO_SANCION_RETIRO_DEFINITIVO)
                await RegistrarEnListaNegraAsync(detalleConf, userId);
        }

        return new AmonestacionCreadaDto { Id = id, Codigo = detalle.Codigo };
    }

    public async Task EditarAsync(int id, AmonestacionEditRequest req, int userId)
    {
        if (string.IsNullOrWhiteSpace(req.MotivoEdicion) || req.MotivoEdicion.Trim().Length < 10)
            throw new AbrilException("Debe indicar el motivo de la corrección (mínimo 10 caracteres) — queda en el registro de auditoría.", 400);

        if (req.PuntosInfraccion is < 0 or > 10)
            throw new AbrilException("Los puntos por infracción deben estar entre 0 y 10.", 400);

        var (workerId, cambiosResumen) = await _repo.EditarAsync(id, req, userId);

        using var ctx = _factory.CreateDbContext();
        ctx.WorkerEvento.Add(new Abril_Backend.Features.Habilitacion.Infrastructure.Models.WorkerEvento
        {
            WorkerId    = workerId,
            TipoEvento  = "EdicionAmonestacion",
            Descripcion = $"Amonestación {id} corregida. Motivo: {req.MotivoEdicion.Trim()}. Cambios: {cambiosResumen}",
            UsuarioId   = userId > 0 ? userId : null,
            CreatedAt   = DateTime.UtcNow,
        });
        await ctx.SaveChangesAsync();

        // La corrección puede subir o bajar los puntos netos del trabajador: reevaluar
        // el bloqueo SSOMA (puede desbloquear si ya no llega al umbral, o bloquear si ahora sí).
        await _inhabilitacion.ReevaluarAsync(workerId, userId);
    }

    private async Task<byte[]?> ResolveLogoAsync(AmonestacionDetalleDto detalle)
    {
        if (detalle.EsEmpresaAbril)
            return File.Exists(_logoPath) ? await File.ReadAllBytesAsync(_logoPath) : null;

        if (!string.IsNullOrEmpty(detalle.EmpresaLogoUrl))
        {
            try
            {
                var dl = await _sharePoint.GetDownloadUrlAsync(detalle.EmpresaLogoUrl, "empresa-logos");
                var url = !string.IsNullOrEmpty(dl) ? dl : detalle.EmpresaLogoUrl;
                using var http = new System.Net.Http.HttpClient();
                var bytes = await http.GetByteArrayAsync(url);
                if (bytes.Length > 0) return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo obtener logo de contratista para amonestacion");
            }
        }
        return null;
    }

    private async Task GenerarPdfYNotificarAsync(int id, string codigo, List<byte[]> fotosBytes)
    {
        // Todo lo de aquí es post-guardado: la amonestación ya existe en BD. Si algo falla
        // (lectura del detalle, PDF, SharePoint, correo) se registra y se sigue — nunca se
        // propaga, porque devolver error al front hace que el usuario reintente y duplique.
        try
        {
            var detalleCompleto = await _repo.GetDetalleAsync(id);
            if (detalleCompleto is null) return;

            var logoBytes = await ResolveLogoAsync(detalleCompleto);

            var pdfBytes = AmonestacionPdfService.GenerarPdf(detalleCompleto, fotosBytes, logoBytes);

            try
            {
                var pdfFileName = $"Amonestacion_{codigo}.pdf";
                var carpeta = $"Amonestaciones/{DateTime.UtcNow:yyyy}";
                using var stream = new MemoryStream(pdfBytes);
                var pdfUrl = await _sharePoint.SubirArchivoEnRutaAsync(
                    stream, pdfFileName, "amonestacion-pdf", carpeta);
                if (!string.IsNullOrEmpty(pdfUrl))
                    await _repo.GuardarPdfUrlAsync(id, pdfUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error subiendo PDF a SharePoint para amonestacion {Id}", id);
            }

            _ = Task.Run(async () =>
            {
                try { await _notif.NotificarAmonestacionAsync(detalleCompleto, pdfBytes); }
                catch (Exception ex) { _logger.LogWarning(ex, "Error notificando amonestacion {Id}", id); }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error generando PDF amonestacion {Id}", id);
        }
    }

    public async Task<AmonestacionPagedResult<AmonestacionListItemDto>> GetListAsync(AmonestacionListQuery q)
    {
        var (items, total) = await _repo.GetListAsync(q);
        return new AmonestacionPagedResult<AmonestacionListItemDto>
        {
            Items    = items,
            Total    = total,
            Page     = q.Page,
            PageSize = q.PageSize
        };
    }

    public Task<AmonestacionDetalleDto?> GetDetalleAsync(int id) => _repo.GetDetalleAsync(id);

    public Task<AmonestacionDashboardDto> GetDashboardAsync(int? empresaIdContratista = null) =>
        _repo.GetDashboardAsync(empresaIdContratista);

    public Task<WorkerPuntajeDto?> GetPuntajeWorkerAsync(int workerId) =>
        _repo.GetPuntajeWorkerAsync(workerId);

    public async Task<(byte[]? Bytes, string? RedirectUrl)> GetPdfAsync(int id)
    {
        var detalle = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Amonestación no encontrada.", 404);

        // Si ya existe PDF generado en SharePoint, redirigir directamente (no regenerar)
        if (!string.IsNullOrEmpty(detalle.PdfUrl))
        {
            var downloadUrl = await _sharePoint.GetDownloadUrlAsync(detalle.PdfUrl, "amonestacion-pdf");
            if (!string.IsNullOrEmpty(downloadUrl))
                return (null, downloadUrl);
        }

        // Fallback: generar en memoria
        var logoBytes = await ResolveLogoAsync(detalle);
        var fotosConNombre = await _repo.GetFotosBytesAsync(id);

        if (fotosConNombre.Count == 0)
        {
            using var http = new System.Net.Http.HttpClient();
            foreach (var foto in detalle.Fotos.Where(f => !string.IsNullOrEmpty(f.Url)))
            {
                try
                {
                    var downloadUrl = await _sharePoint.GetDownloadUrlAsync(foto.Url, "amonestacion-pdf");
                    if (!string.IsNullOrEmpty(downloadUrl))
                    {
                        var bytes = await http.GetByteArrayAsync(downloadUrl);
                        if (bytes.Length > 0) fotosConNombre.Add((bytes, foto.NombreArchivo ?? "foto.jpg"));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "No se pudo descargar foto {Url} para PDF amonestacion {Id}", foto.Url, id);
                }
            }
        }

        return (AmonestacionPdfService.GenerarPdf(detalle, fotosConNombre.Select(f => f.Bytes).ToList(), logoBytes), null);
    }

    public async Task CerrarAsync(int id, AmonestacionCerrarRequest req)
    {
        var detalle = await _repo.GetDetalleAsync(id)
            ?? throw new AbrilException("Amonestación no encontrada.", 404);

        if (detalle.Estado == "Cerrada")
            throw new AbrilException("La amonestación ya está cerrada.", 400);

        if (string.IsNullOrWhiteSpace(req.DocumentoFirmadoBase64))
            throw new AbrilException("Debe adjuntar el documento firmado.", 400);

        // Decodificar imagen/PDF firmado
        var base64 = req.DocumentoFirmadoBase64.Contains(',')
            ? req.DocumentoFirmadoBase64.Split(',')[1]
            : req.DocumentoFirmadoBase64;
        var bytes = Convert.FromBase64String(base64);

        // Subir a SharePoint
        var ext = Path.GetExtension(req.NombreArchivo).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var fileName = $"Firmado_{detalle.Codigo}{ext}";
        var carpeta = $"Amonestaciones/{DateTime.UtcNow:yyyy}/Firmados";

        string documentoUrl;
        try
        {
            using var stream = new MemoryStream(bytes);
            documentoUrl = await _sharePoint.SubirArchivoEnRutaAsync(
                stream, fileName, "amonestacion-pdf", carpeta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subiendo documento firmado amonestacion {Id}", id);
            throw new AbrilException("No se pudo subir el documento firmado.", 502);
        }

        await _repo.CerrarAsync(id, documentoUrl);
    }
}
