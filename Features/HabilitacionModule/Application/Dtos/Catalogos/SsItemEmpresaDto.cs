namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    public class SsItemEmpresaDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public int Orden { get; set; }
        public bool RequiereVigencia { get; set; }
        public bool Activo { get; set; }
    }
}
