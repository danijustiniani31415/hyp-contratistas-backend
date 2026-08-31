namespace Abril_Backend.Features.PlaneamientoBimFeature.Application.Dtos
{
    public class RestriccionDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTimeOffset FechaCreacion { get; set; }
        public DateTimeOffset? FechaActualizacion { get; set; }
        public DateTimeOffset? FechaCierre { get; set; }
        public DateOnly? FechaLevantamientoPrevista { get; set; }

        public int? ZonaId { get; set; }
        public string? ZonaNombre { get; set; }
        public int? ZonaNivelId { get; set; }
        public string? ZonaNivelNombre { get; set; }
        public int? ZonaSectorId { get; set; }
        public string? ZonaSectorNombre { get; set; }
        public int? ActividadId { get; set; }
        public string? ActividadNombre { get; set; }
    }

    public class RestriccionCreateDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaLevantamientoPrevista { get; set; }
        public int? ZonaId { get; set; }
        public int? ZonaNivelId { get; set; }
        public int? ZonaSectorId { get; set; }
        public int? ActividadId { get; set; }
    }

    public class RestriccionUpdateDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaLevantamientoPrevista { get; set; }
        public int? ZonaId { get; set; }
        public int? ZonaNivelId { get; set; }
        public int? ZonaSectorId { get; set; }
        public int? ActividadId { get; set; }
    }
}
