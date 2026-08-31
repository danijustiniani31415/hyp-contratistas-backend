namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa
{
    public class EmpresaContratistaCreateDto
    {
        public string? Ruc { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? Rubro { get; set; }
        public string? Direccion { get; set; }
        public string Password { get; set; } = string.Empty;
        public string? EmailGerente { get; set; }
        public string? EmailAdmin { get; set; }
        public string? EmailResidente { get; set; }
        public string? EmailSsoma { get; set; }
        public string? LogoUrl { get; set; }
        public string? PartidaRegistral { get; set; }
        public string Tipo { get; set; } = "CONTRATISTA";
        public bool Activo { get; set; } = true;
        public string? ActivoRetirado { get; set; }
        public int? ProyectoId { get; set; }
        public int? IdLegacy { get; set; }
    }
}
