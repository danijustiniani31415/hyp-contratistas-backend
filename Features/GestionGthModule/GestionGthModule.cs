using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Services;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Repositories;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Services;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Infrastructure.Repositories;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Services;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interceptors;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Repositories;

namespace Abril_Backend.Features.GestionGthModule
{
    /// <summary>
    /// Módulo Gestión GTH (Talento Humano): Reclutamiento, Onboarding y Base maestra.
    /// Por ahora solo registra la feature de Reclutamiento (formulario de solicitud de personal).
    /// </summary>
    public static class GestionGthModule
    {
        public static IServiceCollection AddGestionGthModule(this IServiceCollection services)
        {
            // Reclutamiento
            services.AddScoped<IReclutamientoRepository, ReclutamientoRepository>();
            services.AddScoped<IReclutamientoService, ReclutamientoService>();

            // Alcance de «Solicitud de Personal»: qué requerimientos ve cada usuario (los de su
            // área y las que cuelgan de ella) y si puede moverlos (solo la jefatura).
            services.AddScoped<ISolicitudPersonalScopeResolver, SolicitudPersonalScopeResolver>();

            // Bitácora de fases del requerimiento. Es un interceptor de EF y no un servicio de la
            // feature porque el estado se mueve desde una docena de sitios: acá se registra y en
            // Program.cs se engancha al DbContext (ver RequerimientoEstadoHistorialInterceptor).
            services.AddSingleton<RequerimientoEstadoHistorialInterceptor>();

            // Archivos del requerimiento en SharePoint (CVs y anexos de la long list, archivos del
            // informe, CV documentado del postulante). Lo comparten la bandeja de GTH y la página
            // pública del formulario: todos los archivos de un requerimiento van a la misma carpeta.
            services.AddScoped<IReclutamientoArchivoStorage, ReclutamientoArchivoStorage>();

            // Formulario de información del postulante (público por token + revisión de GTH)
            services.AddScoped<IPostulanteFormularioRepository, PostulanteFormularioRepository>();
            services.AddScoped<IPostulanteFormularioService, PostulanteFormularioService>();

            // Aprobación de la solicitud (gerente del área + Gerencia General). El resolver define
            // qué solicitudes ve cada usuario y con qué poder decide, según la categoría de su
            // ficha de trabajador.
            services.AddScoped<IAprobacionScopeResolver, AprobacionScopeResolver>();
            services.AddScoped<IAprobacionGgRepository, AprobacionGgRepository>();
            services.AddScoped<IAprobacionGgService, AprobacionGgService>();

            // Configuración de los correos del flujo (pantalla /solicitud-personal/configuracion)
            services.AddScoped<ICorreoConfigRepository, CorreoConfigRepository>();
            services.AddScoped<ICorreoConfigService, CorreoConfigService>();

            // Destinatarios efectivos de cada correo: lo comparten el envío real y la
            // previsualización del modal, así que no puede haber dos versiones.
            services.AddScoped<ICorreoDestinatariosResolver, CorreoDestinatariosResolver>();

            // Reclutadores (Configuración): quiénes del área de GTH salen en el desplegable
            // "Responsable del proceso". Es una tabla filtro aparte de workers: activar o
            // desactivar acá no toca la ficha del trabajador.
            services.AddScoped<IReclutadoresRepository, ReclutadoresRepository>();
            services.AddScoped<IReclutadoresService, ReclutadoresService>();

            // Onboarding: la fase que sigue a Reclutamiento (carta oferta → base maestra).
            services.AddScoped<IOnboardingRepository, OnboardingRepository>();
            services.AddScoped<IOnboardingService, OnboardingService>();

            // File digital del colaborador en SharePoint. Lo comparten la pantalla de GTH y la página
            // pública de firma: las dos tienen que dejar los documentos en la misma carpeta.
            services.AddScoped<IFileDigitalColaboradorService, FileDigitalColaboradorService>();

            // Página pública donde el postulante ve su carta oferta, registra su firma y la firma
            // (acceso por token, sin login).
            services.AddScoped<ICartaOfertaFirmaRepository, CartaOfertaFirmaRepository>();
            services.AddScoped<ICartaOfertaFirmaService, CartaOfertaFirmaService>();
            return services;
        }
    }
}
