namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class ExamenTipoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Codigo { get; set; }
        public string? Categoria { get; set; }
        public bool Activo { get; set; }
    }
}
