namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerUpdateDto
    {
        public string? ApellidoNombre { get; set; }
        public string? Ruc { get; set; }
        public string? Celular { get; set; }
        public string? EmailCorporativo { get; set; }
        public DateOnly? FechaNacimiento { get; set; }
        public DateOnly? FechaIngreso { get; set; }
        public DateOnly? FechaRetiro { get; set; }
        /// <summary>
        /// FK a <c>puesto</c>: el campo de presentación del trabajador y el único camino a su
        /// categoría (<c>puesto.categoria_id</c>). La categoría no se manda: cambiarla es
        /// cambiar de puesto, o cambiarle la categoría al puesto desde Configuración →
        /// Categorías y Puestos.
        /// </summary>
        public int? PuestoId { get; set; }
        public string? Area { get; set; }
        public string? Subarea { get; set; }
        public string? ContrataCasa { get; set; }
        /// <summary>FK a <c>workers_obra_oficina_staff</c> (Obra / Staff / Oficina Central).</summary>
        public int? ObraOficinaStaffId { get; set; }
        public string? Jefatura { get; set; }
        public string? Estado { get; set; }
        public bool? HabilitadoObra { get; set; }
        public bool? Sctr { get; set; }
        public string? CondicionMedica { get; set; }
        public string? Procedencia { get; set; }
        public string? Notas { get; set; }
        public int? PuntosInfraccion { get; set; }
        public int? AniosExperiencia { get; set; }
    }
}
