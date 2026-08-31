namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos
{
    // ── Lectura ──────────────────────────────────────────────────────────────

    public class CargaDiariaDto
    {
        public DateOnly Fecha { get; set; }
        /// <summary>Categoría de "Evidencias" en esta respuesta ("GENERAL" | "PROCURA") — el resto
        /// del payload (grid, catálogos, bloqueos) no está scoped por categoría, es siempre el mismo.</summary>
        public string Categoria { get; set; } = "GENERAL";
        public bool EsEditable { get; set; }
        public List<ZonaDto> Zonas { get; set; } = new();
        public List<ActividadCatalogoDto> Actividades { get; set; } = new();
        public List<CausaCatalogoDto> Causas { get; set; } = new();
        public List<CeldaDto> Celdas { get; set; } = new();
        public List<EvidenciaFotoDto> Evidencias { get; set; } = new();
        public List<RestriccionDto> RestriccionesActivas { get; set; } = new();
    }

    public class ActividadCatalogoDto
    {
        public int Id { get; set; }
        public int MacroActividadId { get; set; }
        public string MacroActividadNombre { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class CausaCatalogoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    /// <summary>Solo se listan las celdas con registro. Ausente = "sin cargar" (no confundir con 0%).</summary>
    public class CeldaDto
    {
        public int ZonaId { get; set; }
        public int NivelId { get; set; }
        public int SectorId { get; set; }
        public int ActividadId { get; set; }
        public decimal PorcentajeAvance { get; set; }
        public int? CausaId { get; set; }
        public string? CausaNombre { get; set; }
        public string? CausaDetalle { get; set; }
    }

    public class EvidenciaFotoDto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
        public DateTimeOffset CreatedDateTime { get; set; }
    }

    // ── Escritura ────────────────────────────────────────────────────────────

    public class CargaDiariaUpdateDto
    {
        public List<CeldaUpdateDto> Celdas { get; set; } = new();
    }

    /// <summary>Upsert por la tupla natural (zona, nivel, sector, actividad) + fecha de la URL — no viaja Id.
    /// PorcentajeAvance debe ser uno de PlaneamientoBimCargaDiariaService.PorcentajesValidos (0/25/50/75/100).</summary>
    public class CeldaUpdateDto
    {
        public int ZonaId { get; set; }
        public int NivelId { get; set; }
        public int SectorId { get; set; }
        public int ActividadId { get; set; }
        public decimal PorcentajeAvance { get; set; }
        public int? CausaId { get; set; }
        public string? CausaDetalle { get; set; }
    }
}
