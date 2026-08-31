using Abril_Backend.Features.SsomaModule.OptFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Models;

// Catálogo GLOBAL, compartido entre todos los PETS, para las secciones que van con
// checkbox en vez de texto libre: Marco Legal, EPP (básico|especifico|emergencia) y
// Recursos (equipo|herramienta|material). "Eliminar" un ítem del catálogo es
// desactivarlo (Activo = false): deja de ofrecerse para futuras selecciones pero no
// rompe los PETS que ya lo tenían seleccionado.
public class SsomaCatalogoItem
{
    public int Id { get; set; }

    // marco_legal | epp | recurso
    public string Grupo { get; set; } = string.Empty;

    // epp: basico|especifico|emergencia ; recurso: equipo|herramienta|material ; marco_legal: null
    public string? Tipo { get; set; }

    public string Descripcion { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

// Selección de un ítem para un PETS puntual: o viene del catálogo global
// (CatalogoItemId), o es propio de este PETS y no existe en el catálogo
// (CatalogoItemId null + DescripcionPersonalizada). "Eliminar" para este PETS es
// desactivar esta fila sin tocar el catálogo global.
public class SsomaPetItemSeleccionado
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string Grupo { get; set; } = string.Empty;
    public string? Tipo { get; set; }
    public int? CatalogoItemId { get; set; }
    public string? DescripcionPersonalizada { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaPet? Pet { get; set; }
    public SsomaCatalogoItem? CatalogoItem { get; set; }
}

// Bloque de texto único por sección narrativa (Introducción, Alcance, Objetivo,
// Definiciones, Restricciones) — a diferencia de Procedimiento/Responsabilidades,
// estas secciones son prosa sin estructura real, así que no usan el árbol de
// SsomaPetPaso. Una fila por (PetId, Seccion).
public class SsomaPetSeccionTexto
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string Seccion { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }

    public SsomaPet? Pet { get; set; }
}

// Elaborado por / Revisado por / Aprobado por — tal cual la tabla de firmas al pie
// del PETS en Word. Una fila por (PetId, Rol).
public class SsomaPetFirma
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string Rol { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Cargo { get; set; }
    public DateOnly? Fecha { get; set; }
    public string? FirmaUrl { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public SsomaPet? Pet { get; set; }
}

public class SsomaPetAnexo
{
    public int Id { get; set; }
    public int PetId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ArchivoUrl { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaPet? Pet { get; set; }
}
