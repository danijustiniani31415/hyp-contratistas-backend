namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoPorTrabajadorDto
    {
        public int WorkerId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        /// <summary>FK al tipo de documento (para prellenar el modal de edición).</summary>
        public int? DocumentIdentityTypeId { get; set; }
        /// <summary>Cumpleaños del trabajador (para prellenar el modal de edición).</summary>
        public DateOnly? Cumpleanos { get; set; }
        public int? EmpresaId { get; set; }
        public string? Empresa { get; set; }
        public string? EmpresaOrigenNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? ObraOficinaStaffId { get; set; }
        public string? ObraOficina { get; set; }
        public string? TipoContrata { get; set; }
        /// <summary>Categoría del trabajador (para prellenar el modal de edición).</summary>
        public string? Categoria { get; set; }
        /// <summary>Ocupación del trabajador (para prellenar el modal de edición).</summary>
        public string? Ocupacion { get; set; }
        /// <summary>FK de la ocupación normalizada (para prellenar el modal de edición).</summary>
        public int? OcupacionId { get; set; }
        /// <summary>Puesto final del trabajador (para prellenar el modal de edición).</summary>
        public string? Puesto { get; set; }
        /// <summary>Nodo del árbol de áreas asignado (workers.area_scope_id, para prellenar el modal de edición).</summary>
        public int? AreaScopeId { get; set; }
        /// <summary>FK a <c>categoria</c>, derivada de <c>puesto.categoria_id</c>, para prellenar
        /// el filtro de categoría del modal de edición. Solo lectura.</summary>
        public int? CategoriaId { get; set; }
        /// <summary>FK a <c>puesto</c> (workers.puesto_id), para prellenar el modal de edición.</summary>
        public int? PuestoId { get; set; }
        /// <summary>Correo corporativo del trabajador (workers.email_corporativo, para prellenar el modal de edición).</summary>
        public string? EmailCorporativo { get; set; }
        /// <summary>Correo personal / de contacto (person.email, para prellenar el modal de edición).</summary>
        public string? EmailPersonal { get; set; }
        /// <summary>
        /// true cuando la fila no es un trabajador todavia, sino la ficha de pre-ingreso de un
        /// finalista que el solicitante aprobo en Reclutamiento y que sigue sin firmar contrato.
        /// El front la marca aparte y solo le deja programar el EMO de tipo Ingreso.
        /// </summary>
        public bool EsFinalistaAprobado { get; set; }

        public bool TieneEmo { get; set; }
        public int? EmoId { get; set; }
        public string? TipoEmo { get; set; }
        public DateOnly? FechaEmo { get; set; }
        public DateOnly? FechaVencimiento { get; set; }
        public string? Aptitud { get; set; }
        public string? Estado { get; set; }
        public int? DiasRestantes { get; set; }
        public string? UrlAptitud { get; set; }
        public string? UrlEmoCompleto { get; set; }
        public string? UrlResultado { get; set; }
        /// <summary>true = la lectura la hace el médico de Abril, no la clínica. Junto con
        /// UrlResultado == null define si el EMO está "pendiente de lectura" para Abril.</summary>
        public bool RequiereLecturaAbril { get; set; }
        public bool RequiereInterconsulta { get; set; }
        public int? InterconsultaId { get; set; }
        public string? InterconsultaEspecialidad { get; set; }
        public string? InterconsultaEstado { get; set; }
        public string? InterconsultaUrlInforme { get; set; }
        /// <summary>Estado de la programación de EMO más reciente (Programado, Aceptado por
        /// Clínica, etc.) — para que Salud vea de un vistazo si la cita ya fue aceptada por la
        /// clínica, sin entrar a Programaciones.</summary>
        public string? EstadoProgramacionEmo { get; set; }
    }
}
