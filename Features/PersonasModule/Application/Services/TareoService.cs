using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    /// <summary>Fase 2 del motor de Planillas — asistencia diaria (equivale a hr.attendance en Odoo).</summary>
    public class TareoService : ITareoService
    {
        private static readonly string[] TiposDiaValidos = { "NORMAL", "DL", "F", "P", "VC", "DM" };

        private readonly IDbContextFactory<AppDbContext> _factory;

        public TareoService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<TareoMesResponseDto> GetMes(int anio, int mes)
        {
            if (mes < 1 || mes > 12) throw new AbrilException("Mes inválido.", 400);

            using var ctx = _factory.CreateDbContext();
            var diasEnMes = DateTime.DaysInMonth(anio, mes);
            var desde = new DateOnly(anio, mes, 1);
            var hasta = new DateOnly(anio, mes, diasEnMes);

            // Personas con vínculo vigente — mismo criterio que el resto del sistema
            // (idx_vinculo_vigente: FechaFin == null).
            var personas = await ctx.Persona
                .Where(p => p.Activo && p.Vinculos.Any(v => v.FechaFin == null))
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Apellidos + " " + p.Nombres,
                    CargoNombre = p.Vinculos.Where(v => v.FechaFin == null)
                        .Select(v => v.Cargo != null ? v.Cargo.Nombre : null)
                        .FirstOrDefault(),
                })
                .OrderBy(p => p.NombreCompleto)
                .ToListAsync();

            var registros = await ctx.Tareo
                .Where(t => t.Fecha >= desde && t.Fecha <= hasta && personas.Select(p => p.Id).Contains(t.PersonaId))
                .ToListAsync();
            var porPersonaYDia = registros.ToDictionary(t => (t.PersonaId, t.Fecha.Day));

            var resultado = personas.Select(p =>
            {
                var dias = new List<TareoCeldaDto>();
                for (int d = 1; d <= diasEnMes; d++)
                {
                    porPersonaYDia.TryGetValue((p.Id, d), out var reg);
                    dias.Add(new TareoCeldaDto
                    {
                        Dia = d,
                        TipoDia = reg?.TipoDia ?? "",
                        HorasTrabajadas = reg?.HorasTrabajadas,
                        HorasExtra = reg?.HorasExtra ?? 0,
                    });
                }
                return new TareoPersonaDto
                {
                    PersonaId = p.Id,
                    NombreCompleto = p.NombreCompleto,
                    CargoNombre = p.CargoNombre,
                    Dias = dias,
                };
            }).ToList();

            return new TareoMesResponseDto { Anio = anio, Mes = mes, DiasEnMes = diasEnMes, Personas = resultado };
        }

        /// <summary>
        /// Reemplaza el tareo del mes para las personas incluidas en el DTO — borra e inserta de
        /// nuevo en vez de upsert celda por celda, más simple y suficiente porque siempre se manda
        /// la grilla completa del mes desde el front. Celdas con TipoDia vacío no se guardan (día
        /// sin registrar todavía, no es lo mismo que "NORMAL").
        /// </summary>
        public async Task GuardarMes(TareoGuardarDto dto, long? registradoPor)
        {
            if (dto.Mes < 1 || dto.Mes > 12) throw new AbrilException("Mes inválido.", 400);

            foreach (var persona in dto.Personas)
            {
                foreach (var celda in persona.Dias)
                {
                    if (string.IsNullOrWhiteSpace(celda.TipoDia)) continue;
                    if (!TiposDiaValidos.Contains(celda.TipoDia))
                        throw new AbrilException($"Tipo de día inválido: {celda.TipoDia}.", 400);
                }
            }

            using var ctx = _factory.CreateDbContext();
            var diasEnMes = DateTime.DaysInMonth(dto.Anio, dto.Mes);
            var personaIds = dto.Personas.Select(p => p.PersonaId).ToList();

            using var tx = await ctx.Database.BeginTransactionAsync();

            var desde = new DateOnly(dto.Anio, dto.Mes, 1);
            var hasta = new DateOnly(dto.Anio, dto.Mes, diasEnMes);
            var existentes = await ctx.Tareo
                .Where(t => t.Fecha >= desde && t.Fecha <= hasta && personaIds.Contains(t.PersonaId))
                .ToListAsync();
            ctx.Tareo.RemoveRange(existentes);

            foreach (var persona in dto.Personas)
            {
                foreach (var celda in persona.Dias)
                {
                    if (string.IsNullOrWhiteSpace(celda.TipoDia)) continue;
                    if (celda.Dia < 1 || celda.Dia > diasEnMes) continue;

                    ctx.Tareo.Add(new Tareo
                    {
                        PersonaId = persona.PersonaId,
                        Fecha = new DateOnly(dto.Anio, dto.Mes, celda.Dia),
                        TipoDia = celda.TipoDia,
                        HorasTrabajadas = celda.HorasTrabajadas,
                        HorasExtra = celda.HorasExtra,
                        RegistradoPorUsuarioSistemaId = registradoPor,
                        CreadoEn = DateTimeOffset.UtcNow,
                    });
                }
            }

            await ctx.SaveChangesAsync();
            await tx.CommitAsync();
        }
    }
}
