namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores
{
    public class WorkerEntregableDto
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string NombreItem { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
        // Fecha propuesta por el contratista en una renovación (estado "Renovando"); null en otros casos.
        public DateTime? VigenciaPropuesta { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ObsAbril { get; set; }
        public string? ObsContratista { get; set; }
        public bool RequiereVigencia { get; set; }
        public bool EsSctrVidaley { get; set; }
        public string Responsable { get; set; } = string.Empty;
    }
}
