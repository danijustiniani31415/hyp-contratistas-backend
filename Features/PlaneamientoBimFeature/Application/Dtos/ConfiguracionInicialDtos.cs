namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos
{
    // ── Lectura ──────────────────────────────────────────────────────────────

    public class ConfiguracionInicialDto
    {
        public List<ZonaDto> Zonas { get; set; } = new();
        public List<FaseDto> Fases { get; set; } = new();
        public int? ResponsableId { get; set; }
        public string? ResponsableNombre { get; set; }
        public decimal? MetaPpc { get; set; }
    }

    public class ZonaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public List<NivelDto> Niveles { get; set; } = new();
    }

    public class NivelDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string? TipoEstructura { get; set; }
        /// <summary>Efectivo: sectores propios de este nivel + sectores
        /// compartidos de la zona (zona_nivel_id NULL).</summary>
        public List<SectorDto> Sectores { get; set; } = new();
    }

    public class SectorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class ResponsableBimLookupDto
    {
        public int Id { get; set; }
        public string ApellidoNombre { get; set; } = string.Empty;
    }

    /// <summary>Id de la fila bim_proyecto_fase (no del catálogo bim_fase). Las 5 fases siempre existen; solo se editan sus fechas.</summary>
    public class FaseDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFinMeta { get; set; }
    }

    // ── Escritura ────────────────────────────────────────────────────────────

    public class ConfiguracionInicialUpdateDto
    {
        public List<ZonaUpdateDto> Zonas { get; set; } = new();
        public List<FaseUpdateDto> Fases { get; set; } = new();
        public int? ResponsableId { get; set; }
        public string? ResponsableNombre { get; set; }
        public decimal? MetaPpc { get; set; }
    }

    /// <summary>Id null/0 = zona nueva a crear; Id existente = actualizar esa fila.</summary>
    public class ZonaUpdateDto
    {
        public int? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public List<NivelUpdateDto> Niveles { get; set; } = new();
        /// <summary>Sectores compartidos por TODOS los niveles de esta zona
        /// (zona_nivel_id NULL al persistir). Separado de los sectores
        /// propios de cada nivel (ver NivelUpdateDto.Sectores).</summary>
        public List<SectorUpdateDto> SectoresCompartidos { get; set; } = new();
    }

    public class NivelUpdateDto
    {
        public int? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public string? TipoEstructura { get; set; }
        /// <summary>Sectores exclusivos de este nivel (zona_nivel_id = este nivel).</summary>
        public List<SectorUpdateDto> Sectores { get; set; } = new();
    }

    public class SectorUpdateDto
    {
        public int? Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    /// <summary>Id obligatorio: debe ser una de las 5 filas bim_proyecto_fase ya creadas para el proyecto. No se crean ni eliminan fases desde acá.</summary>
    public class FaseUpdateDto
    {
        public int Id { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFinMeta { get; set; }
    }
}
