namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Auth
{
    public class ContratistaTokenDto
    {
        public string Token { get; set; } = string.Empty;
        public int EmpresaId { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string Tipo { get; set; } = "CONTRATISTA";
        public List<string> AllowedFeatures { get; set; } = [];
        public string Scope { get; set; } = "TODOS";
        public List<int> ProyectoIds { get; set; } = [];
        public string Modulos { get; set; } = "AMBOS";
    }
}
