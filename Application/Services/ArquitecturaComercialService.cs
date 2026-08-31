using System.Text.Json;
using Abril_Backend.Application.DTOs.ArquitecturaComercial;
using Abril_Backend.Application.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Application.Services
{
    public class ArquitecturaComercialService : IArquitecturaComercialService
    {
        private readonly IArquitecturaComercialRepository _repository;
        private readonly IDbContextFactory<AppDbContext>  _factory;
        private readonly IEmailService                    _emailService;

        public ArquitecturaComercialService(
            IArquitecturaComercialRepository repository,
            IDbContextFactory<AppDbContext>  factory,
            IEmailService                    emailService)
        {
            _repository   = repository;
            _factory      = factory;
            _emailService = emailService;
        }

        public async Task<ArqComercialDashboardDTO> GetDashboardData(string? semana, string? mes, int? proyectoId)
        {
            return await _repository.GetDashboardData(semana, mes, proyectoId);
        }

        public async Task<ArqComercialFiltersDTO> GetFilters()
        {
            return await _repository.GetFilters();
        }

        public async Task<List<ProyectoConActividadesDTO>> GetProyectosConActividades()
        {
            return await _repository.GetProyectosConActividades();
        }

        public async Task<List<SupervisorAcDTO>> GetSupervisoresAc(bool soloObreros = false)
        {
            return await _repository.GetSupervisoresAc(soloObreros);
        }

        public async Task<ActividadListResponseDTO> GetActividades(int? proyectoId, string? tipo, int? etapaId, string? search, bool? soloActivas, int pagina, int porPagina, int? userId, bool esUsuarioAc)
        {
            return await _repository.GetActividades(proyectoId, tipo, etapaId, search, soloActivas, pagina, porPagina, userId, esUsuarioAc);
        }

        public async Task<ActividadListItemDTO?> PatchActividad(int id, Dictionary<string, JsonElement> body)
        {
            return await _repository.PatchActividad(id, body);
        }

        public async Task<ReasignarEncargadoResultDTO?> ReasignarEncargado(int proyectoId)
        {
            return await _repository.ReasignarEncargado(proyectoId);
        }

        public async Task<GenerarActividadesResultDTO?> GenerarActividades(int proyectoId)
        {
            return await _repository.GenerarActividades(proyectoId);
        }

        public async Task<ProyectoConActividadesDTO?> PatchProyecto(int id, PatchProyectoDTO body)
        {
            return await _repository.PatchProyecto(id, body);
        }

        public async Task<List<GanttActividadDTO>> GetGantt(int? proyectoId, string? tipo, string? etapa, bool? soloActivas)
        {
            return await _repository.GetGantt(proyectoId, tipo, etapa, soloActivas);
        }

        public async Task<List<PlantillaActividadDTO>> GetPlantilla()
            => await _repository.GetPlantilla();

        public async Task<PlantillaActividadDTO> CreatePlantilla(CreatePlantillaDTO body)
            => await _repository.CreatePlantilla(body);

        public async Task<PlantillaActividadDTO?> PatchPlantilla(int id, Dictionary<string, JsonElement> body)
            => await _repository.PatchPlantilla(id, body);

        public async Task<List<AcCategoriaDTO>> GetCategorias()
            => await _repository.GetCategorias();

        public async Task<List<AcEspecialidadDTO>> GetEspecialidades()
            => await _repository.GetEspecialidades();

        public async Task<List<AcEtapaDTO>> GetEtapas()
            => await _repository.GetEtapas();

        public async Task<ActividadListItemDTO> CreateActividad(AcActividadCreateDTO dto)
            => await _repository.CreateActividad(dto);

        public async Task<ActividadListItemDTO> UpdateActividad(int id, AcActividadUpdateDTO dto)
            => await _repository.UpdateActividad(id, dto);

        public async Task DeleteActividad(int id)
            => await _repository.DeleteActividad(id);

        public async Task<AvanceSemanalSnapshotResultDTO> SnapshotAvanceSemanal()
            => await _repository.SnapshotAvanceSemanal();

        public async Task<AvanceSemanalSnapshotResultDTO> SnapshotRankingSemanal()
            => await _repository.SnapshotRankingSemanal();

        public async Task<ArqComercialDashboardDTO> GetDashboardDataFiltrado(DashboardFiltroDTO filtro)
            => await _repository.GetDashboardDataFiltrado(filtro);

        public async Task<List<ActividadAlertaDTO>> GetActividadesPorAlerta(
            string tipoAlerta, DashboardFiltroDTO filtro)
            => await _repository.GetActividadesPorAlerta(tipoAlerta, filtro);

        public async Task RecalcularTodosSpi()
            => await _repository.RecalcularTodosSpi();

        public async Task<SupervisorHistoricoDTO?> GetSupervisorHistorico(int userId)
            => await _repository.GetSupervisorHistorico(userId);

        public async Task EnviarAlertasActividades(EnviarAlertaRequestDTO request)
        {
            using var ctx = _factory.CreateDbContext();

            var gestoresConRoles = await ctx.User
                .Join(ctx.UserRole, u => u.UserId, ur => ur.UserId, (u, ur) => new { u, ur })
                .Join(ctx.Role, x => x.ur.RoleId, r => r.RoleId, (x, r) => new { x.u.UserId, x.u.Email, r.RoleDescription })
                .Where(x => x.RoleDescription.ToUpper() == "GESTOR DE ARQUITECTURA COMERCIAL"
                         || x.RoleDescription.ToUpper() == "GERENTE DE PROYECTOS")
                .Select(x => new { x.UserId, x.Email, Rol = x.RoleDescription.ToUpper() })
                .ToListAsync();

            // El Gerente de Proyectos no debe recibir estas alertas aunque también tenga
            // asignado el rol Gestor de Arquitectura Comercial (acceso al dashboard no implica
            // que deba recibir el correo) — se excluye por UserId, no solo por rol.
            var userIdsGerenteProyectos = gestoresConRoles
                .Where(x => x.Rol == "GERENTE DE PROYECTOS")
                .Select(x => x.UserId)
                .ToHashSet();

            // TODO(temporal): exclusión hardcodeada mientras se define un mecanismo
            // basado en roles/puesto de trabajo para separar "acceso al módulo" de
            // "destinatario de alertas". No deben recibir estas alertas aunque tengan
            // el rol Gestor de Arquitectura Comercial asignado.
            var excluidosHardcode = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "vcolonio@abril.pe",
                "coriundo@abril.pe",
            };

            var emailsGestores = gestoresConRoles
                .Where(x => x.Rol == "GESTOR DE ARQUITECTURA COMERCIAL" && !userIdsGerenteProyectos.Contains(x.UserId))
                .Select(x => x.Email)
                .Where(e => e != null && !excluidosHardcode.Contains(e))
                .Distinct()
                .ToList();

            await _repository.EnviarAlertasActividades(
                request.ActividadIds, request.TipoAlerta,
                emailsGestores!, _emailService);
        }
    }
}
