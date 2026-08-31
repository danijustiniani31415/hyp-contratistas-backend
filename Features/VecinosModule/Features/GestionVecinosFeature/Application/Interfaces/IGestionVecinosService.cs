using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Interfaces
{
    public interface IGestionVecinosService
    {
        Task<VecinosPageDto> GetPageData(VecinoFilterDto filter);
        Task<PagedResult<VecinoListItemDto>> GetList(VecinoFilterDto filter);
        Task<VecinoListItemDto> GetById(int vecinoId);
        Task<VecinoLoteRegisterResultDto> RegisterVecinos(VecinoLoteRegisterDto dto, int userId);
        Task Update(int vecinoId, VecinoUpdateDto dto, int userId);
        Task UpdateLote(int vecinoLoteId, VecinoLoteUpdateDto dto, int userId);
        Task<List<VecinoImagenDto>> UploadImagenes(int vecinoId, IFormFileCollection files, int userId);
        Task DeleteImagen(int imagenId, int userId);

        Task<VecinoSolicitudesResponseDto> GetSolicitudes(int vecinoId);
        Task<int> CreateSolicitud(int vecinoId, VecinoSolicitudCreateDto dto, int userId);
        Task UpdateSolicitudEstado(int solicitudId, int estadoId, int userId);
        Task UpdateSolicitudDescripcion(int solicitudId, string descripcion, int userId);

        Task<List<VecinoCompromisoItemDto>> GetCompromisos(int solicitudId);
        Task<int> CreateCompromiso(int solicitudId, VecinoCompromisoCreateDto dto, int userId);
        Task UpdateCompromisoEstado(int compromisoId, int estadoId, int userId);
        Task UpdateCompromisoObservaciones(int compromisoId, string? observaciones, int userId);
        Task UpdateCompromisoFechaMunicipalidad(int compromisoId, DateOnly? fechaFinMunicipalidad, int userId);
        Task UpdateEntregableEstado(int entregableId, int estadoId, int userId);
        Task<string> UploadEntregable(int entregableId, IFormFile file, int userId);
        Task<List<VecinoNormativaDto>> UploadNormativas(int compromisoId, IFormFileCollection files, int userId);
        Task DeleteNormativa(int normativaId, int userId);

        Task<VecinoLimpiezasResponseDto> GetLimpiezas(int projectId, int year, int month);
        Task<VecinoLimpiezaDto> CreateLimpieza(int projectId, VecinoLimpiezaCreateDto dto, int userId);
        Task DeleteLimpieza(int limpiezaId, int userId);
        Task<VecinoLimpiezaCumplimientoDto> GetCumplimiento(int projectId);
        Task<List<VecinoCompromisoSelectDto>> GetCompromisosSelect(int vecinoId);
        Task<string> UploadAtencion(int limpiezaId, IFormFile file, int? vecinoCompromisoId, int userId);

        Task<VecinosDashboardDto> GetDashboard();

        Task<VecinoRequisitosResponseDto> GetRequisitos(int vecinoId);
        Task<string> UploadRequisito(int vecinoId, int tipoId, IFormFile file, int userId);
        Task SetRequisitoNoAplica(int vecinoId, int tipoId, bool noAplica, int userId);
    }
}
