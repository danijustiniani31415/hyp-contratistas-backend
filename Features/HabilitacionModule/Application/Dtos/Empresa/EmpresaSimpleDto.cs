namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Empresa
{
    public class EmpresaSimpleDto
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? NombreComercial { get; set; }
        public string? LogoUrl { get; set; }
    }
}
