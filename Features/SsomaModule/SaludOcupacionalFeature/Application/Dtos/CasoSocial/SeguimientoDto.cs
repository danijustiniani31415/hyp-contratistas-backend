namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial
{
    public class SeguimientoDto
    {
        public Guid Id { get; set; }
        public Guid CasoId { get; set; }
        public DateOnly Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? ResponsableId { get; set; }
        public string? ResponsableNombre { get; set; }
        public DateOnly? ProximaAccion { get; set; }
        public string? AccionTomada { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class SeguimientoCreateDto
    {
        public DateOnly Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int? ResponsableId { get; set; }
        public DateOnly? ProximaAccion { get; set; }
        public string? AccionTomada { get; set; }
    }
}
