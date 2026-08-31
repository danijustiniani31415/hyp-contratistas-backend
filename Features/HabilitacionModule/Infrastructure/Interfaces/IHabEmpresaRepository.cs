using Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa;
using Abril_Backend.Features.Habilitacion.Application.Dtos.Trabajadores;
using Abril_Backend.Features.Habilitacion.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces
{
    public interface IHabEmpresaRepository
    {
        Task<List<EmpresaEntregableDto>> GetEntregablesEmpresaAsync(
            int empresaId, int proyectoId, int? mes, int? anio);

        Task<SsHabEmpresa> UpdateEntregableEmpresaAsync(
            int id, EmpresaEntregableUpdateDto dto, int? userId, int? empresaId = null);

        Task InicializarEntregablesEmpresaAsync(int empresaId, int proyectoId);

        Task ActivarProyectoAsync(int empresaId, int proyectoId);

        Task<List<ProyectoDisponibleDto>> GetProyectosDisponiblesAsync(int empresaId);

        Task DesactivarProyectoAsync(int empresaId, int proyectoId);

        Task<List<SsHabDocumentoVersionDto>> GetVersionesDocumentoEmpresaAsync(int empresaId, int itemId);

        Task<SsHabEmpresa> CrearOActualizarEntregableMesAsync(
            int empresaId, int proyectoId, int itemId, int mes, int anio,
            EmpresaEntregableUpdateDto dto, int? userId, int? empresaContId);

        Task EliminarArchivoVersionAsync(int versionArchivoId, int empresaId);

        Task<string?> GetResponsableItemEmpresaAsync(int entregableId);

        Task<List<EmpresaPorProyectoDto>> GetEmpresasPorProyectoAsync(int proyectoId);
    }
}
