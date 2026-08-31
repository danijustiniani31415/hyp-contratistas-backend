namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    /// <summary>
    /// Fila del widget "Interconsultas pendientes" de Habilitación > Trabajadores (junto al botón
    /// "EMOs Programados"). Expone solo lo necesario para que Habilitación sepa a quién le falta
    /// coordinar la cita — nada de diagnóstico/especialidad/médico: eso es información de salud
    /// confidencial y vive solo en el módulo de Salud Ocupacional.
    /// </summary>
    public class InterconsultaPendienteHabDto
    {
        public int WorkerId { get; set; }
        public string WorkerNombre { get; set; } = string.Empty;
        public string? RazonSocial { get; set; }
        public string? ProyectoActual { get; set; }
        public int DiasPendiente { get; set; }
    }
}
