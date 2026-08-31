using Abril_Backend.Application.DTOs;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface IDescansoMedicoRepository
    {
        Task<List<DescansoTipoDto>> GetTipos(bool soloMiSalud = false);
        Task<int> GetTipoIdPorNombre(string nombre);
        Task<PagedResult<DescansoMedicoListItemDto>> ListPaged(DescansoMedicoFilterDto filter);
        Task<DescansoMedicoDetalleDto> GetById(int id);
        Task<int> Create(DescansoMedicoCreateDto dto, int registradoPorId, List<DescansoCertificadoSubidoDto> adjuntos);
        /// <summary>Ubicación del archivo de un adjunto, para servirlo desde el backend. Null si no existe.</summary>
        Task<DescansoAdjuntoArchivoDto?> GetAdjunto(int adjuntoId);
        Task Update(int id, DescansoMedicoUpdateDto dto);
        /// <summary>Asigna/cambia el diagnóstico CIE-10 — a diferencia de Update, no exige que el
        /// descanso esté en estado Pendiente: el médico lo revisa normalmente después de
        /// Aprobado, cuando ya no se puede editar el resto del descanso.</summary>
        Task AsignarDiagnosticoCie10(int id, string? codigo);
        Task Aprobar(int id, DescansoAprobarDto dto, int? userId);
        Task Rechazar(int id, DescansoRechazarDto dto, int? userId);

        /// <summary>Da de alta el CASO (no un descanso individual) — cierra ss_descanso_caso.</summary>
        Task DarAlta(int casoId, DarAltaDto dto, int? userId);
        /// <summary>Reabre un caso cerrado. Exige registrar un nuevo descanso antes de poder
        /// volver a dar de alta (ver DarAlta).</summary>
        Task ReabrirCaso(int casoId, ReabrirCasoDto dto, int? userId);
        Task<CasoDetalleDto> GetCasoDetalle(int casoId);

        /// <summary>Otros casos abiertos del mismo trabajador, para que el médico pueda vincular
        /// un descanso que llegó suelto (subido por el trabajador desde Mi Salud, que nace como
        /// caso propio de un solo descanso) a un caso ya en curso, sin manejar ids a mano.</summary>
        Task<List<CasoCandidatoDto>> GetCasosCandidatos(int workerId, int excluirCasoId);
        /// <summary>Mueve el descanso (y sus seguimientos) al caso destino, y da de baja el caso
        /// de origen si se queda sin descansos. Solo se permite si el caso de origen tiene
        /// exactamente un descanso — si ya tiene más, vincularlo mezclaría historiales distintos.</summary>
        Task VincularCaso(int descansoId, int casoDestinoId);

        Task<List<DescansoSeguimientoDto>> GetSeguimientosPorCaso(int casoId, bool puedeVerDetalleClinico);
        Task<int> CreateSeguimiento(int casoId, DescansoSeguimientoCreateDto dto, int registradoPorId, string? rolUsuario);
        Task Delete(int id);

        Task<List<SeguimientoTipoDto>> GetSeguimientoTipos();
        Task<List<Cie10Dto>> BuscarCie10(string? search, int limite);
    }
}
