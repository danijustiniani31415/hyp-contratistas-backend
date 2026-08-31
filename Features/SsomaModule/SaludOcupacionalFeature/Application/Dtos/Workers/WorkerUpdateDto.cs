namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    public class WorkerUpdateDto
    {
        public string ApellidoNombre { get; set; } = string.Empty;
        /// <summary>
        /// Corrección de un DNI/CE mal digitado — mismo nombre de campo que <c>WorkerCreateDto.Dni</c>
        /// porque el frontend reusa el mismo payload (WorkerUpsertDto) para crear y editar. Solo se
        /// aplica si el llamador es Administrador de Obra (ver <c>WorkerSearchRepository.Update</c>)
        /// — el formulario lo muestra de solo lectura para el resto de roles y no debería mandarlo,
        /// pero el backend es quien realmente lo garantiza.
        /// </summary>
        public string? Dni { get; set; }
        public string? TipoDocumento { get; set; }
        public string? Celular { get; set; }
        public string? EmailCorporativo { get; set; }
        /// <summary>Correo personal / de contacto. Va a <c>person.email</c> y puede repetirse.</summary>
        public string? EmailPersonal { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        /// <summary>
        /// Checkbox "Mostrar en el boletín" (<c>person.mostrar_en_boletin</c>): true = su
        /// cumpleaños aparece en el calendario del boletín. null = el formulario no gestiona el
        /// campo (contratistas, que no capturan fecha de nacimiento) y se deja intacto lo que ya
        /// estuviera guardado.
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
        /// <summary>
        /// FK a <c>workers_obra_oficina_staff</c> (Obra / Staff / Oficina Central). En la
        /// actualización solo se aplica cuando la ficha todavía no tiene ninguna: cambiar una ya
        /// asignada es exclusivo de "Cambiar obra / puesto de trabajo" (CambiarObraAsync). Ver el
        /// detalle en <c>WorkerSearchRepository.Update</c>.
        /// </summary>
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
        public int? AniosExperiencia { get; set; }
        /// <summary>
        /// true = el formulario gestiona el jefe del trabajador y <see cref="JefePersonalizadoWorkerId"/>
        /// manda: se guarda ese jefe personalizado o, si viene null, se quita el que tuviera para que
        /// vuelva a depender del revisor de su área. false (por defecto) = el formulario no muestra el
        /// campo (contratistas) y no se toca lo que ya estuviera guardado.
        ///
        /// Lo mandan las tres clasificaciones de personal de casa: Staff y Oficina Central detrás
        /// del checkbox "Jefe personalizado" (el campo muestra por defecto el revisor de su área), y
        /// Obra con un desplegable opcional suelto — un obrero no tiene área en el árbol, así que sin
        /// jefe elegido a mano cae directo al fallback de GTH (ver JefeRevisorResolver).
        /// </summary>
        public bool GestionaJefe { get; set; } = false;
        /// <summary>Jefe elegido a mano (workers.id), que se sobrepone al revisor del área.</summary>
        public int? JefePersonalizadoWorkerId { get; set; }
    }
}
