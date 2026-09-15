namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    public class RolListItemDto
    {
        public short Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool EsGlobal { get; set; }
        public int CantidadPermisos { get; set; }
    }

    public class PermisoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string? Descripcion { get; set; }
        /// <summary>
        /// Prefijo antes del primer "_" en el código (ej. "COMBUSTIBLE_VER" -> "COMBUSTIBLE"),
        /// usado por el frontend para agrupar visualmente los permisos por módulo sin necesitar
        /// una tabla de módulos aparte.
        /// </summary>
        public string Modulo => Codigo.Contains('_') ? Codigo[..Codigo.IndexOf('_')] : Codigo;
    }

    public class RolDetalleDto
    {
        public short Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string? Descripcion { get; set; }
        public bool EsGlobal { get; set; }
        public List<int> PermisoIds { get; set; } = new();
    }

    public class ActualizarPermisosRolDto
    {
        public List<int> PermisoIds { get; set; } = new();
    }
}
