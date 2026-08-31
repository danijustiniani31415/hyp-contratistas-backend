namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    /// <summary>Tipo de documento de identidad (DNI, CE, ...) para selects del frontend.</summary>
    public class DocumentTypeDto
    {
        public int Id { get; set; }
        public string Abreviatura { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
    }
}
