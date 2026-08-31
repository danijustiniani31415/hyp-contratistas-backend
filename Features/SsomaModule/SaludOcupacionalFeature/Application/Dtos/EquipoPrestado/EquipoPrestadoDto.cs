namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.EquipoPrestado
{
    public class EquipoPrestadoListItemDto
    {
        public int Id { get; set; }
        public int AccidenteId { get; set; }
        public int TipoEquipoId { get; set; }
        public string TipoEquipoNombre { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public DateOnly FechaPrestamo { get; set; }
        public DateOnly? FechaDevolucion { get; set; }
        public bool Devuelto { get; set; }
        public string? Observaciones { get; set; }
        public string? UrlEvidencia { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class EquipoPrestadoCreateDto
    {
        public int TipoEquipoId { get; set; }
        public int Cantidad { get; set; } = 1;
        public DateOnly FechaPrestamo { get; set; }
        public string? Observaciones { get; set; }
        public string? UrlEvidencia { get; set; }
    }

    public class EquipoPrestadoDevolverDto
    {
        public DateOnly FechaDevolucion { get; set; }
        public string? Observaciones { get; set; }
    }
}
