namespace Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Models
{
    /// <summary>
    /// Lugar de origen o destino para solicitudes de salida.
    /// tipo = "proyecto" → project_id referencia la tabla project (nombre dinámico)
    /// tipo = "fijo"     → nombre almacenado en esta tabla
    /// tipo = "libre"    → opción "Otro lugar" (sin nombre ni project_id)
    /// </summary>
    public class GaLugar
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty; // "proyecto" | "fijo" | "libre"
        public string? Nombre { get; set; }
        public int? ProjectId { get; set; }
        public bool Activo { get; set; } = true;
        public int Orden { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
