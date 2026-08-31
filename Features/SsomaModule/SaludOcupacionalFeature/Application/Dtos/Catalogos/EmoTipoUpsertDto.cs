namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    public class EmoTipoUpsertDto
    {
        public string Nombre { get; set; } = string.Empty;
        public int? VigenciaMeses { get; set; }
        public bool RequiereNuevo { get; set; }
        public string? Descripcion { get; set; }
        public bool Activo { get; set; } = true;
    }
}
