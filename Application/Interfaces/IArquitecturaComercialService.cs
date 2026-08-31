using System.Text.Json;
using Abril_Backend.Application.DTOs.ArquitecturaComercial;

namespace Abril_Backend.Application.Interfaces
{
    public interface IArquitecturaComercialService
    {
        Task<ArqComercialDashboardDTO> GetDashboardData(string? semana, string? mes, int? proyectoId);
        Task<ArqComercialFiltersDTO> GetFilters();
        Task<List<ProyectoConActividadesDTO>> GetProyectosConActividades();
        Task<List<SupervisorAcDTO>> GetSupervisoresAc(bool soloObreros = false);
        Task<ActividadListResponseDTO> GetActividades(int? proyectoId, string? tipo, int? etapaId, string? search, bool? soloActivas, int pagina, int porPagina, int? userId, bool esUsuarioAc);
        Task<ActividadListItemDTO?> PatchActividad(int id, Dictionary<string, JsonElement> body);
        Task<ReasignarEncargadoResultDTO?> ReasignarEncargado(int proyectoId);
        Task<GenerarActividadesResultDTO?> GenerarActividades(int proyectoId);
        Task<ProyectoConActividadesDTO?> PatchProyecto(int id, PatchProyectoDTO body);
        Task<List<GanttActividadDTO>> GetGantt(int? proyectoId, string? tipo, string? etapa, bool? soloActivas);
        Task<List<PlantillaActividadDTO>> GetPlantilla();
        Task<PlantillaActividadDTO> CreatePlantilla(CreatePlantillaDTO body);
        Task<PlantillaActividadDTO?> PatchPlantilla(int id, Dictionary<string, JsonElement> body);
        Task<List<AcCategoriaDTO>> GetCategorias();
        Task<List<AcEspecialidadDTO>> GetEspecialidades();
        Task<List<AcEtapaDTO>> GetEtapas();
        Task<ActividadListItemDTO> CreateActividad(AcActividadCreateDTO dto);
        Task<ActividadListItemDTO> UpdateActividad(int id, AcActividadUpdateDTO dto);
        Task DeleteActividad(int id);
        Task<AvanceSemanalSnapshotResultDTO> SnapshotAvanceSemanal();
        Task<AvanceSemanalSnapshotResultDTO> SnapshotRankingSemanal();
        Task<ArqComercialDashboardDTO>   GetDashboardDataFiltrado(DashboardFiltroDTO filtro);
        Task<List<ActividadAlertaDTO>>   GetActividadesPorAlerta(string tipoAlerta, DashboardFiltroDTO filtro);
        Task                             EnviarAlertasActividades(EnviarAlertaRequestDTO request);
        Task                             RecalcularTodosSpi();
        Task<SupervisorHistoricoDTO?>    GetSupervisorHistorico(int userId);
    }
}
