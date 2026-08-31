namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    public class WorkerCreateDto
    {
        public string ApellidoNombre { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string? Celular { get; set; }
        public string? EmailCorporativo { get; set; }
        /// <summary>Correo personal / de contacto. Va a <c>person.email</c> y puede repetirse.</summary>
        public string? EmailPersonal { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        /// <summary>
        /// Checkbox "Mostrar en el boletín" (<c>person.mostrar_en_boletin</c>): true = su
        /// cumpleaños aparece en el calendario del boletín. null = el formulario no gestiona el
        /// campo (contratistas, que no capturan fecha de nacimiento) y se deja lo que ya estuviera
        /// guardado; en una persona nueva queda en true, el valor por defecto de la columna.
        /// </summary>
        public bool? MostrarEnBoletin { get; set; }
        public string? Sexo { get; set; }
        public DateOnly? FechaIngreso { get; set; }
        /// <summary>
        /// FK a <c>puesto</c>: el campo de presentación del trabajador y el único camino a su
        /// categoría (<c>puesto.categoria_id</c>). La categoría no se manda: cambiarla es
        /// cambiar de puesto, o cambiarle la categoría al puesto desde Configuración →
        /// Categorías y Puestos.
        /// </summary>
        public int? PuestoId { get; set; }
        /// <summary>
        /// Nodo del árbol de áreas elegido en el formulario (workers.area_scope_id). Cuando viene,
        /// es la fuente de verdad del área: el backend deriva de él los campos legacy
        /// Area/Subarea/Jefatura y se ignora lo que llegue en esos tres.
        /// </summary>
        public int? AreaScopeId { get; set; }
        public string? Area { get; set; }
        public string? Subarea { get; set; }
        public string? ContrataCasa { get; set; }
        /// <summary>FK a <c>workers_obra_oficina_staff</c> (Obra / Staff / Oficina Central).</summary>
        public int? ObraOficinaStaffId { get; set; }
        public string? Jefatura { get; set; }
        public string? Ruc { get; set; }
        public string? Procedencia { get; set; }
        public string? CondicionMedica { get; set; }
        public string? Notas { get; set; }
        public bool Sctr { get; set; } = false;
        public bool HabilitadoObra { get; set; } = false;
        public int? EmpresaId { get; set; }
        public int? ProyectoId { get; set; }
        /// <summary>
        /// "DNI" o "CE". No se persiste en BD — se usa solo para validación.
        /// Si no se envía, se infiere del formato: 8 dígitos = DNI, resto = CE.
        /// </summary>
        public string? TipoDocumento { get; set; }
        public int? AniosExperiencia { get; set; }
        /// <summary>
        /// true = el formulario gestiona el jefe del trabajador y <see cref="JefePersonalizadoWorkerId"/>
        /// manda: se guarda ese jefe personalizado o, si viene null, se quita el que tuviera para que
        /// vuelva a depender del revisor de su área. false (por defecto) = el formulario no muestra el
        /// campo (obreros y contratistas) y no se toca lo que ya estuviera guardado.
        /// </summary>
        public bool GestionaJefe { get; set; } = false;
        /// <summary>Jefe elegido a mano (workers.id), que se sobrepone al revisor del área.</summary>
        public int? JefePersonalizadoWorkerId { get; set; }
    }
}
