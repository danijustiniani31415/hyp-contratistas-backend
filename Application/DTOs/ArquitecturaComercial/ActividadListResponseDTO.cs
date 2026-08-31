namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class ActividadListResponseDTO
    {
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int PorPagina { get; set; }
        public List<ActividadListItemDTO> Items { get; set; } = new();
    }
}
