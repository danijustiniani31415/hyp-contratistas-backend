using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories;
using Abril_Backend.Features.Ssoma.Paso.Services;
using Abril_Backend.Features.Ssoma.Rac.Services;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.OptFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.OptFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.InspeccionFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Services;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Interfaces;

using Abril_Backend.Features.SsomaModule.MiSaludFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.MiSaludFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.AccidentesIncidentesFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.AuditoriaAtsFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.DesempenoSupervisorFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.ChecklistFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.HorasHombreFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.InduccionProgramacionFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Interfaces;
using Abril_Backend.Features.SsomaModule.PetsFeature.Application.Services;
using Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.SsomaModule.PetsFeature.Infrastructure.Repositories;
using Abril_Backend.Features.SsomaModule.Shared.DescansoCertificados;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using Abril_Backend.Shared.Services.Graph.Services;

namespace Abril_Backend.Features.Ssoma
{
    public static class SsomaModule
    {
        public static IServiceCollection AddSsomaModule(this IServiceCollection services)
        {
            // Checklist SSOMA
            services.AddScoped<IChecklistRepository, ChecklistRepository>();
            services.AddScoped<IChecklistService, ChecklistService>();

            // Proyectos habilitados para SSOMA
            services.AddScoped<IProyectoHabilitadoRepository, ProyectoHabilitadoRepository>();
            services.AddScoped<IProyectoHabilitadoService, ProyectoHabilitadoService>();

            // Inhabilitaciones y Escuelitas
            services.AddScoped<Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services.SsomaInhabilitacionService>();
            services.AddScoped<Abril_Backend.Features.SsomaModule.AmonestacionesFeature.Application.Services.SsomaEscuelitaService>();

            // Catalogos
            services.AddScoped<ICatalogosRepository, CatalogosRepository>();
            services.AddScoped<ICatalogosService, CatalogosService>();

            // Clinica usuarios
            services.AddScoped<IClinicaUsuarioService, ClinicaUsuarioService>();

            // EMO
            services.AddScoped<IEmoRepository, EmoRepository>();
            services.AddScoped<IEmoService, EmoService>();

            // Convalidacion
            services.AddScoped<IConvalidacionRepository, ConvalidacionRepository>();
            services.AddScoped<IConvalidacionService, ConvalidacionService>();

            // Programacion EMO
            services.AddScoped<IProgramacionEmoRepository, ProgramacionEmoRepository>();
            services.AddScoped<IProgramacionEmoService, ProgramacionEmoService>();

            // Interconsulta
            services.AddScoped<IInterconsultaRepository, InterconsultaRepository>();
            services.AddScoped<IInterconsultaService, InterconsultaService>();

            // Dashboard
            services.AddScoped<IDashboardRepository, DashboardRepository>();
            services.AddScoped<IDashboardService, DashboardService>();

            // Workers search
            services.AddScoped<IWorkerSearchRepository, WorkerSearchRepository>();
            services.AddScoped<IWorkerSearchService, WorkerSearchService>();

            // Validación de los correos del trabajador: el corporativo contra el directorio de Abril
            // (Microsoft Graph, app-only) y contra los ya asignados en workers, el personal solo en
            // formato, y la regla de que quede al menos uno de los dos.
            services.AddScoped<IWorkerEmailValidator, WorkerEmailValidator>();
            services.AddScoped<IGraphUserService, GraphUserService>();

            // Alertas EMO (cron)
            services.AddScoped<IEmoAlertaService, EmoAlertaService>();

            // Alertas SSOMA (cron) — accidentes, descansos, reinducción, casos sociales
            services.AddScoped<ISsomaReminderService, SsomaReminderService>();

            // Auto-programación EMO (cron)
            services.AddScoped<IEmoAutoProgramacionService, EmoAutoProgramacionService>();

            // Configuración de EMOs — matriz de destinatarios de los 4 correos de EMO
            // (correo × perfil del trabajador × destinatario) y el resolver que la
            // aplica: única fuente de verdad de a quién le llega cada correo.
            services.AddScoped<IEmoCorreoConfigRepository, EmoCorreoConfigRepository>();
            services.AddScoped<IEmoCorreoConfigService, EmoCorreoConfigService>();
            services.AddScoped<IEmoDestinatariosResolver, EmoDestinatariosResolver>();

            // Resumen diario EMO (cron 4:30pm)
            services.AddScoped<IEmoResumenDiarioService, EmoResumenDiarioService>();

            // Aviso al ingresar: interconsultas pendientes + EMOs vencidos de los proyectos del
            // Administrador/Coordinador SSOMA logueado. Calculado en vivo, sin cron.
            services.AddScoped<IAlertaLoginSsomaService, AlertaLoginSsomaService>();

            // PASO — Programa Anual de Seguridad
            services.AddScoped<IPasoService, PasoService>();

            // RAC — Reporte de Actos y Condiciones Subestándar
            services.AddScoped<IRacService, RacService>();
            services.AddScoped<IPenalidadService, PenalidadService>();
            services.AddScoped<IRacSharePointService, RacSharePointService>();
            services.AddScoped<IRacNotificationService, RacNotificationService>();

            // OPT — Observación Planeada de Tarea
            services.AddScoped<IOptRepository, OptRepository>();
            services.AddScoped<IOptSharePointService, OptSharePointService>();
            services.AddScoped<IOptService, OptService>();

            // Inspecciones
            services.AddScoped<IInspeccionSharePointService, InspeccionSharePointService>();
            services.AddScoped<IInspeccionRepository, InspeccionRepository>();
            services.AddScoped<IInspeccionService, InspeccionService>();
            services.AddScoped<InspeccionPdfService>();

            // Tópico Médico
            services.AddScoped<ITopicoRepository, TopicoRepository>();
            services.AddScoped<ITopicoService, TopicoService>();

            // Accidentes de Trabajo
            services.AddScoped<IAccidenteTrabajoRepository, AccidenteTrabajoRepository>();
            services.AddScoped<IAccidenteTrabajoService, AccidenteTrabajoService>();

            // Seguimiento Médico de Accidentes (citas, equipos, alta)
            services.AddScoped<ICitaMedicaRepository, CitaMedicaRepository>();
            services.AddScoped<ICitaMedicaService, CitaMedicaService>();
            services.AddScoped<IEquipoPrestadoRepository, EquipoPrestadoRepository>();
            services.AddScoped<IEquipoPrestadoService, EquipoPrestadoService>();
            services.AddScoped<IAltaMedicaRepository, AltaMedicaRepository>();
            services.AddScoped<IAltaMedicaService, AltaMedicaService>();

            // Descansos Médicos
            services.AddScoped<IDescansoMedicoRepository, DescansoMedicoRepository>();
            services.AddScoped<IDescansoMedicoService, DescansoMedicoService>();

            // Certificados médicos: lo comparten Descansos y Mi Salud, y el destino es la
            // carpeta de SharePoint configurada en ss_descanso_carpeta.
            services.AddScoped<Abril_Backend.Shared.Services.SharePoint.Interfaces.IGraphSharePointService,
                Abril_Backend.Shared.Services.SharePoint.Services.GraphSharePointService>();
            services.AddScoped<IDescansoCertificadoStorage, DescansoCertificadoStorage>();

            // SCTR — Asistente Social
            services.AddScoped<ISctrGestionRepository, SctrGestionRepository>();
            services.AddScoped<ISctrGestionService, SctrGestionService>();

            // Mi Salud (self-service staff)
            services.AddScoped<IMiSaludRepository, MiSaludRepository>();
            services.AddScoped<IMiSaludService, MiSaludService>();

            // Asistente Social — Casos Sociales
            services.AddScoped<ICasoSocialRepository, CasoSocialRepository>();
            services.AddScoped<ISeguimientoRepository, SeguimientoRepository>();
            services.AddScoped<ICasoSocialService, CasoSocialService>();

            // Charlas y Capacitaciones
            services.AddScoped<ICharlaService, CharlaService>();
            services.AddScoped<Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Interfaces.ICharlaContratistaService,
                Abril_Backend.Features.SsomaModule.CharlasFeature.Application.Services.CharlaContratistaService>();

            // Accidentes e Incidentes
            services.AddScoped<IAccidenteIncidenteRepository, AccidenteIncidenteRepository>();
            services.AddScoped<IAccidenteIncidenteService, AccidenteIncidenteService>();

            // Auditoría ATS
            services.AddScoped<IAuditoriaAtsRepository, AuditoriaAtsRepository>();
            services.AddScoped<IAuditoriaAtsService, AuditoriaAtsService>();

            // Amonestaciones y Suspensiones
            services.AddScoped<IAmonestacionRepository, AmonestacionRepository>();
            services.AddScoped<IAmonestacionService, AmonestacionService>();
            services.AddScoped<AmonestacionNotificationService>();

            // Indicadores Proactivos
            services.AddScoped<IIndicadoresProactivosRepository, IndicadoresProactivosRepository>();
            services.AddScoped<IIndicadoresProactivosService, IndicadoresProactivosService>();
            services.AddSingleton<Abril_Backend.Features.SsomaModule.IndicadoresProactivosFeature.Infrastructure.ReactivosCacheVersion>();

            // Desempeño Supervisor
            services.AddScoped<DesempenoSupervisorRepository>();

            // Presupuesto de Materiales SSOMA
            services.AddScoped<ICatalogoMaterialesRepository, CatalogoMaterialesRepository>();
            services.AddScoped<ICatalogoMaterialesService, CatalogoMaterialesService>();
            services.AddScoped<IConsumoRepository, ConsumoRepository>();
            services.AddScoped<IEstandarizacionRepository, EstandarizacionRepository>();
            services.AddScoped<IEstandarizacionService, EstandarizacionService>();
            services.AddScoped<IConsumoService, ConsumoService>();
            services.AddScoped<IRevisionMaterialesService, RevisionMaterialesService>();
            services.AddScoped<IRatioRepository, RatioRepository>();
            services.AddScoped<IRatioService, RatioService>();
            services.AddScoped<IDriversService, DriversService>();
            services.AddScoped<IPresupuestoRepository, PresupuestoRepository>();
            services.AddScoped<IPresupuestoService, PresupuestoService>();
            services.AddScoped<IControlConsumoRepository, ControlConsumoRepository>();
            services.AddScoped<IControlConsumoService, ControlConsumoService>();
            services.AddScoped<IPersonalHitoRepository, PersonalHitoRepository>();
            services.AddScoped<IPersonalHitoService, PersonalHitoService>();
            services.AddScoped<IKitRepository, KitRepository>();
            services.AddScoped<IKitService, KitService>();
            services.AddScoped<IRatioDriverRepository, RatioDriverRepository>();
            services.AddScoped<IRatioDriverService, RatioDriverService>();
            services.AddScoped<IHhCargaRepository, HhCargaRepository>();
            services.AddScoped<IHhCargaService, HhCargaService>();

            // Horas Hombre (a partir del Tareo de Control de Acceso)
            services.AddScoped<IHorasHombreRepository, HorasHombreRepository>();
            services.AddScoped<IHorasHombreService, HorasHombreService>();

            // Programación de Inducciones (rotación por proyecto + aviso automático)
            services.AddScoped<IInduccionProgramacionRepository, InduccionProgramacionRepository>();
            services.AddScoped<IInduccionProgramacionService, InduccionProgramacionService>();

            // PETS — catálogo de pasos, piloto para que OPT (y a futuro Estándares/IPERC)
            // jalen automáticamente la estructura en vez de tipearla a mano.
            services.AddScoped<IPetsRepository, PetsRepository>();
            services.AddScoped<IPetsService, PetsService>();
            services.AddScoped<IPetsImportService, PetsImportService>();

            return services;
        }
    }
}
