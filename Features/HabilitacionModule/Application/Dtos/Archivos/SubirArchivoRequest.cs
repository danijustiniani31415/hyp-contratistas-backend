namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Archivos
{
    public class SubirArchivoRequest
    {
        public IFormFile? File { get; set; }
        public string Contexto { get; set; } = "";
    }
}
