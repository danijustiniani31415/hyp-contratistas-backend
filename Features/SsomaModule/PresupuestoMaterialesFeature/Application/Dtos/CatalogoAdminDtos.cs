namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

public class TipoMaterialDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

// ─── Sección 1: catálogo normalizado (editable) ───────────────────────────────

public class FamiliaCatalogoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int TipoId { get; set; }
    public string NombreTipo { get; set; } = "";
    public string VariableBase { get; set; } = "";
    public string? UnidadMedida { get; set; }
    public bool PerteneceSsoma { get; set; }
    public bool Activo { get; set; }
}

public class ActualizarFamiliaDto
{
    public string Nombre { get; set; } = "";
    public int TipoId { get; set; }
    public string VariableBase { get; set; } = "";
    public string? UnidadMedida { get; set; }
    public bool PerteneceSsoma { get; set; }
    public bool Activo { get; set; }
}

/// <summary>Crear un ítem nuevo desde el buscador de Revisión, cuando de verdad no existe en el catálogo.</summary>
public class CrearItemCatalogoDto
{
    public string Nombre { get; set; } = "";
    public int FamiliaId { get; set; }
}

/// <summary>Crear una família nueva desde Revisión, cuando el material no encaja en ninguna categoría existente.</summary>
public class CrearFamiliaCatalogoDto
{
    public string Nombre { get; set; } = "";
    public int TipoId { get; set; }
    public string VariableBase { get; set; } = "";
    public string? UnidadMedida { get; set; }
    public bool PerteneceSsoma { get; set; } = true;
}

// ─── Sección 2: materiales sin estandarizar (global, todos los proyectos) ─────

public class MaterialPendienteGlobalDto
{
    public long LineaId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectDescription { get; set; } = "";
    public string RecursoCrudo { get; set; } = "";
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal PrecioTotal { get; set; }
    public DateOnly FechaGuia { get; set; }
    public string? NombreItemSugerido { get; set; }
    public string? NombreFamiliaSugerida { get; set; }
    public int? ItemIdSugerido { get; set; }
    public decimal? ScoreMatch { get; set; }
    public string? MetodoMatch { get; set; }
}

// ─── Sección 3: materiales que no pertenecen a SSOMA (reporte) ───────────────

public class MaterialNoSsomaDto
{
    public long LineaId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectDescription { get; set; } = "";
    public string RecursoCrudo { get; set; } = "";
    public decimal PrecioTotal { get; set; }
    public DateOnly FechaGuia { get; set; }
    public string? EstadoRevision { get; set; }
}
