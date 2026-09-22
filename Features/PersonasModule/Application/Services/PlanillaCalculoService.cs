using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    /// <summary>
    /// Fase 3 del motor de Planillas — motor de cálculo (equivale a hr.payslip.run/hr.payslip en
    /// Odoo). Las tasas (AFP, ONP, EsSalud, etc.) NUNCA se hardcodean acá: son "Conceptos de
    /// planilla" configurables desde el frontend — este servicio solo aplica las reglas que el
    /// usuario ya definió, sobre el sueldo/jornal (snapshot de lb_persona_planilla) y los días
    /// trabajados del tareo del mes (lb_tareo).
    /// </summary>
    public class PlanillaCalculoService : IPlanillaCalculoService
    {
        private static readonly string[] TiposValidos = { "INGRESO", "DESCUENTO", "APORTE_EMPLEADOR" };
        private static readonly string[] FormasCalculoValidas = { "FIJO", "PORCENTAJE_SUELDO", "PORCENTAJE_JORNAL", "POR_DIA_TAREO" };

        private readonly IDbContextFactory<AppDbContext> _factory;

        public PlanillaCalculoService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ── Conceptos ────────────────────────────────────────────────────────────────────

        public async Task<List<ConceptoPlanillaDto>> ListConceptos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.ConceptoPlanilla
                .OrderBy(c => c.Orden).ThenBy(c => c.Nombre)
                .Select(c => ToDto(c))
                .ToListAsync();
        }

        public async Task<ConceptoPlanillaDto> CrearConcepto(ConceptoPlanillaCreateDto dto)
        {
            ValidarConcepto(dto.Tipo, dto.FormaCalculo, dto.CategoriaLaboral);
            var codigo = dto.Codigo.Trim().ToUpperInvariant();
            var nombre = dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(codigo) || string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("Código y nombre son obligatorios.", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.ConceptoPlanilla.AnyAsync(c => c.Codigo == codigo))
                throw new AbrilException($"Ya existe un concepto con código \"{codigo}\".", 409);

            var maxOrden = await ctx.ConceptoPlanilla.Select(c => (int?)c.Orden).MaxAsync() ?? 0;

            var entidad = new ConceptoPlanilla
            {
                Codigo = codigo,
                Nombre = nombre,
                Tipo = dto.Tipo,
                CategoriaLaboral = dto.CategoriaLaboral,
                FormaCalculo = dto.FormaCalculo,
                Valor = dto.Valor,
                Orden = maxOrden + 1,
                Activo = true,
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.ConceptoPlanilla.Add(entidad);
            await ctx.SaveChangesAsync();
            return ToDto(entidad);
        }

        public async Task<ConceptoPlanillaDto> ActualizarConcepto(int id, ConceptoPlanillaUpdateDto dto)
        {
            ValidarConcepto(dto.Tipo, dto.FormaCalculo, dto.CategoriaLaboral);
            var nombre = dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("El nombre es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var entidad = await ctx.ConceptoPlanilla.FindAsync(id)
                ?? throw new AbrilException("Concepto de planilla no encontrado.", 404);

            entidad.Nombre = nombre;
            entidad.Tipo = dto.Tipo;
            entidad.CategoriaLaboral = dto.CategoriaLaboral;
            entidad.FormaCalculo = dto.FormaCalculo;
            entidad.Valor = dto.Valor;
            entidad.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return ToDto(entidad);
        }

        private static void ValidarConcepto(string tipo, string formaCalculo, string? categoriaLaboral)
        {
            if (!TiposValidos.Contains(tipo))
                throw new AbrilException("Tipo inválido — debe ser INGRESO, DESCUENTO o APORTE_EMPLEADOR.", 400);
            if (!FormasCalculoValidas.Contains(formaCalculo))
                throw new AbrilException("Forma de cálculo inválida.", 400);
            if (categoriaLaboral is not null && categoriaLaboral != "OBRERO" && categoriaLaboral != "EMPLEADO")
                throw new AbrilException("Categoría laboral inválida — debe ser OBRERO, EMPLEADO o vacío (ambas).", 400);
        }

        private static ConceptoPlanillaDto ToDto(ConceptoPlanilla c) => new()
        {
            Id = c.Id,
            Codigo = c.Codigo,
            Nombre = c.Nombre,
            Tipo = c.Tipo,
            CategoriaLaboral = c.CategoriaLaboral,
            FormaCalculo = c.FormaCalculo,
            Valor = c.Valor,
            Orden = c.Orden,
            Activo = c.Activo,
        };

        // ── Períodos ─────────────────────────────────────────────────────────────────────

        public async Task<List<PlanillaPeriodoListItemDto>> ListPeriodos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.PlanillaPeriodo
                .Include(p => p.Proyecto)
                .Include(p => p.Detalles)
                .OrderByDescending(p => p.Anio).ThenByDescending(p => p.Mes)
                .Select(p => new PlanillaPeriodoListItemDto
                {
                    Id = p.Id,
                    Anio = p.Anio,
                    Mes = p.Mes,
                    ProyectoNombre = p.Proyecto != null ? p.Proyecto.Nombre : null,
                    Estado = p.Estado,
                    CantidadPersonas = p.Detalles.Count,
                    TotalNeto = p.Detalles.Any() ? p.Detalles.Sum(d => d.NetoPagar) : null,
                    CreadoEn = p.CreadoEn,
                })
                .ToListAsync();
        }

        public async Task<PlanillaPeriodoDetailDto> CrearPeriodo(PlanillaPeriodoCreateDto dto)
        {
            if (dto.Mes < 1 || dto.Mes > 12) throw new AbrilException("Mes inválido.", 400);
            if (dto.Anio < 2020 || dto.Anio > 2100) throw new AbrilException("Año inválido.", 400);

            using var ctx = _factory.CreateDbContext();

            if (dto.ProyectoId.HasValue && !await ctx.Proyecto.AnyAsync(p => p.Id == dto.ProyectoId))
                throw new AbrilException("Proyecto no encontrado.", 404);

            var yaExiste = await ctx.PlanillaPeriodo.AnyAsync(p =>
                p.Anio == dto.Anio && p.Mes == dto.Mes && p.ProyectoId == dto.ProyectoId);
            if (yaExiste)
                throw new AbrilException("Ya existe un período de planilla para ese mes y proyecto.", 409);

            var periodo = new PlanillaPeriodo
            {
                Anio = dto.Anio,
                Mes = dto.Mes,
                ProyectoId = dto.ProyectoId,
                Estado = "BORRADOR",
                CreadoEn = DateTimeOffset.UtcNow,
            };
            ctx.PlanillaPeriodo.Add(periodo);
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, periodo.Id);
        }

        public async Task<PlanillaPeriodoDetailDto> GetPeriodo(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await BuildDetail(ctx, id);
        }

        public async Task<PlanillaPeriodoDetailDto> Cerrar(int periodoId)
        {
            using var ctx = _factory.CreateDbContext();
            var periodo = await ctx.PlanillaPeriodo.FindAsync(periodoId)
                ?? throw new AbrilException("Período no encontrado.", 404);

            if (periodo.Estado != "CALCULADO")
                throw new AbrilException("Solo se puede cerrar un período ya CALCULADO.", 400);

            periodo.Estado = "CERRADO";
            periodo.CerradoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, periodoId);
        }

        /// <summary>
        /// Calcula (o recalcula) todas las boletas del período. Borra e inserta de nuevo — igual
        /// que Tareo.GuardarMes — porque siempre se recalcula completo, nunca celda por celda.
        /// Solo entran personas con vínculo vigente durante el mes Y categoría laboral ya definida
        /// (sin eso no se sabe si cobran por jornal o por sueldo) — el resto queda en
        /// PersonasOmitidas, mismo criterio que el dashboard de datos faltantes.
        /// </summary>
        public async Task<PlanillaPeriodoDetailDto> Calcular(int periodoId, long calculadoPorId)
        {
            using var ctx = _factory.CreateDbContext();
            var periodo = await ctx.PlanillaPeriodo.FindAsync(periodoId)
                ?? throw new AbrilException("Período no encontrado.", 404);

            if (periodo.Estado == "CERRADO")
                throw new AbrilException("Este período está cerrado — no se puede recalcular.", 400);

            var diasEnMes = DateTime.DaysInMonth(periodo.Anio, periodo.Mes);
            var desde = new DateOnly(periodo.Anio, periodo.Mes, 1);
            var hasta = new DateOnly(periodo.Anio, periodo.Mes, diasEnMes);

            var vinculosQuery = ctx.VinculoLaboral
                .Include(v => v.Persona)
                .Where(v => v.FechaInicio <= hasta && (v.FechaFin == null || v.FechaFin >= desde));
            if (periodo.ProyectoId.HasValue)
                vinculosQuery = vinculosQuery.Where(v => v.ProyectoId == periodo.ProyectoId);

            var vinculos = await vinculosQuery.ToListAsync();
            // Una persona puede tener más de un vínculo histórico superpuesto por error de datos —
            // se queda con el más reciente por FechaInicio, mismo criterio "un vigente" del resto del sistema.
            var personaVinculo = vinculos
                .GroupBy(v => v.PersonaId)
                .Select(g => g.OrderByDescending(v => v.FechaInicio).First())
                .ToList();

            var personaIds = personaVinculo.Select(v => v.PersonaId).ToList();
            var planillas = await ctx.PersonaPlanilla.Where(pl => personaIds.Contains(pl.PersonaId)).ToListAsync();
            var planillaPorPersona = planillas.ToDictionary(pl => pl.PersonaId);

            var conceptosActivos = await ctx.ConceptoPlanilla
                .Where(c => c.Activo)
                .OrderBy(c => c.Orden)
                .ToListAsync();

            var tareoDelMes = await ctx.Tareo
                .Where(t => t.Fecha >= desde && t.Fecha <= hasta && personaIds.Contains(t.PersonaId))
                .ToListAsync();
            var tareoPorPersona = tareoDelMes.GroupBy(t => t.PersonaId).ToDictionary(g => g.Key, g => g.ToList());

            using var tx = await ctx.Database.BeginTransactionAsync();

            var detallesExistentes = await ctx.PlanillaDetalle.Where(d => d.PeriodoId == periodoId).ToListAsync();
            ctx.PlanillaDetalle.RemoveRange(detallesExistentes);
            await ctx.SaveChangesAsync();

            var omitidas = new List<string>();

            foreach (var vinculo in personaVinculo)
            {
                planillaPorPersona.TryGetValue(vinculo.PersonaId, out var planilla);
                var categoria = planilla?.CategoriaLaboral;
                if (string.IsNullOrWhiteSpace(categoria))
                {
                    omitidas.Add($"{vinculo.Persona!.Apellidos} {vinculo.Persona.Nombres} (sin categoría laboral)");
                    continue;
                }

                tareoPorPersona.TryGetValue(vinculo.PersonaId, out var registrosTareo);
                registrosTareo ??= new List<Tareo>();
                var diasTrabajados = registrosTareo.Count(t => t.TipoDia == "NORMAL");
                var diasFalta = registrosTareo.Count(t => t.TipoDia == "F");

                var detalle = new PlanillaDetalle
                {
                    PeriodoId = periodoId,
                    PersonaId = vinculo.PersonaId,
                    CategoriaLaboral = categoria,
                    SueldoBase = planilla!.SueldoBase,
                    Jornal = planilla.Jornal,
                    DiasTrabajados = diasTrabajados,
                    DiasFalta = diasFalta,
                    CreadoEn = DateTimeOffset.UtcNow,
                };

                decimal totalIngresos = 0, totalDescuentos = 0, totalAportes = 0;
                foreach (var concepto in conceptosActivos)
                {
                    if (concepto.CategoriaLaboral is not null && concepto.CategoriaLaboral != categoria) continue;

                    var monto = concepto.FormaCalculo switch
                    {
                        "FIJO" => concepto.Valor,
                        "PORCENTAJE_SUELDO" => (planilla.SueldoBase ?? 0) * concepto.Valor / 100m,
                        "PORCENTAJE_JORNAL" => (planilla.Jornal ?? 0) * diasTrabajados * concepto.Valor / 100m,
                        "POR_DIA_TAREO" => concepto.Valor * diasTrabajados,
                        _ => 0m,
                    };
                    if (monto == 0) continue;

                    detalle.Conceptos.Add(new PlanillaDetalleConcepto
                    {
                        ConceptoPlanillaId = concepto.Id,
                        ConceptoNombre = concepto.Nombre,
                        Tipo = concepto.Tipo,
                        Monto = Math.Round(monto, 2),
                    });

                    switch (concepto.Tipo)
                    {
                        case "INGRESO": totalIngresos += monto; break;
                        case "DESCUENTO": totalDescuentos += monto; break;
                        case "APORTE_EMPLEADOR": totalAportes += monto; break;
                    }
                }

                detalle.TotalIngresos = Math.Round(totalIngresos, 2);
                detalle.TotalDescuentos = Math.Round(totalDescuentos, 2);
                detalle.TotalAportesEmpleador = Math.Round(totalAportes, 2);
                detalle.NetoPagar = Math.Round(totalIngresos - totalDescuentos, 2);

                ctx.PlanillaDetalle.Add(detalle);
            }

            periodo.Estado = "CALCULADO";
            periodo.CalculadoEn = DateTimeOffset.UtcNow;
            periodo.CalculadoPorUsuarioSistemaId = calculadoPorId;
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();

            return await BuildDetail(ctx, periodoId, omitidas);
        }

        private static async Task<PlanillaPeriodoDetailDto> BuildDetail(AppDbContext ctx, int id, List<string>? omitidas = null)
        {
            var periodo = await ctx.PlanillaPeriodo
                .Include(p => p.Proyecto)
                .Include(p => p.Detalles).ThenInclude(d => d.Persona)
                .Include(p => p.Detalles).ThenInclude(d => d.Conceptos)
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new AbrilException("Período no encontrado.", 404);

            return new PlanillaPeriodoDetailDto
            {
                Id = periodo.Id,
                Anio = periodo.Anio,
                Mes = periodo.Mes,
                ProyectoNombre = periodo.Proyecto?.Nombre,
                Estado = periodo.Estado,
                CalculadoEn = periodo.CalculadoEn,
                PersonasOmitidas = omitidas ?? new List<string>(),
                Detalles = periodo.Detalles.OrderBy(d => d.Persona!.Apellidos).Select(d => new PlanillaDetalleDto
                {
                    Id = d.Id,
                    PersonaId = d.PersonaId,
                    PersonaNombre = $"{d.Persona!.Apellidos} {d.Persona.Nombres}",
                    CategoriaLaboral = d.CategoriaLaboral,
                    SueldoBase = d.SueldoBase,
                    Jornal = d.Jornal,
                    DiasTrabajados = d.DiasTrabajados,
                    DiasFalta = d.DiasFalta,
                    TotalIngresos = d.TotalIngresos,
                    TotalDescuentos = d.TotalDescuentos,
                    TotalAportesEmpleador = d.TotalAportesEmpleador,
                    NetoPagar = d.NetoPagar,
                    Conceptos = d.Conceptos.Select(c => new PlanillaDetalleConceptoDto
                    {
                        ConceptoNombre = c.ConceptoNombre,
                        Tipo = c.Tipo,
                        Monto = c.Monto,
                    }).ToList(),
                }).ToList(),
            };
        }
    }
}
