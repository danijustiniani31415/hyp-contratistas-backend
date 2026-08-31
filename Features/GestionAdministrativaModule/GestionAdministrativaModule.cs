using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.GestionSalidas.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Lugares.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.AreaRevisores.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.DelegacionRevision.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.VisibilidadSalidas.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Application.Services;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionAdministrativa.CorreosSalida.Infrastructure.Repositories;
using Abril_Backend.Features.GestionAdministrativa.Shared.Services;

namespace Abril_Backend.Features.GestionAdministrativa
{
    public static class GestionAdministrativaModule
    {
        public static IServiceCollection AddGestionAdministrativaModule(this IServiceCollection services)
        {
            // Solicitud Salidas
            services.AddScoped<ISolicitudSalidaRepository, SolicitudSalidaRepository>();
            // El revisor/jefe del solicitante lo resuelve IJefeRevisorResolver, servicio
            // compartido registrado en Program.cs (lo usan también los correos de EMO).
            // JefeResolver (ApproverResolver): algoritmo de jerarquía SIN USO desde 2026-07-13,
            // reemplazado por JefeRevisorResolver. Se conserva el código por si se retoma.
            // services.AddScoped<IApproverResolver, ApproverResolver>();
            services.AddScoped<ISolicitudSalidaTokenService, SolicitudSalidaTokenService>();
            services.AddScoped<ISolicitudSalidaService, SolicitudSalidaService>();

            // Gestión de Salidas
            services.AddScoped<IGestionSalidaRepository, GestionSalidaRepository>();
            services.AddScoped<IGestionSalidaService, GestionSalidaService>();
            services.AddScoped<ISalidaVisibilityResolver, SalidaVisibilityResolver>();

            // Lugares (configuración)
            services.AddScoped<IGaLugarRepository, GaLugarRepository>();
            services.AddScoped<IGaLugarService, GaLugarService>();

            // Motivos de salida (configuración)
            services.AddScoped<IGaMotivoSalidaRepository, GaMotivoSalidaRepository>();
            services.AddScoped<IGaMotivoSalidaService, GaMotivoSalidaService>();

            // Trayectos (configuración: par origen-destino con monto)
            services.AddScoped<IGaTrayectoRepository, GaTrayectoRepository>();
            services.AddScoped<IGaTrayectoService, GaTrayectoService>();

            // El jefe personalizado por trabajador (workers_revisores) ya no se configura acá:
            // se asigna con el checkbox "Jefe personalizado" del formulario de trabajadores
            // (Gestión de Ingresos) y lo gestiona IJefePersonalizadoService, servicio compartido
            // registrado en Program.cs junto a IJefeRevisorResolver.

            // Carpeta de adjuntos (configuración: carpeta SharePoint/OneDrive detectada por link
            // donde se guardan los documentos adjuntos de las solicitudes de salida)
            services.AddScoped<ICarpetaAdjuntosRepository, CarpetaAdjuntosRepository>();
            services.AddScoped<ICarpetaAdjuntosService, CarpetaAdjuntosService>();

            // Revisores de áreas (configuración: n revisores por área estándar, 2do paso
            // al resolver el revisor de una salida, entre workers_revisores y el fallback GTH)
            services.AddScoped<IAreaRevisorRepository, AreaRevisorRepository>();
            services.AddScoped<IAreaRevisorService, AreaRevisorService>();

            // Visibilidad de salidas (configuración: override manual de áreas visibles por trabajador)
            services.AddScoped<IVisibilidadSalidaRepository, VisibilidadSalidaRepository>();
            services.AddScoped<IVisibilidadSalidaService, VisibilidadSalidaService>();

            // Delegación de Revisión (funcionalidad principal: el propio revisor autogestiona los
            // revisores de su área/proyecto — delegar suplentes y tomar/soltar el puesto)
            services.AddScoped<IDelegacionRevisionRepository, DelegacionRevisionRepository>();
            services.AddScoped<IDelegacionRevisionService, DelegacionRevisionService>();

            // Configuración de correos (destinatarios por correo: se enviará a / nunca se enviará a).
            services.AddScoped<ICorreoConfigRepository, CorreoConfigRepository>();
            services.AddScoped<ICorreoConfigService, CorreoConfigService>();
            // Resolver consumido por SolicitudSalidaService para armar el CC de cada correo.
            services.AddScoped<ICorreoSalidaRecipientResolver, CorreoSalidaRecipientResolver>();

            // Consolidado del S10 (PDF de respaldo de una salida ya rendida). Lo usan las dos
            // pantallas de salidas, de ahí que viva en el Shared del módulo.
            services.AddScoped<IConsolidadoS10Service, ConsolidadoS10Service>();

            return services;
        }
    }
}
