namespace Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Dtos
{
    public class ProjectCreateDto
    {
        public string ProjectDescription { get; set; } = null!;
        public string? Codigo { get; set; }
        public string? Abbreviation { get; set; }
        public string? LevelDescription { get; set; }
        public string? Estado { get; set; }

        // Contribuyente
        public int? ContributorId { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }

        // Ubicación
        public string? ProjectDistrict { get; set; }
        public string? ProjectProvince { get; set; }
        public string? ProjectDepartment { get; set; }
        public string? ProjectLocation { get; set; }

        // Responsable
        public string? ResponsableArqCom { get; set; }
        public int? ResponsableArqComId { get; set; }
        public string? ResponsableUdp { get; set; }
        public int? ResponsableUdpId { get; set; }

        // Fechas
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public DateOnly? InicioObra { get; set; }
        public DateOnly? FinObra { get; set; }

        // Métricas físicas
        public string? NumNiveles { get; set; }
        public string? NumSotanos { get; set; }
        public string? Pisos { get; set; }
        public int? TiempoConstruccion { get; set; }
        public decimal? AreaM2 { get; set; }
        public decimal? AreaTechadaM2 { get; set; }
        public decimal? HhTotalCasa { get; set; }
        public string? CantTrabajadoresCasa { get; set; }

        // Flags
        public bool? TieneArquitecturaComercial { get; set; }

        public bool Active { get; set; }
    }
}
