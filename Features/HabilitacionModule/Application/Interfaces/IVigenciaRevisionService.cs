namespace Abril_Backend.Features.Habilitacion.Application.Interfaces
{
    public interface IVigenciaRevisionService
    {
        Task<VigenciaRevisionResultDto> RevisarVigencias();
    }

    public class VigenciaRevisionResultDto
    {
        public int Trabajadores { get; set; }
        public int Empresas { get; set; }
        public int Equipos { get; set; }
        public int Emos { get; set; }
    }
}
