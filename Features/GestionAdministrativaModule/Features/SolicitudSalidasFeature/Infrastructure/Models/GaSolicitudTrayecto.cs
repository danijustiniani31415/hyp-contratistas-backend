namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models
{
    /// <summary>
    /// Un tramo individual de una solicitud de salida — una solicitud puede tener N trayectos
    /// (encadenados: el origen del trayecto N+1 suele ser el destino del N, pero se almacena
    /// independientemente para flexibilidad).
    /// </summary>
    public class GaSolicitudTrayecto
    {
        public int Id { get; set; }
        public int SolicitudId { get; set; }
        /// <summary>Orden secuencial dentro de la solicitud (0-based).</summary>
        public int Orden { get; set; }
        public TimeOnly HoraSalida { get; set; }
        public TimeOnly? HoraRetorno { get; set; }
        public int? MotivoId { get; set; }
        public string? MotivoLibre { get; set; }
        /// <summary>Detalle que acompaña al motivo del catálogo cuando este tiene
        /// requiere_motivo_adicional = true. No confundir con <see cref="MotivoLibre"/>,
        /// que es la vía "Otro motivo" (motivo fuera del catálogo, con MotivoId nulo).</summary>
        public string? MotivoAdicional { get; set; }
        public int? LugarOrigenId { get; set; }
        public string? LugarOrigenLibre { get; set; }
        public int? LugarDestinoId { get; set; }
        public string? LugarDestinoLibre { get; set; }

        // ── Documento adjunto (prueba) ───────────────────────────────────────
        // Se llena al crear la solicitud cuando el motivo elegido tiene
        // requiere_adjunto = true. El archivo vive en la carpeta configurada de
        // SharePoint/OneDrive (ga_adjunto_folder).
        /// <summary>webUrl del archivo en SharePoint/OneDrive (para abrirlo desde el detalle).</summary>
        public string? AdjuntoUrl { get; set; }
        public string? AdjuntoItemId { get; set; }
        public string? AdjuntoDriveId { get; set; }
        public string? AdjuntoFilename { get; set; }
        public DateTimeOffset? AdjuntoUploadedAt { get; set; }
    }
}
