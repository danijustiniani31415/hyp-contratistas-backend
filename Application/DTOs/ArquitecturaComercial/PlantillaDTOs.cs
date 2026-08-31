namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class PlantillaActividadDTO
    {
        public int Id { get; set; }
        public int? Orden { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Tipo { get; set; }
        public int? EtapaId { get; set; }
        public string? EtapaNombre { get; set; }
        public int? CategoriaId { get; set; }
        public string? CategoriaNombre { get; set; }
        public int? EspecialidadId { get; set; }
        public string? EspecialidadNombre { get; set; }
        public bool Activo { get; set; }
    }

    public class CreatePlantillaDTO
    {
        public string? Nombre { get; set; }
        public string? Tipo { get; set; }
        public int? EtapaId { get; set; }
        public int? CategoriaId { get; set; }
        public int? EspecialidadId { get; set; }
        public int? Orden { get; set; }
        public bool? Activo { get; set; }
    }

    public class AcCategoriaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class AcEspecialidadDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class AcEtapaDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }
}
