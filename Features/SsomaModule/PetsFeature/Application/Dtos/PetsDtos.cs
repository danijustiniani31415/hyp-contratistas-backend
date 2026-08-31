namespace Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;

public class PetListItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public bool Activo { get; set; }
    public int TotalPasos { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PetPasoDto
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public string Tipo { get; set; } = "paso";
    public string Descripcion { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public int Orden { get; set; }
}

public class PetDetalleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? SharepointUrl { get; set; }
    public bool Activo { get; set; }

    // "Procedimiento (paso a paso)" — se mantiene aparte por compatibilidad, ya que
    // OPT jala este mismo dato vía GET /pets/{id}/pasos.
    public List<PetPasoDto> Pasos { get; set; } = [];

    // Responsabilidades sí tiene estructura real (subtítulo por cargo, con ítems
    // debajo) — es el único árbol además de Procedimiento.
    public List<PetPasoDto> Responsabilidades { get; set; } = [];

    // Secciones narrativas — un solo bloque de texto cada una, por clave:
    // introduccion | alcance | objetivo | definiciones | restricciones.
    // "" si todavía no se ha escrito nada.
    public Dictionary<string, string> SeccionesTexto { get; set; } = [];

    public List<PetItemSeleccionadoDto> MarcoLegal { get; set; } = [];
    public List<PetItemSeleccionadoDto> Epp { get; set; } = [];
    public List<PetItemSeleccionadoDto> Recursos { get; set; } = [];
    public List<PetAnexoDto> Anexos { get; set; } = [];

    // Elaborado por / Revisado por / Aprobado por — siempre las 3 claves presentes.
    public Dictionary<string, PetFirmaDto> Firmas { get; set; } = [];
}

public class CrearPetRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? SharepointUrl { get; set; }
}

public class ActualizarPetRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? SharepointUrl { get; set; }
    public bool Activo { get; set; } = true;
}

public class CrearPetPasoRequest
{
    public string Descripcion { get; set; } = string.Empty;

    // procedimiento (default) | responsabilidades — las únicas dos secciones en
    // árbol; el resto son bloques de texto único (ver SeccionesTexto).
    public string Seccion { get; set; } = "procedimiento";

    // null = nivel superior de la sección. Si se indica, el nuevo paso se crea
    // como hijo de ese subtítulo (a cualquier nivel de anidamiento).
    public int? ParentId { get; set; }

    // subtitulo | paso | letra | guion
    public string Tipo { get; set; } = "paso";

    // 1-based, posición ENTRE LOS HERMANOS del mismo ParentId. Null o fuera de rango
    // = se agrega al final de ese grupo. Todo lo que esté en esa posición o después
    // se corre +1 — así insertar "entre el 3 y el 4" no requiere renumerar a mano.
    public int? Posicion { get; set; }
}

public class ActualizarPetPasoRequest
{
    public string Descripcion { get; set; } = string.Empty;
    public string Tipo { get; set; } = "paso";
}

public class ReordenarPasosRequest
{
    public string Seccion { get; set; } = "procedimiento";

    // null = reordena los del nivel superior. Si se indica, reordena solo los
    // hijos de ese subtítulo — cada grupo de hermanos se reordena por separado.
    public int? ParentId { get; set; }

    // Ids de los pasos ACTIVOS de ese grupo de hermanos, en el nuevo orden deseado.
    public List<int> PasoIds { get; set; } = [];
}

// ── Secciones de texto único (Introducción / Alcance / Objetivo / Definiciones / Restricciones) ──

public class ActualizarSeccionTextoRequest
{
    public string Contenido { get; set; } = string.Empty;
}

// ── Catálogo (Marco Legal / EPP / Recursos) ──────────────────────────────────────

public class CatalogoItemDto
{
    public int Id { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public int Orden { get; set; }
}

public class CrearCatalogoItemRequest
{
    // marco_legal | epp | recurso
    public string Grupo { get; set; } = string.Empty;

    // epp: basico|especifico|emergencia ; recurso: equipo|herramienta|material ; marco_legal: null
    public string? Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
}

public class PetItemSeleccionadoDto
{
    public int Id { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public int? CatalogoItemId { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool EsPersonalizado { get; set; }
    public int Orden { get; set; }
}

// Selecciona un ítem existente del catálogo global para este PETS.
public class SeleccionarItemCatalogoRequest
{
    public string Grupo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public int CatalogoItemId { get; set; }
}

// Agrega un ítem que no está en el catálogo. Si AgregarAlCatalogoGlobal es true,
// además de seleccionarlo para este PETS lo crea en el catálogo global para que
// otros PETS puedan elegirlo después.
public class AgregarItemPersonalizadoRequest
{
    public string Grupo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public bool AgregarAlCatalogoGlobal { get; set; }
}

// ── Anexos ────────────────────────────────────────────────────────────────────

public class PetAnexoDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ArchivoUrl { get; set; } = string.Empty;
    public int Orden { get; set; }
}

// ── Firmas (Elaborado por / Revisado por / Aprobado por) ────────────────────────

public class PetFirmaDto
{
    public string Rol { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Cargo { get; set; }
    public DateOnly? Fecha { get; set; }
    public string? FirmaUrl { get; set; }
}

public class ActualizarFirmaRequest
{
    public string? Nombre { get; set; }
    public string? Cargo { get; set; }
    public DateOnly? Fecha { get; set; }
}
