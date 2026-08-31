using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Services;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Infrastructure.Repositories;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Application.Services;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.CroquisFeature.Infrastructure.Repositories;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Application.Services;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.VecinosModule.Features.ControlLicenciasFeature.Infrastructure.Repositories;

namespace Abril_Backend.Features.VecinosModule
{
    public static class VecinosModule
    {
        public static IServiceCollection AddVecinosModule(this IServiceCollection services)
        {
            services.AddScoped<IGestionVecinosRepository, GestionVecinosRepository>();
            services.AddScoped<IGestionVecinosService, GestionVecinosService>();
            services.AddScoped<ICroquisRepository, CroquisRepository>();
            services.AddScoped<ICroquisService, CroquisService>();
            services.AddScoped<IControlLicenciasRepository, ControlLicenciasRepository>();
            services.AddScoped<IControlLicenciasService, ControlLicenciasService>();
            return services;
        }
    }
}
