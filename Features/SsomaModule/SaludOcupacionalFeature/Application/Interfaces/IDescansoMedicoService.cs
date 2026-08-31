using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface IDescansoMedicoService
    {
        Task<List<DescansoTipoDto>> GetTipos();
        Task<DescansosInicioDto> GetInicio(DescansoMedicoFilterDto filter);
        Task<PagedResult<DescansoMedicoListItemDto>> ListPaged(DescansoMedicoFilterDto filter);
        Task<DescansoMedicoDetalleDto> GetById(int id);
        /// <summary>
        /// Registra el descanso y sube sus certificados (dto.Documentos) a la carpeta de
        /// SharePoint configurada en ss_descanso_carpeta.
        /// </summary>
        Task<int> Create(DescansoMedicoCreateDto dto, int? userId);
        /// <summary>
        /// Contenido de un certificado, servido por el backend (que lo baja de SharePoint con su
        /// token de app) para no depender de la sesión de Microsoft 365 del navegador.
        /// </summary>
        Task<DescansoCertificadoArchivoDto> GetCertificado(int adjuntoId);
        Task Update(int id, DescansoMedicoUpdateDto dto);
        Task AsignarDiagnosticoCie10(int id, string? codigo);
        Task Aprobar(int id, DescansoAprobarDto dto, int? userId);
        Task Rechazar(int id, DescansoRechazarDto dto, int? userId);

        Task DarAlta(int casoId, DarAltaDto dto, int? userId);
        Task ReabrirCaso(int casoId, ReabrirCasoDto dto, int? userId);
        Task<CasoDetalleDto> GetCasoDetalle(int casoId);
        Task<List<CasoCandidatoDto>> GetCasosCandidatos(int workerId, int excluirCasoId);
        Task VincularCaso(int descansoId, int casoDestinoId);

        Task<List<DescansoSeguimientoDto>> GetSeguimientosPorCaso(int casoId);
        Task<int> CreateSeguimiento(int casoId, DescansoSeguimientoCreateDto dto, int? userId, string? rolUsuario);
        Task Delete(int id);

        Task<List<SeguimientoTipoDto>> GetSeguimientoTipos();
        Task<List<Cie10Dto>> BuscarCie10(string? search);
    }
}
