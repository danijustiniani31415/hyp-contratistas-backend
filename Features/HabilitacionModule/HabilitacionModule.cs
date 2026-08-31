using Abril_Backend.Features.Habilitacion.Application.Interfaces;
using Abril_Backend.Features.Habilitacion.Application.Services;
using Abril_Backend.Features.Habilitacion.Infrastructure.Interfaces;
using Abril_Backend.Features.Habilitacion.Infrastructure.Repositories;

namespace Abril_Backend.Features.Habilitacion
{
    public static class HabilitacionModule
    {
        public static IServiceCollection AddHabilitacionModule(this IServiceCollection services)
        {
            services.AddHttpClient();

            services.AddScoped<IContratistaAuthService, ContratistaAuthService>();
            services.AddScoped<IObreroAuthService, ObreroAuthService>();
            services.AddScoped<ICatalogosHabilitacionRepository, CatalogosHabilitacionRepository>();
            services.AddScoped<IEmpresaContratistaRepository, EmpresaContratistaRepository>();
            services.AddScoped<IHabTrabajadorRepository, HabTrabajadorRepository>();
            services.AddScoped<IBandejaRepository, BandejaRepository>();
            services.AddScoped<IReglasTrabajadorRepository, ReglasTrabajadorRepository>();
            services.AddScoped<IHabEmpresaRepository, HabEmpresaRepository>();
            services.AddScoped<ISctrVidaLeyRepository, SctrVidaLeyRepository>();
            services.AddScoped<IEquipoRepository, EquipoRepository>();
            services.AddScoped<IProyectoHabRepository, ProyectoHabRepository>();
            services.AddScoped<IInduccionRepository, InduccionRepository>();
            services.AddScoped<IControlAccesoRepository, ControlAccesoRepository>();
            services.AddSingleton<ISharePointHabService, SharePointHabService>();
            services.AddScoped<ITrabajadorRestringidoRepository, TrabajadorRestringidoRepository>();
            services.AddScoped<ITrabajadorRestringidoService, TrabajadorRestringidoService>();
            services.AddScoped<IContratistaUsuarioRepository, ContratistaUsuarioRepository>();
            services.AddScoped<IContratistaUsuarioService, ContratistaUsuarioService>();
            services.AddScoped<IVigenciaRevisionService, VigenciaRevisionService>();
            services.AddScoped<IDashboardHabRepository, DashboardHabRepository>();
            services.AddScoped<IRetiroAutomaticoService, RetiroAutomaticoService>();
            services.AddScoped<IResponsablesRepository, ResponsablesRepository>();

            // Dossier Semanal
            services.AddScoped<IDossierRepository, DossierRepository>();
            services.AddScoped<IDossierService, DossierService>();

            return services;
        }
    }
}
