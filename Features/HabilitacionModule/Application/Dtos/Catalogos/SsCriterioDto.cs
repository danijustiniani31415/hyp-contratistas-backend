namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    public class SsCriterioDto
    {
        public int Id { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}
