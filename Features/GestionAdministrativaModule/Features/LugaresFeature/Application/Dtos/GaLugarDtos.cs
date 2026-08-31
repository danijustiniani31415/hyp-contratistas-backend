namespace Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Dtos
{
    /// <summary>Ítem para la tabla de configuración (fijos + todos los proyectos).</summary>
    public class GaLugarConfigItemDto
    {
        /// <summary>Id de la fila en ga_lugar. Null si el proyecto nunca fue activado.</summary>
        public int? GaLugarId { get; set; }
        public string Tipo { get; set; } = string.Empty;       // "proyecto" | "fijo"
        public string NombreDisplay { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public int? ProjectId { get; set; }                    // solo para tipo="proyecto"
    }

    /// <summary>Creación en lote de lugares fijos.</summary>
    public class GaLugarCreateBatchDto
    {
        public List<string> Nombres { get; set; } = new();
    }

    /// <summary>Edición del nombre de un lugar fijo.</summary>
    public class GaLugarEditDto
    {
        public string Nombre { get; set; } = string.Empty;
    }

    /// <summary>Resultado del toggle de un proyecto (UPSERT).</summary>
    public class ToggleProyectoResultDto
    {
        public bool Activo { get; set; }
        public int GaLugarId { get; set; }
    }
}
