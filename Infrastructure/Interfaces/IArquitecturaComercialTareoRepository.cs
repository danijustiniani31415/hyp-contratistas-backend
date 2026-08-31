using Abril_Backend.Application.DTOs.ArquitecturaComercial;

namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IArquitecturaComercialTareoRepository
    {
        /// <summary>Resuelve el Worker.Id del trabajador logueado a partir del User.Id del JWT
        /// (vía Person), igual que MiSaludRepository.ResolverWorkerIdAsync. Nunca hay que asumir
        /// que el claim NameIdentifier ES el Worker.Id: son secuencias distintas.</summary>
        Task<int> ResolverWorkerId(int userId);
        Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresParaEnrolar();
        Task<List<TareoTrabajadorEnrolamientoDTO>> GetTrabajadoresDisponiblesParaEnrolar();
        Task<List<TareoProyectoGeoDTO>> GetProyectosGeo();
        Task SetProyectoGeo(int projectId, TareoProyectoGeoUpdateDTO dto);
        Task<TareoAutorizacionDetalleDTO> GetAutorizacionDetalle(int workerId);
        Task<bool> TieneAutorizacion(int workerId);
        Task SetAutorizacion(int workerId, string urlDocumento, int? subidoPorUserId);
        Task<TareoIdentificacionDTO> IdentificarPorEmbedding(float[] embedding);
        Task<bool> EsObreroAc(int workerId);
        Task<TareoEnrolamientoEstadoDTO> GetEnrolamientoEstado(int workerId);
        Task EnrolarWorker(int workerId, string fotoUrl, float[] embedding);
        Task<TareoRegistroDTO> Marcar(int workerId, Guid idempotencyKey, TareoMarcarRequestDTO body, string fotoUrl, string fotoHash, string? ipOrigen);
        Task<TareoMiTareoHoyDTO> GetMiTareoHoy(int workerId);
        Task<TareoRegistroListResponseDTO> GetRegistros(TareoFiltroDTO filtro);
        Task<bool> Revisar(int id, int revisorUserId, TareoRevisarRequestDTO body);
        Task<List<TareoReporteSemanalDTO>> GetReporteSemanal(int? proyectoId, DateOnly semanaLunes);
    }
}
