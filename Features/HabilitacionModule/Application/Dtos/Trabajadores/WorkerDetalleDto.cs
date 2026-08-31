namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerDetalleDto
    {
        public int Id { get; set; }
        public int? IdTrabajador { get; set; }
        /// <summary>
        /// Persona de la ficha (<c>workers.person_id</c>). El formulario la usa para descartar al
        /// propio trabajador de los candidatos a jefe: la misma persona puede tener varias fichas
        /// en <c>workers</c> (reingreso), así que comparar solo por ficha dejaría pasar el caso.
        /// </summary>
        public int? PersonId { get; set; }
        public string? ApellidoNombre { get; set; }
        public string? Dni { get; set; }
        public string? Ruc { get; set; }
        public string? Celular { get; set; }
        public string? EmailCorporativo { get; set; }
        /// <summary>Correo personal / de contacto (person.email).</summary>
        public string? EmailPersonal { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        /// <summary>
        /// <c>person.mostrar_en_boletin</c>: true = su cumpleaños aparece en el calendario del
        /// boletín. Es lo que precarga el checkbox "Mostrar en el boletín" del formulario.
        /// </summary>
        public bool MostrarEnBoletin { get; set; } = true;
        public string? Sexo { get; set; }
        public DateOnly? FechaIngreso { get; set; }
        public DateOnly? FechaRetiro { get; set; }
        /// <summary>FK a <c>categoria</c> derivada de <c>puesto.categoria_id</c>, para precargar
        /// el filtro de categoría del formulario. Solo lectura: no se puede guardar.</summary>
        public int? CategoriaId { get; set; }
        public string? Categoria { get; set; }
        /// <summary>FK a <c>puesto</c> (campo de presentación), para precargar el desplegable.</summary>
        public int? PuestoId { get; set; }
        public string? Puesto { get; set; }
        /// <summary>
        /// Nodo del árbol de áreas asignado (workers.area_scope_id). Es lo que el formulario usa
        /// para precargar los desplegables de área; Area/Subarea son su equivalencia legacy.
        /// </summary>
        public int? AreaScopeId { get; set; }
        public string? Area { get; set; }
        public string? Subarea { get; set; }
        public string? ContrataCasa { get; set; }
        /// <summary>FK a <c>workers_obra_oficina_staff</c>. Fuente de verdad.</summary>
        public int? ObraOficinaStaffId { get; set; }
        /// <summary>Nombre del catálogo (solo lectura, derivado de <see cref="ObraOficinaStaffId"/>).</summary>
        public string? ObraOficina { get; set; }
        public string? Jefatura { get; set; }
        public string? Estado { get; set; }
        public bool? HabilitadoObra { get; set; }
        public bool? Sctr { get; set; }
        public string? CondicionMedica { get; set; }
        public string? Procedencia { get; set; }
        public string? Notas { get; set; }
        public int? PuntosInfraccion { get; set; }
        public int? AniosExperiencia { get; set; }
        /// <summary>
        /// Jefe elegido a mano para este trabajador (<c>workers_revisores</c>), que se sobrepone al
        /// revisor de su área. Null = no tiene y le corresponde el revisor del área. Es lo que
        /// precarga el checkbox "Jefe personalizado" del formulario.
        /// </summary>
        public int? JefePersonalizadoWorkerId { get; set; }
        public string? JefePersonalizadoNombre { get; set; }
        public string? JefePersonalizadoEmail { get; set; }
    }
}
