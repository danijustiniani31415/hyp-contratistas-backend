namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    /// <summary>
    /// Datos que necesita el validador de correo corporativo, resueltos en un solo roundtrip:
    /// la clasificación y el correo actual del trabajador que se está editando (si aplica) y el
    /// trabajador no retirado que ya tiene ese correo asignado (si existe).
    /// </summary>
    public class EmailCorporativoContextoDto
    {
        /// <summary>True si el <c>workerId</c> consultado existe en la tabla workers.</summary>
        public bool WorkerEncontrado { get; set; }

        /// <summary>workers.contrata_casa del trabajador consultado ('Casa' / 'Contratista').</summary>
        public string? WorkerContrataCasa { get; set; }

        /// <summary>workers.obra_oficina del trabajador consultado ('Obra' / 'Staff' / 'Oficina Central').</summary>
        public int? WorkerObraOficinaStaffId { get; set; }

        /// <summary>workers.email_corporativo actualmente guardado para el trabajador consultado.</summary>
        public string? WorkerEmailActual { get; set; }

        /// <summary>person.email (correo de contacto) actualmente guardado para el trabajador consultado.</summary>
        public string? WorkerEmailPersonalActual { get; set; }

        /// <summary>Id del otro trabajador no retirado que ya tiene ese correo, o null si está libre.</summary>
        public int? OcupadoPorWorkerId { get; set; }

        /// <summary>Nombre del otro trabajador que ya tiene ese correo.</summary>
        public string? OcupadoPorNombre { get; set; }

        /// <summary>Documento del otro trabajador que ya tiene ese correo (ayuda a identificarlo).</summary>
        public string? OcupadoPorDni { get; set; }
    }
}
