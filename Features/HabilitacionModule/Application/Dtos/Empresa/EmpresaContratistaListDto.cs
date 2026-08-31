namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa
{
    public class EmpresaContratistaListDto
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? Ruc { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string? EmailAdmin { get; set; }
        public string? EmailSsoma { get; set; }
        public string? LogoUrl { get; set; }
        public string? Rubro { get; set; }
    }
}
