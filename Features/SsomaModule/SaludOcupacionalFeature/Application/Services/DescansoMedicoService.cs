using Abril_Backend.Application.DTOs;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Restringidos;
using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.DescansoMedico;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.Shared;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    public class DescansoMedicoService : IDescansoMedicoService
    {
        private readonly IDescansoMedicoRepository _repo;
        private readonly ITrabajadorRestringidoService _restringido;
        private readonly IDescansoCertificadoStorage _certificados;

        public DescansoMedicoService(
            IDescansoMedicoRepository repo,
            ITrabajadorRestringidoService restringido,
            IDescansoCertificadoStorage certificados)
        {
            _repo = repo;
            _restringido = restringido;
            _certificados = certificados;
        }

        public Task<List<DescansoTipoDto>> GetTipos() => _repo.GetTipos();

        /// <summary>Carga inicial de la pantalla: catálogo de tipos + primera página, en una sola llamada.</summary>
        public async Task<DescansosInicioDto> GetInicio(DescansoMedicoFilterDto filter) => new()
        {
            Tipos     = await _repo.GetTipos(),
            Descansos = await _repo.ListPaged(filter),
        };

        public Task<PagedResult<DescansoMedicoListItemDto>> ListPaged(DescansoMedicoFilterDto filter) =>
            _repo.ListPaged(filter);

        public Task<DescansoMedicoDetalleDto> GetById(int id) => _repo.GetById(id);

        public async Task<int> Create(DescansoMedicoCreateDto dto, int? userId)
        {
            if (dto.WorkerId <= 0)
                throw new AbrilException("El trabajador es obligatorio.", 400);
            if (dto.TipoId <= 0)
                throw new AbrilException("El tipo de descanso es obligatorio.", 400);
            if (dto.FechaFin < dto.FechaInicio)
                throw new AbrilException("La fecha de fin no puede ser anterior a la fecha de inicio.", 400);

            // Mismo destino que Mi Salud: la carpeta de SharePoint configurada en ss_descanso_carpeta.
            var adjuntos = await _certificados.SubirAsync(dto.Documentos ?? [], "ssoma");

            return await _repo.Create(dto, userId ?? 0, adjuntos);
        }

        public async Task<DescansoCertificadoArchivoDto> GetCertificado(int adjuntoId)
        {
            var adjunto = await _repo.GetAdjunto(adjuntoId)
                ?? throw new AbrilException("El certificado solicitado no existe.", 404);

            return await _certificados.DescargarAsync(
                    adjunto.DriveId, adjunto.ItemId, adjunto.Url, adjunto.NombreArchivo)
                ?? throw new AbrilException("No se pudo obtener el certificado desde SharePoint.", 502);
        }

        public Task Update(int id, DescansoMedicoUpdateDto dto)
        {
            if (dto.TipoId <= 0)
                throw new AbrilException("El tipo de descanso es obligatorio.", 400);
            if (dto.FechaFin < dto.FechaInicio)
                throw new AbrilException("La fecha de fin no puede ser anterior a la fecha de inicio.", 400);
            return _repo.Update(id, dto);
        }

        public async Task Aprobar(int id, DescansoAprobarDto dto, int? userId)
        {
            var descanso = await _repo.GetById(id);
            await _repo.Aprobar(id, dto, userId);

            if (descanso?.WorkerId > 0)
            {
                // Tipo=DESCANSO_MEDICO: bloquea el acceso a obra igual que antes, pero queda
                // marcado como bloqueo temporal, no como sanción — así la pantalla de
                // Amonestaciones/Inhabilitados (que es de sanciones) no lo muestra mezclado.
                await _restringido.CreateAsync(new TrabajadorRestringidoCreateDto
                {
                    WorkerId       = descanso.WorkerId,
                    Dni            = descanso.WorkerDni,
                    ApellidoNombre = descanso.WorkerNombre,
                    Motivo         = "Descanso médico aprobado",
                    FechaRestriccion = DateOnly.FromDateTime(DateTime.Today),
                    Tipo           = "DESCANSO_MEDICO",
                }, userId);
            }
        }

        public Task Rechazar(int id, DescansoRechazarDto dto, int? userId)
        {
            if (string.IsNullOrWhiteSpace(dto.MotivoRechazo))
                throw new AbrilException("El motivo de rechazo es obligatorio.", 400);
            return _repo.Rechazar(id, dto, userId);
        }

        public Task AsignarDiagnosticoCie10(int id, string? codigo) => _repo.AsignarDiagnosticoCie10(id, codigo);

        public Task DarAlta(int casoId, DarAltaDto dto, int? userId) =>
            _repo.DarAlta(casoId, dto, userId);

        public Task ReabrirCaso(int casoId, ReabrirCasoDto dto, int? userId) =>
            _repo.ReabrirCaso(casoId, dto, userId);

        public Task<CasoDetalleDto> GetCasoDetalle(int casoId) => _repo.GetCasoDetalle(casoId);

        public Task<List<CasoCandidatoDto>> GetCasosCandidatos(int workerId, int excluirCasoId) =>
            _repo.GetCasosCandidatos(workerId, excluirCasoId);

        public Task VincularCaso(int descansoId, int casoDestinoId) =>
            _repo.VincularCaso(descansoId, casoDestinoId);

        // "Solo el médico puede hacer seguimiento" queda pendiente de resolver (depende de cómo
        // inicia sesión el médico en el sistema) — por ahora se pasa true, sin filtrar nada. El
        // punto de extensión es este mismo parámetro cuando se defina el mecanismo de acceso.
        public Task<List<DescansoSeguimientoDto>> GetSeguimientosPorCaso(int casoId) =>
            _repo.GetSeguimientosPorCaso(casoId, puedeVerDetalleClinico: true);

        public Task<int> CreateSeguimiento(int casoId, DescansoSeguimientoCreateDto dto, int? userId, string? rolUsuario)
        {
            if (string.IsNullOrWhiteSpace(dto.Nota))
                throw new AbrilException("La nota es obligatoria.", 400);
            // TODO: cuando se defina cómo inicia sesión el médico, restringir acá:
            // if (rolUsuario != "MEDICO") throw new AbrilException("Solo el médico puede registrar seguimiento.", 403);
            return _repo.CreateSeguimiento(casoId, dto, userId ?? 0, rolUsuario);
        }

        public Task<List<SeguimientoTipoDto>> GetSeguimientoTipos() => _repo.GetSeguimientoTipos();

        public Task<List<Cie10Dto>> BuscarCie10(string? search) => _repo.BuscarCie10(search, limite: 30);

        public Task Delete(int id) => _repo.Delete(id);
    }
}
