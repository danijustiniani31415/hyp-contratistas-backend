namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    public class EvPlantillaDto
    {
        public int Id { get; set; }
        public string AreaNombre { get; set; } = string.Empty;
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
        public int Version { get; set; }
    }

    public class EvPlantillaUpdateDto
    {
        public string Criterio { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }

    public class EvPlantillaCreateDto
    {
        public string AreaNombre { get; set; } = string.Empty;
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
    }
}
