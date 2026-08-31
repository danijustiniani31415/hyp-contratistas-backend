using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Interfaces
{
    public interface IGestionVecinosRepository
    {
        Task<VecinoFormOptionsDto> GetOptions();
        Task<PagedResult<VecinoListItemDto>> GetPaged(VecinoFilterDto filter);
        Task<VecinoListItemDto?> GetById(int vecinoId);
        Task<VecinoLoteRegisterResultDto> RegisterVecinos(VecinoLoteRegisterDto dto, int userId);
        Task<bool> Update(int vecinoId, VecinoUpdateDto dto, int userId);
        Task<bool> UpdateLote(int vecinoLoteId, VecinoLoteUpdateDto dto, int userId);
        Task<List<VecinoImagenDto>> AddImagenes(int vecinoId, List<(string ArchivoUrl, string? OriginalFileName)> imagenes, int userId);
        Task<bool> DeleteImagen(int imagenId, int userId);

        Task<bool> VecinoExists(int vecinoId);
        Task<VecinoSolicitudesResponseDto> GetSolicitudes(int vecinoId);
        Task<int> CreateSolicitud(int vecinoId, VecinoSolicitudCreateDto dto, int userId);
        Task<bool> UpdateSolicitudEstado(int solicitudId, int estadoId, int userId);
        Task<bool> UpdateSolicitudDescripcion(int solicitudId, string descripcion, int userId);

        Task<bool> SolicitudExists(int solicitudId);
        Task<List<VecinoCompromisoItemDto>> GetCompromisos(int solicitudId);
        Task<int> CreateCompromiso(int solicitudId, VecinoCompromisoCreateDto dto, int userId);
        Task<bool> UpdateCompromisoEstado(int compromisoId, int estadoId, int userId);
        Task<bool> UpdateCompromisoObservaciones(int compromisoId, string? observaciones, int userId);
        Task<bool> UpdateCompromisoFechaMunicipalidad(int compromisoId, DateOnly? fechaFinMunicipalidad, int userId);
        Task<bool> UpdateEntregableEstado(int entregableId, int estadoId, int userId);
        Task<bool> UploadEntregable(int entregableId, string archivoUrl, string? originalFileName, int userId);
        Task<bool> CompromisoExists(int compromisoId);
        Task<List<VecinoNormativaDto>> AddNormativas(int compromisoId, List<(string ArchivoUrl, string? OriginalFileName)> archivos, int userId);
        Task<bool> DeleteNormativa(int normativaId, int userId);

        Task<VecinoLimpiezasResponseDto> GetLimpiezas(int projectId, int year, int month);
        Task<VecinoLimpiezaDto> CreateLimpieza(int projectId, VecinoLimpiezaCreateDto dto, int userId);
        Task<bool> DeleteLimpieza(int limpiezaId, int userId);
        Task<VecinoLimpiezaCumplimientoDto> GetCumplimiento(int projectId);
        Task<List<VecinoCompromisoSelectDto>> GetCompromisosSelect(int vecinoId);
        Task<bool> UploadAtencion(int limpiezaId, string archivoUrl, string? originalFileName, int? vecinoCompromisoId, int userId);

        Task<VecinosDashboardDto> GetDashboard();

        Task<VecinoRequisitosResponseDto> GetRequisitos(int vecinoId);
        Task<bool> TipoRequisitoExists(int tipoId);
        Task UploadRequisito(int vecinoId, int tipoId, string archivoUrl, string? originalFileName, int userId);
        Task SetRequisitoNoAplica(int vecinoId, int tipoId, bool noAplica, int userId);
    }
}
