using Abril_Backend.Application.Exceptions;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Dtos;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Models;

namespace Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Services
{
    /// <summary>
    /// Rotación circular de proyectos para la Programación de Inducciones SSOMA: cada fecha
    /// hábil de inducción (lunes, miércoles y viernes, sin feriados) se asigna al siguiente
    /// proyecto activo de la cola. El orden se define una sola vez (a mano) y de ahí en
    /// adelante la generación automática lo sigue solo; agregar un proyecto nuevo lo suma al
    /// final y entra en su turno sin reiniciar a los demás.
    /// </summary>
    public class InduccionProgramacionService : IInduccionProgramacionService
    {
        private static readonly DayOfWeek[] DiasInduccion =
            { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday };

        private readonly IInduccionProgramacionRepository _repo;
        private readonly IEmailService _emailService;

        public InduccionProgramacionService(IInduccionProgramacionRepository repo, IEmailService emailService)
        {
            _repo = repo;
            _emailService = emailService;
        }

        // ── Rotación ──────────────────────────────────────────────────────

        public async Task<List<ProyectoSimpleInduccionDto>> GetProyectosDisponiblesAsync()
        {
            // Un mismo proyecto puede tener varios turnos (uno por responsable, ej. "Oficina
            // Central" cubierto por dos personas) — ya no se excluyen los que ya están en la
            // rotación, se listan todos los proyectos vigentes.
            var activos = await _repo.GetProyectosActivosAsync();
            return activos
                .Select(p => new ProyectoSimpleInduccionDto { ProyectoId = p.ProyectoId, Nombre = p.Nombre })
                .ToList();
        }

        public Task<List<ResponsableProyectoDto>> GetResponsablesDisponiblesAsync(int proyectoId)
            => _repo.GetResponsablesDisponiblesAsync(proyectoId);

        public async Task<List<RotacionProyectoDto>> GetRotacionAsync()
        {
            var rotacion = await _repo.GetRotacionAsync();
            var responsableIds = rotacion.Where(r => r.ResponsableWorkerId.HasValue)
                .Select(r => r.ResponsableWorkerId!.Value);
            var nombres = await _repo.GetWorkerNombresAsync(responsableIds);

            return rotacion.Select(r => new RotacionProyectoDto
            {
                Id = r.Id,
                ProyectoId = r.ProyectoId,
                ProyectoNombre = r.Proyecto?.ProjectDescription ?? $"Proyecto {r.ProyectoId}",
                Orden = r.Orden,
                Activo = r.Activo,
                ResponsableWorkerId = r.ResponsableWorkerId,
                ResponsableNombre = r.ResponsableWorkerId.HasValue
                    ? (nombres.TryGetValue(r.ResponsableWorkerId.Value, out var n) ? n : $"Trabajador {r.ResponsableWorkerId}")
                    : null,
            }).ToList();
        }

        public async Task<RotacionProyectoDto> AgregarARotacionAsync(int proyectoId, int? responsableWorkerId)
        {
            var existentes = await _repo.GetRotacionAsync();
            if (existentes.Any(r => r.ProyectoId == proyectoId && r.ResponsableWorkerId == responsableWorkerId))
                throw new AbrilException("Este turno (proyecto + responsable) ya está en la rotación.", 400);

            var entity = await _repo.AgregarARotacionAsync(proyectoId, responsableWorkerId);
            var nombre = await _repo.GetProyectoNombreAsync(proyectoId);
            var responsableNombre = responsableWorkerId.HasValue
                ? (await _repo.GetWorkerNombresAsync(new[] { responsableWorkerId.Value }))
                    .GetValueOrDefault(responsableWorkerId.Value)
                : null;

            return new RotacionProyectoDto
            {
                Id = entity.Id,
                ProyectoId = entity.ProyectoId,
                ProyectoNombre = nombre,
                Orden = entity.Orden,
                Activo = entity.Activo,
                ResponsableWorkerId = entity.ResponsableWorkerId,
                ResponsableNombre = responsableNombre,
            };
        }

        public Task SetResponsableAsync(int id, int? responsableWorkerId)
            => _repo.SetResponsableAsync(id, responsableWorkerId);

        public Task ReordenarAsync(RotacionReordenarDto dto)
            => _repo.ReordenarAsync(dto.Items.Select(i => (i.Id, i.Orden)).ToList());

        public Task SetActivoAsync(int id, bool activo)
            => _repo.SetActivoAsync(id, activo);

        // ── Calendario ──────────────────────────────────────────────────────

        public async Task<List<ProgramacionInduccionDto>> GetCalendarioAsync(DateOnly desde, DateOnly hasta)
        {
            await GenerarProgramacionAsync(hasta);

            var items = await _repo.GetProgramacionAsync(desde, hasta);
            var nombresProyecto = await _repo.GetProyectoNombresAsync(items.Select(i => i.ProyectoId));
            var responsableIds = items.Where(i => i.ResponsableWorkerId.HasValue).Select(i => i.ResponsableWorkerId!.Value);
            var nombresResponsable = await _repo.GetWorkerNombresAsync(responsableIds);

            return items.Select(i => new ProgramacionInduccionDto
            {
                Id = i.Id,
                Fecha = i.Fecha,
                ProyectoId = i.ProyectoId,
                ProyectoNombre = nombresProyecto.TryGetValue(i.ProyectoId, out var n) ? n : $"Proyecto {i.ProyectoId}",
                ResponsableWorkerId = i.ResponsableWorkerId,
                ResponsableNombre = i.ResponsableWorkerId.HasValue
                    ? (nombresResponsable.TryGetValue(i.ResponsableWorkerId.Value, out var rn) ? rn : $"Trabajador {i.ResponsableWorkerId}")
                    : null,
                Estado = i.Estado,
                EsManual = i.EsManual,
                MotivoCambio = i.MotivoCambio,
                AvisoEnviado = i.AvisoEnviado,
            }).ToList();
        }

        /// <summary>
        /// Genera (si hacen falta) las fechas hábiles de inducción entre el último punto
        /// generado y <paramref name="hasta"/>, asignando cada una al siguiente proyecto activo
        /// de la rotación. No toca fechas ya generadas ni las que fueron editadas a mano.
        /// </summary>
        private async Task GenerarProgramacionAsync(DateOnly hasta)
        {
            var cursor = await _repo.GetOrCreateCursorAsync();
            var desde = cursor.UltimaFechaGenerada?.AddDays(1) ?? DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));
            if (desde > hasta) return;

            var rotacion = (await _repo.GetRotacionAsync()).Where(r => r.Activo).OrderBy(r => r.Orden).ToList();
            if (rotacion.Count == 0)
            {
                Console.WriteLine("[Inducciones] Sin proyectos activos en la rotación; no se genera calendario.");
                return;
            }

            var feriados = await _repo.GetFeriadosAsync(desde, hasta);

            var indiceActual = cursor.UltimoProyectoRotacionId.HasValue
                ? rotacion.FindIndex(r => r.Id == cursor.UltimoProyectoRotacionId.Value)
                : -1;

            for (var fecha = desde; fecha <= hasta; fecha = fecha.AddDays(1))
            {
                if (!DiasInduccion.Contains(fecha.DayOfWeek)) continue;
                if (feriados.Contains(fecha)) continue;

                indiceActual = (indiceActual + 1) % rotacion.Count;
                var proyecto = rotacion[indiceActual];
                await _repo.CrearProgramacionAsync(fecha, proyecto.ProyectoId, proyecto.ResponsableWorkerId);
            }

            var ultimoProyectoId = indiceActual >= 0 ? rotacion[indiceActual].Id : (int?)null;
            await _repo.GuardarCursorAsync(ultimoProyectoId, hasta);
        }

        // ── Edición manual ────────────────────────────────────────────────

        public async Task ReasignarAsync(int id, ProgramacionReasignarDto dto)
        {
            var programacion = await _repo.GetProgramacionByIdAsync(id)
                ?? throw new AbrilException("Fecha de inducción no encontrada.", 404);

            programacion.ProyectoId = dto.ProyectoId;
            // Cambió el proyecto: el responsable anterior pertenecía al proyecto viejo y ya no
            // aplica — queda sin asignar hasta que elijan uno nuevo para este proyecto.
            programacion.ResponsableWorkerId = null;
            programacion.EsManual = true;
            programacion.MotivoCambio = dto.Motivo;
            // El aviso ya enviado (si lo hubo) apuntaba a otro proyecto/destinatarios, así que
            // se reabre para reenviar a los correctos.
            programacion.AvisoEnviado = false;
            programacion.FechaAvisoEnviado = null;
            await _repo.GuardarProgramacionAsync(programacion);
        }

        public async Task SetProgramacionResponsableAsync(int id, int? responsableWorkerId)
        {
            var programacion = await _repo.GetProgramacionByIdAsync(id)
                ?? throw new AbrilException("Fecha de inducción no encontrada.", 404);

            programacion.ResponsableWorkerId = responsableWorkerId;
            programacion.EsManual = true;
            await _repo.GuardarProgramacionAsync(programacion);
        }

        public async Task CancelarAsync(int id, ProgramacionCancelarDto dto)
        {
            var programacion = await _repo.GetProgramacionByIdAsync(id)
                ?? throw new AbrilException("Fecha de inducción no encontrada.", 404);

            programacion.Estado = "Cancelada";
            programacion.EsManual = true;
            programacion.MotivoCambio = dto.Motivo;
            await _repo.GuardarProgramacionAsync(programacion);
        }

        public async Task ReprogramarAsync(int id, ProgramacionReprogramarDto dto)
        {
            var programacion = await _repo.GetProgramacionByIdAsync(id)
                ?? throw new AbrilException("Fecha de inducción no encontrada.", 404);

            programacion.Fecha = dto.NuevaFecha;
            programacion.EsManual = true;
            programacion.MotivoCambio = dto.Motivo;
            programacion.AvisoEnviado = false;
            programacion.FechaAvisoEnviado = null;
            await _repo.GuardarProgramacionAsync(programacion);
        }

        // ── Aviso por correo ──────────────────────────────────────────────

        /// <summary>
        /// Fecha en la que debe salir el aviso de una inducción programada para
        /// <paramref name="fechaInduccion"/>: el día hábil inmediatamente anterior a las 3pm,
        /// salvo que la inducción caiga LUNES — en ese caso el aviso sale el SÁBADO anterior (a
        /// las 10am), porque el "día antes" sería domingo. Si el día hábil anterior es feriado,
        /// se retrocede al día hábil más cercano.
        /// </summary>
        private static DateOnly FechaAvisoPara(DateOnly fechaInduccion, HashSet<DateOnly> feriados)
        {
            if (fechaInduccion.DayOfWeek == DayOfWeek.Monday)
                return fechaInduccion.AddDays(-2); // Sábado

            var d = fechaInduccion.AddDays(-1);
            while (d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday || feriados.Contains(d))
                d = d.AddDays(-1);
            return d;
        }

        public async Task<AvisoInduccionResultDto> EnviarAvisosPendientesAsync()
        {
            var result = new AvisoInduccionResultDto();
            var hoy = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(-5));

            // Ventana corta hacia adelante: solo nos interesan las inducciones de los
            // próximos días (el aviso más lejano posible es el de un lunes, avisado el
            // sábado anterior — 2 días).
            var candidatas = await _repo.GetPendientesDeAvisoAsync(hoy.AddDays(4));
            if (candidatas.Count == 0) return result;

            var feriados = await _repo.GetFeriadosAsync(hoy.AddDays(-7), hoy.AddDays(4));

            foreach (var prog in candidatas)
            {
                var fechaAviso = FechaAvisoPara(prog.Fecha, feriados);
                // Idempotente y tolerante a que el cron externo no haya corrido exactamente
                // el día del aviso: se envía si ya llegó (o pasó) esa fecha y la inducción
                // todavía no ocurrió.
                if (fechaAviso > hoy || hoy > prog.Fecha) continue;

                try
                {
                    var destinatarios = await _repo.GetDestinatariosAsync(prog.ProyectoId);
                    var to = new List<string?>
                        {
                            destinatarios.EmailCoordAdmin,
                            destinatarios.EmailCoordSsoma,
                            destinatarios.EmailResidente,
                        }
                        .Concat(destinatarios.EmailsPrevencionistas)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Select(e => e!.Trim())
                        .GroupBy(e => e.ToLowerInvariant())
                        .Select(g => g.First())
                        .ToList();

                    var nombreProyecto = await _repo.GetProyectoNombreAsync(prog.ProyectoId);

                    if (to.Count == 0)
                    {
                        result.Detalles.Add($"{nombreProyecto} ({prog.Fecha:dd/MM/yyyy}): sin destinatarios configurados, no se envió.");
                        continue;
                    }

                    var body = $@"
                    <p>Estimados,</p>
                    <p>
                        Les recordamos que el proyecto <strong>{nombreProyecto}</strong> tiene
                        programada la <strong>inducción de obra</strong> el
                        <strong>{prog.Fecha:dddd dd 'de' MMMM}</strong>.
                    </p>
                    <p>
                        Por favor coordinar con anticipación la disponibilidad de los
                        trabajadores nuevos/pendientes de inducción para ese día.
                    </p>
                    <p style='font-size: 12px; color: #666;'>
                        Este mensaje se envía automáticamente según la programación de
                        inducciones SSOMA.
                    </p>";

                    await _emailService.SendAsync(
                        to: to,
                        subject: $"🦺 Recordatorio de Inducción — {nombreProyecto} ({prog.Fecha:dd/MM/yyyy})",
                        body: body,
                        isHtml: true);

                    prog.AvisoEnviado = true;
                    prog.FechaAvisoEnviado = DateTime.UtcNow;
                    await _repo.GuardarProgramacionAsync(prog);

                    result.Enviados++;
                    result.Detalles.Add($"{nombreProyecto} ({prog.Fecha:dd/MM/yyyy}): enviado a {to.Count} destinatario(s).");
                }
                catch (Exception ex)
                {
                    result.Errores++;
                    result.Detalles.Add($"Programación #{prog.Id}: ERROR — {ex.Message}");
                }
            }

            return result;
        }
    }
}
