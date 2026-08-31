namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class AgenteRiesgoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}
