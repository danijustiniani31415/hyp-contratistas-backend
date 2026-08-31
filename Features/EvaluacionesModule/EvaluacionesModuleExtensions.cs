using Abril_Backend.Features.Evaluaciones.Application.Interfaces;
using Abril_Backend.Features.Evaluaciones.Application.Services;
using Abril_Backend.Features.Evaluaciones.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Abril_Backend.Features.Evaluaciones
{
    public static class EvaluacionesModuleExtensions
    {
        public static IServiceCollection AddEvaluacionesModule(this IServiceCollection services)
        {
            services.AddScoped<IEvPeriodoRepository, EvPeriodoRepository>();
            services.AddScoped<IEvPlantillaRepository, EvPlantillaRepository>();
            services.AddScoped<IEvEvaluacionResidenteRepository, EvEvaluacionResidenteRepository>();
            services.AddScoped<IEvDashboardRepository, EvDashboardRepository>();
            services.AddScoped<IEvRecordatorioRepository, EvRecordatorioRepository>();
            services.AddScoped<IEvRecordatorioService, EvRecordatorioService>();
            services.AddScoped<IEvAsignacionSupervisorRepository, EvAsignacionSupervisorRepository>();
            services.AddScoped<IEvAsignacionSupervisorService, EvAsignacionSupervisorService>();
            services.AddScoped<IEvContratistaRepository, EvContratistaRepository>();
            services.AddScoped<IEvSupervisorContratistaRepository, EvSupervisorContratistaRepository>();
            services.AddScoped<IEvJefeSsomaRepository, EvJefeSsomaRepository>();
            services.AddScoped<IEvPrevencionistaRepository, EvPrevencionistaRepository>();
            services.AddScoped<IEvGestionSsomaRepository, EvGestionSsomaRepository>();
            return services;
        }
    }
}
