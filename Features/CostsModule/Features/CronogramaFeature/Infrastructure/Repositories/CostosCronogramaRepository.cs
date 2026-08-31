using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Dtos;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Infrastructure.Repositories
{
    public class CostosCronogramaRepository : ICostosCronogramaRepository
    {
        private readonly AppDbContext _context;

        public CostosCronogramaRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<CronogramaActividadDto>> GetActividadesAsync()
        {
            return await _context.CostosCronogramaActividad
                .Where(a => a.State && a.Active)
                .OrderBy(a => a.Nombre)
                .Select(a => new CronogramaActividadDto
                {
                    CostosCronogramaActividadId = a.CostosCronogramaActividadId,
                    Nombre = a.Nombre,
                })
                .ToListAsync();
        }

        public async Task<CronogramaActividadDto> CreateActividadAsync(string nombre, int userId)
        {
            var trimmed = nombre.Trim();
            if (trimmed.Length == 0)
                throw new AbrilException("El nombre de la actividad es obligatorio.");

            var exists = await _context.CostosCronogramaActividad
                .AnyAsync(a => a.State && a.Nombre.ToUpper() == trimmed.ToUpper());
            if (exists)
                throw new AbrilException("Ya existe una actividad con ese nombre.");

            var actividad = new CostosCronogramaActividad
            {
                Nombre = trimmed,
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true,
            };
            _context.CostosCronogramaActividad.Add(actividad);
            await _context.SaveChangesAsync();

            return new CronogramaActividadDto
            {
                CostosCronogramaActividadId = actividad.CostosCronogramaActividadId,
                Nombre = actividad.Nombre,
            };
        }

        public async Task<List<CronogramaNodoDto>> GetNodosAsync(int projectSubContractorId)
        {
            var cronogramaId = await _context.CostosCronograma
                .Where(c => c.ProjectSubContractorId == projectSubContractorId && c.State)
                .Select(c => (int?)c.CostosCronogramaId)
                .FirstOrDefaultAsync();

            if (cronogramaId == null) return new List<CronogramaNodoDto>();

            var nodos = await _context.CostosCronogramaActividadNodo
                .Where(n => n.CostosCronogramaId == cronogramaId)
                .ToListAsync();

            // El padre se expone por actividad (una actividad aparece una sola vez por cronograma).
            var actividadPorNodo = nodos.ToDictionary(n => n.CostosCronogramaActividadNodoId, n => n.CostosCronogramaActividadId);

            return nodos
                .OrderBy(n => n.CostosCronogramaNodoOrden)
                .Select(n => new CronogramaNodoDto
                {
                    ActividadId = n.CostosCronogramaActividadId,
                    ParentActividadId = n.CostosCronogramaActividadNodoPadreId == null
                        ? null
                        : actividadPorNodo.GetValueOrDefault(n.CostosCronogramaActividadNodoPadreId.Value),
                    Orden = n.CostosCronogramaNodoOrden,
                    FechaInicio = n.FechaInicio,
                    FechaFin = n.FechaFin,
                })
                .ToList();
        }

        public async Task<List<CronogramaNodoDetalleDto>> GetNodosDetalleAsync(int projectSubContractorId)
        {
            var cronogramaId = await _context.CostosCronograma
                .Where(c => c.ProjectSubContractorId == projectSubContractorId && c.State)
                .Select(c => (int?)c.CostosCronogramaId)
                .FirstOrDefaultAsync();

            if (cronogramaId == null) return new List<CronogramaNodoDetalleDto>();

            var nodos = await _context.CostosCronogramaActividadNodo
                .Where(n => n.CostosCronogramaId == cronogramaId)
                .ToListAsync();

            var actividadIds = nodos.Select(n => n.CostosCronogramaActividadId).Distinct().ToList();
            var nombres = await _context.CostosCronogramaActividad
                .Where(a => actividadIds.Contains(a.CostosCronogramaActividadId))
                .ToDictionaryAsync(a => a.CostosCronogramaActividadId, a => a.Nombre);

            var actividadPorNodo = nodos.ToDictionary(n => n.CostosCronogramaActividadNodoId, n => n.CostosCronogramaActividadId);

            return nodos
                .OrderBy(n => n.CostosCronogramaNodoOrden)
                .Select(n => new CronogramaNodoDetalleDto
                {
                    ActividadId = n.CostosCronogramaActividadId,
                    ParentActividadId = n.CostosCronogramaActividadNodoPadreId == null
                        ? null
                        : actividadPorNodo.GetValueOrDefault(n.CostosCronogramaActividadNodoPadreId.Value),
                    Orden = n.CostosCronogramaNodoOrden,
                    Nombre = nombres.GetValueOrDefault(n.CostosCronogramaActividadId, string.Empty),
                    FechaInicio = n.FechaInicio,
                    FechaFin = n.FechaFin,
                })
                .ToList();
        }

        public async Task SaveFileInfoAsync(int projectSubContractorId, string fileUrl, string originalFileName, int userId)
        {
            var cronograma = await _context.CostosCronograma
                .FirstOrDefaultAsync(c => c.ProjectSubContractorId == projectSubContractorId && c.State)
                ?? throw new AbrilException("No existe un cronograma para esta adjudicación.");

            cronograma.FileUrl = fileUrl;
            cronograma.OriginalFileName = originalFileName;
            cronograma.UpdatedDateTime = DateTimeOffset.UtcNow;
            cronograma.UpdatedUserId = userId;
            await _context.SaveChangesAsync();
        }

        public async Task SaveAsync(int projectSubContractorId, List<CronogramaNodoDto> nodos, int userId)
        {
            var pscExists = await _context.ProjectSubContractor
                .AnyAsync(p => p.ProjectSubContractorId == projectSubContractorId && p.State);
            if (!pscExists)
                throw new AbrilException("La adjudicación no existe.");

            // Validaciones del árbol: actividades únicas y padres dentro del propio conjunto.
            var ids = nodos.Select(n => n.ActividadId).ToList();
            if (ids.Count != ids.Distinct().Count())
                throw new AbrilException("Una actividad no puede aparecer más de una vez en el cronograma.");
            var idSet = ids.ToHashSet();
            if (nodos.Any(n => n.ParentActividadId != null && !idSet.Contains(n.ParentActividadId.Value)))
                throw new AbrilException("Hay un nodo cuyo padre no forma parte del cronograma.");

            var cronograma = await _context.CostosCronograma
                .FirstOrDefaultAsync(c => c.ProjectSubContractorId == projectSubContractorId && c.State);

            if (cronograma == null)
            {
                cronograma = new CostosCronograma
                {
                    ProjectSubContractorId = projectSubContractorId,
                    CreatedDateTime = DateTimeOffset.UtcNow,
                    CreatedUserId = userId,
                    Active = true,
                    State = true,
                };
                _context.CostosCronograma.Add(cronograma);
                await _context.SaveChangesAsync();
            }
            else
            {
                cronograma.UpdatedDateTime = DateTimeOffset.UtcNow;
                cronograma.UpdatedUserId = userId;
            }

            // Reemplazo completo: se borran los nodos actuales en un solo DELETE.
            // (Con RemoveRange, EF podía borrar un padre antes que sus hijos: el ON DELETE CASCADE
            // de la FK autoreferenciada los eliminaba y el borrado del hijo fallaba por concurrencia.)
            await _context.CostosCronogramaActividadNodo
                .Where(n => n.CostosCronogramaId == cronograma.CostosCronogramaId)
                .ExecuteDeleteAsync();

            // Insertar en orden topológico (padres antes que hijos) para resolver los FK.
            var porActividad = new Dictionary<int, CostosCronogramaActividadNodo>();
            var pendientes = new List<CronogramaNodoDto>(nodos);
            var safety = nodos.Count + 1;
            while (pendientes.Count > 0 && safety-- > 0)
            {
                var listos = pendientes
                    .Where(n => n.ParentActividadId == null || porActividad.ContainsKey(n.ParentActividadId.Value))
                    .ToList();
                if (listos.Count == 0)
                    throw new AbrilException("El cronograma contiene un ciclo de jerarquía.");

                foreach (var dto in listos)
                {
                    var nodo = new CostosCronogramaActividadNodo
                    {
                        CostosCronogramaId = cronograma.CostosCronogramaId,
                        CostosCronogramaActividadId = dto.ActividadId,
                        CostosCronogramaActividadNodoPadreId = dto.ParentActividadId == null
                            ? null
                            : porActividad[dto.ParentActividadId.Value].CostosCronogramaActividadNodoId,
                        CostosCronogramaNodoOrden = dto.Orden,
                        FechaInicio = dto.FechaInicio,
                        FechaFin = dto.FechaFin,
                    };
                    _context.CostosCronogramaActividadNodo.Add(nodo);
                    // Guardar por lote de nivel para obtener los IDs de los padres.
                    porActividad[dto.ActividadId] = nodo;
                    pendientes.Remove(dto);
                }
                await _context.SaveChangesAsync();
            }
        }
    }
}
