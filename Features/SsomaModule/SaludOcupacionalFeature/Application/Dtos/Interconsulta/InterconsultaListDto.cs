namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Interconsulta
{
    public class InterconsultaListDto
    {
        public int Id { get; set; }
        public int? EmoId { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? ContributorId { get; set; }
        public string? RazonSocial { get; set; }
        /// <summary>Valor crudo de workers.obra_oficina (Staff, Oficina Central, Obra, Contratista).</summary>
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficina { get; set; }
        /// <summary>Valor crudo de workers.contrata_casa (Casa, Contrata).</summary>
        public string? ContrataCasa { get; set; }
        /// <summary>Nombre de la categoría (campo de lógica, ej. "OPERARIO", "RESIDENTE").</summary>
        public string? Categoria { get; set; }
        /// <summary>Nombre del puesto (campo de presentación).</summary>
        public string? Puesto { get; set; }
        /// <summary>Correo corporativo del trabajador, si tiene (workers.email_corporativo).</summary>
        public string? WorkerEmail { get; set; }
        /// <summary>Correo del administrador encargado de la razón social/proyecto para el envío consolidado (obreros sin correo).</summary>
        public string? AdministradorEmail { get; set; }
        /// <summary>Nombre del jefe/revisor del trabajador (o del área de GTH si se cayó al fallback).</summary>
        public string? Jefatura { get; set; }
        /// <summary>Correo del jefe, resuelto por IJefeRevisorResolver desde la configuración
        /// global de revisores: revisor directo → revisor del área → fallback GTH.</summary>
        public string? JefaturaEmail { get; set; }
        public string Especialidad { get; set; } = string.Empty;
        public string? MedicoDeriva { get; set; }
        public DateOnly FechaDerivacion { get; set; }
        public DateOnly? FechaAtencion { get; set; }
        public string? CentroAtencion { get; set; }
        public string? Diagnostico { get; set; }
        public string? Resultado { get; set; }
        public string Estado { get; set; } = string.Empty;
        public bool RequiereSeguimiento { get; set; }
        public string? UrlInforme { get; set; }
        public int DiasPendiente { get; set; }
    }
}
