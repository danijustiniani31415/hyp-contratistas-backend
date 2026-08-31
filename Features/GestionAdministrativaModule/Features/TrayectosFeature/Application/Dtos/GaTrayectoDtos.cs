namespace Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Dtos
{
    /// <summary>Item de la lista de trayectos para la tabla de configuración.</summary>
    public class GaTrayectoListItemDto
    {
        public int Id { get; set; }
        public int LugarOrigenId { get; set; }
        public string LugarOrigenNombre { get; set; } = string.Empty;
        public int LugarDestinoId { get; set; }
        public string LugarDestinoNombre { get; set; } = string.Empty;
        public decimal Monto { get; set; }
        public bool Activo { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class GaTrayectoCreateDto
    {
        public int LugarOrigenId { get; set; }
        public int LugarDestinoId { get; set; }
        public decimal Monto { get; set; }
    }

    public class GaTrayectoEditDto
    {
        public int LugarOrigenId { get; set; }
        public int LugarDestinoId { get; set; }
        public decimal Monto { get; set; }
    }

    /// <summary>Opciones de lugares activos para el selector del modal.</summary>
    public class GaTrayectoLugarOptionDto
    {
        public int Id { get; set; }
        public string NombreDisplay { get; set; } = string.Empty;
    }
}
