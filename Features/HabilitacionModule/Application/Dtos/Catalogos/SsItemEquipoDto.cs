namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    public class SsItemEquipoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool RequiereVigencia { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>Ítem de equipo para la pantalla de administración: incluye inactivos y a qué tipo aplica.</summary>
    public class ItemEquipoAdminDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool RequiereVigencia { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
        /// <summary>Null = ítem genérico, se exige a todos los equipos.</summary>
        public int? TipoEquipoId { get; set; }
        public string? TipoEquipoNombre { get; set; }
    }

    public class ItemEquipoUpsertRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public bool RequiereVigencia { get; set; }
        public int? TipoEquipoId { get; set; }
    }
}
