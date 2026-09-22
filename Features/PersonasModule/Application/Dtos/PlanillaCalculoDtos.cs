namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    // ── Conceptos de planilla (reglas configurables, estilo salary.rule de Odoo) ──────────────

    public class ConceptoPlanillaDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public string? CategoriaLaboral { get; set; }
        public string FormaCalculo { get; set; } = null!;
        public decimal Valor { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    public class ConceptoPlanillaCreateDto
    {
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public string? CategoriaLaboral { get; set; }
        public string FormaCalculo { get; set; } = null!;
        public decimal Valor { get; set; }
    }

    public class ConceptoPlanillaUpdateDto
    {
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public string? CategoriaLaboral { get; set; }
        public string FormaCalculo { get; set; } = null!;
        public decimal Valor { get; set; }
        public bool Activo { get; set; } = true;
    }

    // ── Períodos de planilla ────────────────────────────────────────────────────────────────

    public class PlanillaPeriodoCreateDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public int? ProyectoId { get; set; }
    }

    public class PlanillaPeriodoListItemDto
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string? ProyectoNombre { get; set; }
        public string Estado { get; set; } = null!;
        public int CantidadPersonas { get; set; }
        public decimal? TotalNeto { get; set; }
        public DateTimeOffset CreadoEn { get; set; }
    }

    public class PlanillaDetalleConceptoDto
    {
        public string ConceptoNombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public decimal Monto { get; set; }
    }

    public class PlanillaDetalleDto
    {
        public long Id { get; set; }
        public int PersonaId { get; set; }
        public string PersonaNombre { get; set; } = null!;
        public string? CategoriaLaboral { get; set; }
        public decimal? SueldoBase { get; set; }
        public decimal? Jornal { get; set; }
        public decimal DiasTrabajados { get; set; }
        public decimal DiasFalta { get; set; }
        public decimal TotalIngresos { get; set; }
        public decimal TotalDescuentos { get; set; }
        public decimal TotalAportesEmpleador { get; set; }
        public decimal NetoPagar { get; set; }
        public List<PlanillaDetalleConceptoDto> Conceptos { get; set; } = new();
    }

    public class PlanillaPeriodoDetailDto
    {
        public int Id { get; set; }
        public int Anio { get; set; }
        public int Mes { get; set; }
        public string? ProyectoNombre { get; set; }
        public string Estado { get; set; } = null!;
        public DateTimeOffset? CalculadoEn { get; set; }
        public List<PlanillaDetalleDto> Detalles { get; set; } = new();
        /// <summary>Personas con vínculo vigente en el período que no se pudieron calcular por
        /// falta de categoría laboral — mismo criterio que el dashboard de datos faltantes.</summary>
        public List<string> PersonasOmitidas { get; set; } = new();
    }
}
