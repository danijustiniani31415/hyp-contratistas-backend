namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    public class SsItemTrabajadorDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string AplicaA { get; set; } = string.Empty;
        public string Responsable { get; set; } = string.Empty;
        public bool RequiereVigencia { get; set; }
        public bool EsSctrVidaley { get; set; }
        public int Orden { get; set; }
        public bool Activo { get; set; }
    }
}
