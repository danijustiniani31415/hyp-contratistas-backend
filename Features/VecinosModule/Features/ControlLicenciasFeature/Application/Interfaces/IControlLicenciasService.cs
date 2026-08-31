using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Dtos;
using Microsoft.AspNetCore.Http;

namespace Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Interfaces
{
    public interface IControlLicenciasService
    {
        Task<List<ProjectOptionDto>> GetProyectos();
        Task<VecinoLicenciaPlantillaResponseDto> GetPlantilla(int projectId);
        Task<VecinoLicenciaTipoDto> AddTipo(int projectId, VecinoLicenciaTipoCreateDto dto, int userId);
        Task UploadLicencia(int projectId, int tipoId, VecinoLicenciaUploadDto dto, IFormFile file, int userId);
        Task<VecinoLicenciaRecordatorioDto> AddRecordatorio(int projectId, int tipoId, VecinoLicenciaRecordatorioCreateDto dto, int userId);
        Task DeleteRecordatorio(int recordatorioId, int userId);
        Task SetNoAplica(int projectId, int tipoId, bool noAplica, int userId);
        Task<List<VecinoLicenciaHistorialItemDto>> GetHistorial(int projectId, int tipoId);

        /// <summary>Catálogo base (plantilla común a todos los proyectos), para administrarlo.</summary>
        Task<List<VecinoLicenciaTipoDto>> GetCatalogoBase();
        Task<VecinoLicenciaTipoDto> AddTipoBase(VecinoLicenciaTipoBaseUpsertDto dto, int userId);
        Task<VecinoLicenciaTipoDto> UpdateTipo(int tipoId, VecinoLicenciaTipoBaseUpsertDto dto, int userId);
        Task DeleteTipo(int tipoId, int userId);

        Task<VecinoLicenciaDestinatariosResponseDto> GetDestinatarios(int projectId);
        Task<VecinoLicenciaDestinatarioDto> AddDestinatario(int projectId, VecinoLicenciaDestinatarioUpsertDto dto, int userId);
        Task DeleteDestinatario(int destinatarioId, int userId);

        /// <summary>Cron: envía los recordatorios de licencias cuya fecha de recordatorio ya llegó, en todos los proyectos.</summary>
        Task<RecordatoriosResultDto> ProcesarRecordatorios();
    }
}
