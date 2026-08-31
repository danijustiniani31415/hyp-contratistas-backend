using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.OptFeature.Infrastructure.Models;

public class SsomaOpt
{
    public int Id { get; set; }
    public int ProyectoId { get; set; }
    public int? PetId { get; set; }
    public DateTime Fecha { get; set; }
    public string TipoObservacion { get; set; } = string.Empty;
    public bool CuentaConPet { get; set; }
    public string? Area { get; set; }
    public bool SeInformaTrabajador { get; set; }
    public int? ObservadorId { get; set; }
    public string? ObservadorNombre { get; set; }
    public string? ObservadorCargo { get; set; }
    public string? FirmaObservadorUrl { get; set; }
    public bool SeFelicito { get; set; }
    public bool SeRecibieronComentarios { get; set; }
    public bool SeRetroalimento { get; set; }
    [Column("se_obtuvo_compromiso")]
    public bool SeObtuvoCCompromiso { get; set; }
    public string? AccionRequerida { get; set; }
    public string? AccionObservacion { get; set; }
    public int TotalPasos { get; set; }
    public int TotalSeguros { get; set; }
    public int TotalInseguros { get; set; }
    public decimal? ScorePct { get; set; }
    public string Estado { get; set; } = "Completado";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }

    public Project? Proyecto { get; set; }
    public SsomaPet? Pet { get; set; }
    public Worker? Observador { get; set; }
    public ICollection<SsomaOptTrabajador> Trabajadores { get; set; } = [];
    public ICollection<SsomaOptVerificacion> Verificaciones { get; set; } = [];
    public ICollection<SsomaOptPaso> Pasos { get; set; } = [];
    public ICollection<SsomaOptFotoArea> FotosArea { get; set; } = [];
}

public class SsomaOptTrabajador
{
    public int Id { get; set; }
    public int OptId { get; set; }
    public int TrabajadorId { get; set; }
    public string? TipoTrabajador { get; set; }
    public string? TiempoEnObra { get; set; }
    public string? AniosExperiencia { get; set; }
    public string? FirmaTrabajadorUrl { get; set; }

    public SsomaOpt? Opt { get; set; }
    public Worker? Trabajador { get; set; }
}

public class SsomaPet
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Codigo { get; set; }
    public string? SharepointUrl { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<SsomaPetPaso> Pasos { get; set; } = [];
}

// Catálogo de pasos del PETS: piloto para que OPT (y a futuro otras herramientas)
// jalen automáticamente la estructura del PETS en vez de tipearla a mano cada vez.
// "Orden" es solo la clave de ordenamiento interna — el número que se muestra al
// usuario ("Paso 4") se calcula por posición, nunca se guarda como texto fijo, para
// que insertar un paso en medio no requiera renumerar nada a mano.
public class SsomaPetPaso
{
    public int Id { get; set; }
    public int PetId { get; set; }

    // Sección del documento a la que pertenece este árbol: procedimiento (original) |
    // introduccion | alcance | objetivo | definiciones | responsabilidades |
    // restricciones. Cada sección es un árbol independiente dentro del mismo PetId —
    // "hermanos" se agrupan por (PetId, Seccion, ParentId).
    public string Seccion { get; set; } = "procedimiento";

    // Árbol: null = nivel superior de la sección. "Orden" es la posición entre
    // los HERMANOS del mismo ParentId (no global) — así insertar/reordenar dentro de
    // un subtítulo no toca el orden de otro subtítulo.
    public int? ParentId { get; set; }

    // subtitulo | paso | letra | guion — controla cómo se numera/viñetea al mostrarlo,
    // nunca se guarda el número/letra como texto fijo.
    public string Tipo { get; set; } = "paso";

    public string Descripcion { get; set; } = string.Empty;
    public string? ImagenUrl { get; set; }
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public SsomaPet? Pet { get; set; }
    public SsomaPetPaso? Parent { get; set; }
    public ICollection<SsomaPetPaso> Hijos { get; set; } = [];
}

public class SsomaOptCriterioVerificacion
{
    public int Id { get; set; }
    public string Pregunta { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
}

public class SsomaOptVerificacion
{
    public int Id { get; set; }
    public int OptId { get; set; }
    public int CriterioId { get; set; }
    public bool Resultado { get; set; }

    public SsomaOpt? Opt { get; set; }
    public SsomaOptCriterioVerificacion? Criterio { get; set; }
}

public class SsomaOptFotoArea
{
    public int Id { get; set; }
    public int OptId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Orden { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SsomaOpt? Opt { get; set; }
}

public class SsomaOptPaso
{
    public int Id { get; set; }
    public int OptId { get; set; }
    public string NumeroDisplay { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public int Nivel { get; set; } = 1;
    public string? Resultado { get; set; }
    public string? DesviacionObservada { get; set; }
    public int Orden { get; set; }

    public SsomaOpt? Opt { get; set; }
}
