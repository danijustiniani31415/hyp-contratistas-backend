namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class ProyectoConActividadesDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public int? ResponsableArqComId { get; set; }
        public string? ResponsableArqCom { get; set; }
        public int TotalActividades { get; set; }
        public int Activas { get; set; }
        public bool SinActividades { get; set; }
    }
}
