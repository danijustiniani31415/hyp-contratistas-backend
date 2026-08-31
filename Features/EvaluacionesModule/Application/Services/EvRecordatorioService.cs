using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Models;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Features.Evaluaciones.Application.Services
{
    public class EvRecordatorioService : IEvRecordatorioService
    {
        private readonly IEvRecordatorioRepository _repo;
        private readonly IEvPeriodoRepository _periodoRepo;
        private readonly IEvContratistaRepository _contratistaRepo;
        private readonly IEvJefeSsomaRepository _jefeSsomaRepo;
        private readonly IEvSupervisorContratistaRepository _supervisorContratistaRepo;
        private readonly IEvGestionSsomaRepository _gestionSsomaRepo;
        private readonly IEvPrevencionistaRepository _prevencionistaRepo;
        private readonly IEmailService _email;
        private readonly ILogger<EvRecordatorioService> _logger;
        private readonly string _evaluarUrl;
        private readonly string _evaluarContratistaUrl;
        private readonly string _evaluarSupervisorContratistaUrl;
        private readonly string _evaluarJefeSsomaUrl;
        private readonly string _evaluarGestionSsomaUrl;
        private readonly string _evaluarPrevencionistaUrl;
        private const string GerenteProyectosEmail = "coriundo@abril.pe";

        public EvRecordatorioService(
            IEvRecordatorioRepository repo,
            IEvPeriodoRepository periodoRepo,
            IEvContratistaRepository contratistaRepo,
            IEvJefeSsomaRepository jefeSsomaRepo,
            IEvSupervisorContratistaRepository supervisorContratistaRepo,
            IEvGestionSsomaRepository gestionSsomaRepo,
            IEvPrevencionistaRepository prevencionistaRepo,
            IEmailService email,
            ILogger<EvRecordatorioService> logger,
            IConfiguration configuration)
        {
            _repo = repo;
            _periodoRepo = periodoRepo;
            _contratistaRepo = contratistaRepo;
            _jefeSsomaRepo = jefeSsomaRepo;
            _supervisorContratistaRepo = supervisorContratistaRepo;
            _gestionSsomaRepo = gestionSsomaRepo;
            _prevencionistaRepo = prevencionistaRepo;
            _email = email;
            _logger = logger;
            var frontendUrl = configuration["App:FrontendUrl"]?.TrimEnd('/');
            _evaluarUrl = $"{frontendUrl}/evaluaciones/evaluar";
            _evaluarContratistaUrl = $"{frontendUrl}/evaluaciones/evaluar-contratista";
            _evaluarSupervisorContratistaUrl = $"{frontendUrl}/evaluaciones/evaluar-supervisor-contratista";
            _evaluarJefeSsomaUrl = $"{frontendUrl}/evaluaciones/evaluar-jefe-ssoma";
            _evaluarGestionSsomaUrl = $"{frontendUrl}/evaluaciones/gestion-ssoma";
            // Ojo: a diferencia del resto, esta pantalla vive en el portal contratista
            // (módulo "habilitacion"), no en "evaluaciones" — confirmado en
            // habilitacion.routes.ts (path: 'evaluar-prevencionista').
            _evaluarPrevencionistaUrl = $"{frontendUrl}/habilitacion/evaluar-prevencionista";
        }

        // Un solo correo por persona con TODAS sus evaluaciones pendientes (Residentes,
        // Contratistas, Jefe SSOMA, Supervisor de Contratista, Gestión SSOMA), en vez de
        // uno por cada flujo. Reutiliza sin cambios la lógica de elegibilidad/pendientes
        // de cada repositorio — cada Agregar*Async solo aporta ítems a un digest en vez
        // de enviar su propio correo.
        public async Task<object> ProcesarRecordatoriosAsync()
        {
            // Autogestiona la apertura/cierre/activación del período (residentes y contratistas
            // comparten la misma tabla ev_periodo) para que no dependa de un paso manual mensual.
            await _periodoRepo.SincronizarVigenciaAsync();

            var periodo = await _periodoRepo.GetActivoAsync();
            if (periodo == null)
                return new { mensaje = "Sin período activo", enviados = 0 };

            var hoy = DateOnly.FromDateTime(DateTime.UtcNow);
            if (hoy < periodo.FechaApertura || hoy > periodo.FechaCierre)
                return new { mensaje = "Fuera de ventana de recordatorio", enviados = 0 };

            // Día 25 = primer aviso a todos (Residentes/Contratistas); resto = solo pendientes.
            // Jefe SSOMA / Supervisor de Contratista / Gestión SSOMA no tienen "aviso de
            // apertura": solo recuerdan lo que sigue pendiente, cualquier día de la ventana.
            bool esPrimerDia = hoy.Day == periodo.FechaApertura.Day;
            var mesAnio = new DateTime(periodo.Anio, periodo.Mes, 1)
                .ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-PE"));

            var digest = new Dictionary<string, PersonaDigest>(StringComparer.OrdinalIgnoreCase);

            await AgregarResidentesAsync(digest, periodo, esPrimerDia);
            await AgregarContratistasAsync(digest, periodo, esPrimerDia);
            await AgregarJefeSsomaAsync(digest, periodo);
            await AgregarSupervisorContratistaAsync(digest, periodo);
            await AgregarGestionSsomaAsync(digest, periodo);
            await AgregarPrevencionistaAsync(digest, periodo);

            var tipoLog = esPrimerDia ? "DIGEST_PRIMER_AVISO" : $"DIGEST_DIA_{hoy.Day}";
            int enviados = 0;

            foreach (var persona in digest.Values)
            {
                if (persona.Items.Count == 0) continue;

                // Evitar duplicado si el cron corre más de una vez al día.
                if (await _repo.YaEnvioRecordatorioHoyAsync(periodo.Id, persona.UserId, tipoLog))
                    continue;

                var asunto = $"[Evaluaciones] Tienes {persona.Items.Count} pendiente(s) — {mesAnio}";
                var cuerpo = BuildCuerpoDigest(persona, mesAnio, periodo.FechaCierre);

                try
                {
                    await _email.SendAsync(to: [persona.Email], subject: asunto, body: cuerpo, isHtml: true, cc: null);
                    await _repo.RegistrarLogAsync(periodo.Id, persona.UserId, tipoLog, persona.Email, ccJefatura: false, ccGerencia: false);
                    enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando digest de evaluaciones a {Email}", persona.Email);
                }
            }

            return new
            {
                mensaje = "OK",
                fecha = hoy.ToString("yyyy-MM-dd"),
                esPrimerDia,
                enviados,
                personasConPendientes = digest.Values.Count(p => p.Items.Count > 0)
            };
        }

        private static PersonaDigest ObtenerOCrear(Dictionary<string, PersonaDigest> digest, int? userId, string email, string nombre)
        {
            if (!digest.TryGetValue(email, out var persona))
            {
                persona = new PersonaDigest { UserId = userId, Email = email, Nombre = nombre };
                digest[email] = persona;
            }
            else if (persona.UserId == null && userId != null)
            {
                persona.UserId = userId; // completa el id si una fuente anterior no lo trajo
            }
            return persona;
        }

        private async Task AgregarResidentesAsync(Dictionary<string, PersonaDigest> digest, EvPeriodo periodo, bool esPrimerDia)
        {
            var evaluadores = await _repo.GetEvaluadoresPendientesAsync(periodo.Id, !esPrimerDia);
            foreach (var ev in evaluadores)
            {
                var p = ObtenerOCrear(digest, ev.UserId, ev.EmailCorporativo, ev.NombreCompleto);
                p.Items.Add(new PendienteItem(
                    "Evaluación de Residentes",
                    esPrimerDia
                        ? "Se inicia el período de evaluación de residentes."
                        : "Aún tienes evaluaciones de residentes pendientes.",
                    _evaluarUrl));
            }
        }

        private async Task AgregarContratistasAsync(Dictionary<string, PersonaDigest> digest, EvPeriodo periodo, bool esPrimerDia)
        {
            var candidatos = await _contratistaRepo.GetEvaluadoresCandidatosAsync();
            foreach (var ev in candidatos)
            {
                if (ev.UserId == null) continue;

                var inicio = await _contratistaRepo.GetInicioAsync(ev.UserId.Value);
                if (inicio.ContratistasAEvaluar.Count == 0)
                    continue; // no tiene contratistas asignados este período

                var pendientes = inicio.ContratistasAEvaluar.Count(c => !c.YaEvalue);
                if (pendientes == 0 && !esPrimerDia)
                    continue; // ya evaluó a todos, no molestar salvo el primer aviso

                var p = ObtenerOCrear(digest, ev.UserId, ev.EmailCorporativo, ev.NombreCompleto);
                p.Items.Add(new PendienteItem(
                    "Evaluación de Contratistas",
                    esPrimerDia
                        ? "Se inicia el período de evaluación de contratistas."
                        : $"Tienes {pendientes} contratista(s) pendiente(s) de evaluar.",
                    _evaluarContratistaUrl));
            }
        }

        private async Task AgregarJefeSsomaAsync(Dictionary<string, PersonaDigest> digest, EvPeriodo periodo)
        {
            var cumplimiento = await _jefeSsomaRepo.GetCumplimientoAsync(periodo.Id);
            foreach (var pend in cumplimiento.Pendientes)
            {
                var p = ObtenerOCrear(digest, pend.UserId, pend.EmailCorporativo, pend.NombreCompleto);
                p.Items.Add(new PendienteItem(
                    "Evaluación al Jefe SSOMA",
                    "Evaluación anónima y obligatoria — aún no la completas.",
                    _evaluarJefeSsomaUrl));
            }
        }

        private async Task AgregarSupervisorContratistaAsync(Dictionary<string, PersonaDigest> digest, EvPeriodo periodo)
        {
            var candidatos = await _supervisorContratistaRepo.GetEvaluadoresCandidatosAsync();
            foreach (var ev in candidatos)
            {
                if (ev.UserId == null) continue;

                var inicio = await _supervisorContratistaRepo.GetInicioAsync(ev.UserId.Value);
                if (inicio.SupervisoresAEvaluar.Count == 0 || inicio.YaMarcoNoAplica)
                    continue;

                var pendientes = inicio.SupervisoresAEvaluar.Count(s => !s.YaEvalue);
                if (pendientes == 0)
                    continue;

                var p = ObtenerOCrear(digest, ev.UserId, ev.EmailCorporativo, ev.NombreCompleto);
                p.Items.Add(new PendienteItem(
                    "Evaluación de Supervisores de Contratista",
                    $"Tienes {pendientes} supervisor(es) de contratista pendiente(s) de evaluar.",
                    _evaluarSupervisorContratistaUrl));
            }
        }

        private async Task AgregarGestionSsomaAsync(Dictionary<string, PersonaDigest> digest, EvPeriodo periodo)
        {
            var cumplimiento = await _gestionSsomaRepo.GetCumplimientoAsync(periodo.Id);
            foreach (var grupo in cumplimiento.Pendientes.GroupBy(x => x.UserId))
            {
                var primero = grupo.First();
                var p = ObtenerOCrear(digest, primero.UserId, primero.EmailCorporativo, primero.NombreCompleto);
                var relaciones = grupo.Select(g => g.Relacion).Distinct().ToList();
                p.Items.Add(new PendienteItem(
                    "Evaluación de Gestión SSOMA",
                    DescribirPendientesGestionSsoma(relaciones, grupo.Count()),
                    _evaluarGestionSsomaUrl));
            }
        }

        private async Task AgregarPrevencionistaAsync(Dictionary<string, PersonaDigest> digest, EvPeriodo periodo)
        {
            var candidatos = await _prevencionistaRepo.GetEvaluadoresCandidatosAsync();
            foreach (var ev in candidatos)
            {
                if (ev.ProyectoIds.Count == 0) continue;

                var inicio = await _prevencionistaRepo.GetInicioAsync(ev.UserId, ev.ContributorId, ev.ProyectoIds);
                if (inicio.AEvaluar.Count == 0) continue;

                var pendientes = inicio.AEvaluar.Count(a => !a.YaEvalue);
                if (pendientes == 0) continue;

                var p = ObtenerOCrear(digest, ev.UserId, ev.Email, ev.Nombre);
                p.Items.Add(new PendienteItem(
                    "Evaluación de Prevencionistas / Coordinadores SSOMA",
                    $"Tienes {pendientes} persona(s) del equipo SSOMA pendiente(s) de evaluar.",
                    _evaluarPrevencionistaUrl));
            }
        }

        private static string DescribirPendientesGestionSsoma(List<string> relaciones, int total)
        {
            // D4 es la evaluación anónima (Prevencionista -> su Coordinador SSOMA); si es
            // la única pendiente, el mensaje lo deja explícito para que no la confunda con
            // las identificadas.
            if (relaciones.Count == 1 && relaciones[0] == "D4")
                return "Evaluación anónima a tu Coordinador SSOMA pendiente.";
            return $"Tienes {total} evaluación(es) de liderazgo/gestión pendiente(s).";
        }

        public async Task<object> ProcesarDescargoAsync()
        {
            // Asegura que el período de ayer haya quedado desactivado antes de buscarlo,
            // sin depender de que el cron de "enviar" haya corrido primero.
            await _periodoRepo.SincronizarVigenciaAsync();

            var periodo = await _repo.GetPeriodoCerradoAyerAsync();
            if (periodo == null)
                return new { mensaje = "Sin período cerrado ayer", enviados = 0 };

            var noEvaluaron = await _repo.GetEvaluadoresPendientesAsync(periodo.Id, true);

            var mesAnio = new DateTime(periodo.Anio, periodo.Mes, 1)
                .ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-PE"));

            int enviados = 0;
            foreach (var ev in noEvaluaron)
            {
                var cc = new List<string> { GerenteProyectosEmail };
                if (!string.IsNullOrEmpty(ev.JefeEmail) && ev.JefeEmail != GerenteProyectosEmail)
                    cc.Add(ev.JefeEmail);

                var asunto = $"[Evaluación Residentes] Solicitud de descargo — {mesAnio}";
                var cuerpo = BuildCuerpoDescargo(ev, mesAnio);

                try
                {
                    await _email.SendAsync(
                        to: [ev.EmailCorporativo],
                        subject: asunto,
                        body: cuerpo,
                        isHtml: true,
                        cc: cc);

                    await _repo.RegistrarLogAsync(
                        periodo.Id, ev.UserId, "DESCARGO",
                        ev.EmailCorporativo,
                        ccJefatura: true,
                        ccGerencia: true);

                    enviados++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error enviando descargo a {Email}", ev.EmailCorporativo);
                }
            }

            return new { mensaje = "OK", enviados, periodo = mesAnio };
        }

        public async Task<object> ProcesarDiarioAsync()
        {
            var recordatorios = await ProcesarRecordatoriosAsync();
            var descargo = await ProcesarDescargoAsync();
            return new { recordatorios, descargo };
        }

        private static string BuildCuerpoDigest(PersonaDigest persona, string mesAnio, DateOnly fechaCierre)
        {
            var items = string.Join("", persona.Items.Select(i => $@"
      <div style='margin:16px 0;padding:16px;background:#fff;border:1px solid #e2e8f0;border-radius:8px'>
        <h3 style='margin:0 0 6px;font-size:15px;color:#1E3A5F'>{i.Titulo}</h3>
        <p style='margin:0 0 12px;color:#334155'>{i.Descripcion}</p>
        <a href='{i.Url}'
           style='background:#1E3A5F;color:#fff;padding:8px 20px;border-radius:6px;text-decoration:none;font-weight:bold;font-size:14px'>
          Ir a evaluar
        </a>
      </div>"));

            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px'>
  <div style='background:#1E3A5F;padding:16px 24px;border-radius:8px 8px 0 0'>
    <h2 style='color:#fff;margin:0;font-size:18px'>Evaluaciones pendientes — {mesAnio}</h2>
  </div>
  <div style='background:#f8fafc;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Estimado/a <strong>{persona.Nombre}</strong>,</p>
    <p>Tienes {persona.Items.Count} evaluación(es) pendiente(s) de completar en la plataforma Abril:</p>
    {items}
    <p style='color:#64748b;font-size:0.85rem;margin-top:16px'>
      El período cierra el {fechaCierre:dd/MM/yyyy}. Si tienes consultas, contacta a tu jefe directo.
    </p>
  </div>
</div>";
        }

        private static string BuildCuerpoDescargo(EvaluadorDto ev, string mesAnio)
        {
            return $@"
<div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px'>
  <div style='background:#dc2626;padding:16px 24px;border-radius:8px 8px 0 0'>
    <h2 style='color:#fff;margin:0;font-size:18px'>Solicitud de Descargo — Evaluación {mesAnio}</h2>
  </div>
  <div style='background:#f8fafc;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 8px 8px'>
    <p>Estimado/a <strong>{ev.NombreCompleto}</strong>,</p>
    <p>El período de evaluación de residentes correspondiente a <strong>{mesAnio}</strong>
       ha concluido y <strong>no se registra ninguna evaluación</strong> de su parte.</p>
    <p>Se le solicita remitir el <strong>descargo correspondiente</strong> explicando
       los motivos por los cuales no completó las evaluaciones en el plazo establecido.</p>
    <p>Este correo ha sido enviado con copia a la Gerencia de Proyectos y a su jefe directo.</p>
    <hr style='border:none;border-top:1px solid #e2e8f0;margin:20px 0'>
    <p style='color:#64748b;font-size:0.85rem'>
      Sistema de Evaluaciones — Abril Grupo Inmobiliario
    </p>
  </div>
</div>";
        }

        private class PersonaDigest
        {
            public int? UserId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public List<PendienteItem> Items { get; } = [];
        }

        private record PendienteItem(string Titulo, string Descripcion, string Url);
    }
}
