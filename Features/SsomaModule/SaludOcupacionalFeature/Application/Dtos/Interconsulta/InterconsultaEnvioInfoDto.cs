namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    /// <summary>Datos crudos de una interconsulta + trabajador + proyecto/empresa, usados solo
    /// para armar los correos de recordatorio (no se expone directamente al frontend).</summary>
    public class InterconsultaEnvioInfoDto
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? Especialidad { get; set; }
        public DateOnly FechaDerivacion { get; set; }
        public int DiasPendiente { get; set; }
        public string? WorkerEmailCorporativo { get; set; }
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficina { get; set; }
        public string? ContrataCasa { get; set; }
        public string? Jefatura { get; set; }
        public string? JefaturaEmail { get; set; }

        /// <summary>True para Oficina Central: no tiene proyecto real, su unidad organizativa es la jefatura.</summary>
        public bool EsOficinaCentral => ObraOficinaStaffId == Abril_Backend.Shared.Constants.ObraOficinaStaffIds.OficinaCentral;

        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public string? ProyectoEmailCoordAdmin { get; set; }
        public string? ProyectoEmailResidente { get; set; }
        public string? ProyectoEmailResponsable { get; set; }
        public string? ProyectoEmailRrhh { get; set; }
        public string? ProyectoEmailCoordSsoma { get; set; }

        public int? ContributorId { get; set; }
        public string? ContributorNombre { get; set; }
        public string? ContributorEmailAdministrador { get; set; }

        /// <summary>Staff/Oficina Central con correo corporativo propio → correo individual.
        /// Sin correo propio (típicamente Obra/Contratista) → se agrupa en el correo consolidado del proyecto.</summary>
        public bool TieneCorreoPropio => !string.IsNullOrWhiteSpace(WorkerEmailCorporativo);
    }
}
