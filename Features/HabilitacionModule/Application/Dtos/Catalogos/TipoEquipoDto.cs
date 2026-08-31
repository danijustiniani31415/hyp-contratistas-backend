namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    /// <summary>Catálogo de tipos de equipo (Volquete, Excavadora de Oruga, …) para el desplegable del formulario de equipos.</summary>
    public class TipoEquipoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class TipoEquipoAdminDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}
