namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    /// <summary>Catálogo de tipos de requisito (documentos) que se gestionan por vecino.</summary>
    public class VecinoRequisitoTipo
    {
        public int VecinoRequisitoTipoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Orden { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Catálogo de estados de requisito: No subido, Subido, No aplica.</summary>
    public class VecinoRequisitoEstado
    {
        public int VecinoRequisitoEstadoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    /// <summary>Requisito de un vecino: un registro por vecino + tipo, con su estado y archivo.</summary>
    public class VecinoRequisito
    {
        public int VecinoRequisitoId { get; set; }

        public int VecinoId { get; set; }
        public Vecino? Vecino { get; set; }

        public int VecinoRequisitoTipoId { get; set; }
        public VecinoRequisitoTipo? Tipo { get; set; }

        public int VecinoRequisitoEstadoId { get; set; }
        public VecinoRequisitoEstado? Estado { get; set; }

        public string? ArchivoUrl { get; set; }
        public string? OriginalFileName { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
