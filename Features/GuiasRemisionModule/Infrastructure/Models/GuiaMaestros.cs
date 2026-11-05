using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models
{
    /// <summary>Empresa de transporte público (modalidad 01) que se elige al crear una guía.</summary>
    [Table("lb_guia_transportista")]
    public class GuiaTransportista
    {
        public long Id { get; set; }
        public string Ruc { get; set; } = null!;
        public string RazonSocial { get; set; } = null!;
        public string? NroMtc { get; set; }
        public bool Activo { get; set; } = true;
    }

    /// <summary>Vehículo propio (modalidad 02).</summary>
    [Table("lb_guia_vehiculo")]
    public class GuiaVehiculo
    {
        public long Id { get; set; }
        public string Placa { get; set; } = null!;
        public string? NroCirculacion { get; set; }
        public bool Activo { get; set; } = true;
    }

    /// <summary>Conductor (modalidad 02): SUNAT exige documento, apellidos y nombres por separado, y licencia.</summary>
    [Table("lb_guia_conductor")]
    public class GuiaConductor
    {
        public long Id { get; set; }
        public string TipoDoc { get; set; } = "1";
        public string NumDoc { get; set; } = null!;
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string Licencia { get; set; } = null!;
        /// <summary>Si el conductor es un trabajador registrado en Personas (por su DNI); null = conductor externo.</summary>
        public int? PersonaId { get; set; }
        public bool Activo { get; set; } = true;
    }
}
