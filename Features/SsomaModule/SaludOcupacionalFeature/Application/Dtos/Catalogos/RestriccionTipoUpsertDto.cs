namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class RestriccionTipoUpsertDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public string? Categoria { get; set; }
        public bool Activo { get; set; } = true;
    }
}
