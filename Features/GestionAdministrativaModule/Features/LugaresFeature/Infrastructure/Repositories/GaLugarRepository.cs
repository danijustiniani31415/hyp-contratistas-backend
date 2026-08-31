using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Repositories
{
    public class GaLugarRepository : IGaLugarRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public GaLugarRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Devuelve todos los lugares fijos (ga_lugar) + todos los proyectos de la tabla project
        /// con su estado activo según ga_lugar (null → false por defecto).
        /// Los fijos van primero, luego los proyectos ordenados alfabéticamente.
        /// </summary>
        public async Task<List<GaLugarConfigItemDto>> GetAll()
        {
            using var ctx = _factory.CreateDbContext();

            // ── Lugares fijos ─────────────────────────────────────────────────
            var fijos = await ctx.GaLugar
                .Where(g => g.Tipo == "fijo")
                .OrderBy(g => g.Nombre)
                .Select(g => new GaLugarConfigItemDto
                {
                    GaLugarId    = (int?)g.Id,
                    Tipo         = "fijo",
                    NombreDisplay = g.Nombre ?? string.Empty,
                    Activo       = g.Activo,
                    ProjectId    = null
                })
                .ToListAsync();

            // ── Proyectos (todos) con su estado en ga_lugar ───────────────────
            var proyectos = await (
                from p in ctx.Project
                join g in ctx.GaLugar.Where(x => x.Tipo == "proyecto")
                    on p.ProjectId equals g.ProjectId into gGroup
                from g in gGroup.DefaultIfEmpty()
                orderby p.ProjectDescription
                select new GaLugarConfigItemDto
                {
                    GaLugarId    = g == null ? (int?)null : (int?)g.Id,
                    Tipo         = "proyecto",
                    NombreDisplay = p.ProjectDescription,
                    Activo       = g != null && g.Activo,
                    ProjectId    = (int?)p.ProjectId
                }
            ).ToListAsync();

            return fijos.Concat(proyectos).ToList();
        }

        /// <summary>Crea uno o varios lugares fijos de una vez.</summary>
        public async Task CreateBatch(GaLugarCreateBatchDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var nombres = dto.Nombres
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (nombres.Count == 0)
                throw new AbrilException("Debe ingresar al menos un nombre de lugar.", 400);

            var entidades = nombres.Select(n => new GaLugar
            {
                Tipo      = "fijo",
                Nombre    = n,
                Activo    = true,
                Orden     = 0,
                CreatedAt = DateTimeOffset.UtcNow
            }).ToList();

            ctx.GaLugar.AddRange(entidades);
            await ctx.SaveChangesAsync();
        }

        /// <summary>Toggle activo/inactivo de una fila existente en ga_lugar (fijo o proyecto ya registrado).</summary>
        public async Task<bool> ToggleActivo(int id)
        {
            using var ctx = _factory.CreateDbContext();

            var lugar = await ctx.GaLugar.FindAsync(id)
                ?? throw new AbrilException("Lugar no encontrado.", 404);

            lugar.Activo = !lugar.Activo;
            await ctx.SaveChangesAsync();
            return lugar.Activo;
        }

        /// <summary>
        /// Toggle de un proyecto: si no existe fila en ga_lugar lo crea con activo=true.
        /// Si ya existe, invierte el flag activo.
        /// </summary>
        public async Task<ToggleProyectoResultDto> ToggleProyecto(int projectId)
        {
            using var ctx = _factory.CreateDbContext();

            _ = await ctx.Project.FindAsync(projectId)
                ?? throw new AbrilException("Proyecto no encontrado.", 404);

            var lugar = await ctx.GaLugar
                .FirstOrDefaultAsync(g => g.Tipo == "proyecto" && g.ProjectId == projectId);

            if (lugar == null)
            {
                // Primera vez: insertar con activo = true
                lugar = new GaLugar
                {
                    Tipo      = "proyecto",
                    ProjectId = projectId,
                    Activo    = true,
                    Orden     = 0,
                    CreatedAt = DateTimeOffset.UtcNow
                };
                ctx.GaLugar.Add(lugar);
            }
            else
            {
                lugar.Activo = !lugar.Activo;
            }

            await ctx.SaveChangesAsync();
            return new ToggleProyectoResultDto { Activo = lugar.Activo, GaLugarId = lugar.Id };
        }

        /// <summary>Edita el nombre de un lugar fijo.</summary>
        public async Task Edit(int id, GaLugarEditDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var lugar = await ctx.GaLugar.FindAsync(id)
                ?? throw new AbrilException("Lugar no encontrado.", 404);

            if (lugar.Tipo != "fijo")
                throw new AbrilException("Solo se puede editar el nombre de lugares fijos.", 400);

            if (string.IsNullOrWhiteSpace(dto.Nombre))
                throw new AbrilException("El nombre no puede estar vacío.", 400);

            lugar.Nombre = dto.Nombre.Trim();
            await ctx.SaveChangesAsync();
        }
    }
}
