namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Models
{
    public class VecinoCompromiso
    {
        public int VecinoCompromisoId { get; set; }

        public int VecinoSolicitudId { get; set; }
        public VecinoSolicitud? Solicitud { get; set; }

        public string Descripcion { get; set; } = null!;
        public bool EsCritico { get; set; }

        public int VecinoCompromisoEstadoId { get; set; }
        public VecinoCompromisoEstado? Estado { get; set; }

        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }

        /// <summary>
        /// Fecha límite impuesta por la municipalidad/fiscalización (anterior a la fecha fin del
        /// compromiso). Cuando está presente, el compromiso se cataloga como "con fecha límite por
        /// municipalidad/fiscalización" y debe priorizarse.
        /// </summary>
        public DateOnly? FechaFinMunicipalidad { get; set; }

        /// <summary>Observaciones libres del compromiso (opcional).</summary>
        public string? Observaciones { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }

        public List<VecinoCompromisoEntregable> Entregables { get; set; } = new();
    }

    /// <summary>Archivo de "normativas" de un compromiso. Un compromiso puede tener varios.</summary>
    public class VecinoCompromisoNormativa
    {
        public int VecinoCompromisoNormativaId { get; set; }

        public int VecinoCompromisoId { get; set; }
        public VecinoCompromiso? Compromiso { get; set; }

        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }

    public class VecinoCompromisoEntregable
    {
        public int VecinoCompromisoEntregableId { get; set; }

        public int VecinoCompromisoId { get; set; }
        public VecinoCompromiso? Compromiso { get; set; }

        public int VecinoEntregableTipoId { get; set; }
        public VecinoEntregableTipo? Tipo { get; set; }

        public int VecinoEntregableEstadoId { get; set; }
        public VecinoEntregableEstado? Estado { get; set; }

        /// <summary>Archivo adjunto del entregable (opcional).</summary>
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
