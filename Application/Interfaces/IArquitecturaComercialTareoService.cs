using Abril_Backend.Application.DTOs.ArquitecturaComercial;

namespace Abril_Backend.Application.Interfaces
{
    public interface IArquitecturaComercialTareoService
    {
        Task<int> ResolverWorkerId(int userId);
        Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresParaEnrolar();
        Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresDisponiblesParaEnrolar();
        Task<TareoIdentificacionDTO> Identificar(float[]? embedding);
        Task<TareoEnrolamientoEstadoDTO> GetEnrolamientoEstado(int workerId);
        Task<List<TareoProyectoGeoDTO>> GetProyectosGeo();
        Task SetProyectoGeo(int projectId, TareoProyectoGeoUpdateDTO dto);
        Task<TareoAutorizacionDetalleDTO> GetAutorizacionDetalle(int workerId);
        Task<byte[]> GenerarAutorizacionPdf(int workerId);
        Task<string> SubirAutorizacion(int workerId, Stream fileStream, string fileName, int? subidoPorUserId);
        /// <summary>Lanza AbrilException(400) si el trabajador no tiene el SSO-FO-150 subido — el
        /// enrolamiento facial nunca procede sin esa evidencia.</summary>
        Task EnrolarWorker(int workerId, TareoEnrolamientoRequestDTO body);
        /// <summary>La identidad de quien marca sale SIEMPRE del reconocimiento facial (1:N contra los
        /// obreros AC enrolados), nunca del usuario logueado — el login corporativo se comparte
        /// entre varios trabajadores. Lanza AbrilException(422) si no se pudo identificar a nadie
        /// con confianza suficiente.</summary>
        Task<TareoRegistroDTO> Marcar(Guid idempotencyKey, TareoMarcarRequestDTO body, string? ipOrigen);
        Task<TareoMiTareoHoyDTO> GetMiTareoHoy(int workerId);
        Task<TareoRegistroListResponseDTO> GetRegistros(TareoFiltroDTO filtro);
        Task<bool> Revisar(int id, int revisorUserId, TareoRevisarRequestDTO body);
        Task<List<TareoReporteSemanalDTO>> GetReporteSemanal(int? proyectoId, DateOnly semanaLunes);
    }
}
