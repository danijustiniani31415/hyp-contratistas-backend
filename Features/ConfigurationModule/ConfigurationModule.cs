using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Application.Services;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.ProjectFeature.Infrastructure.Repositories;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Application.Services;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.AreaFeature.Infrastructure.Repositories;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Application.Services;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ConfigurationModule.Features.HolidayFeature.Infrastructure.Repositories;

namespace Abril_Backend.Features.ConfigurationModule
{
    public static class ConfigurationModule
    {
        public static IServiceCollection AddConfigurationModule(this IServiceCollection services)
        {
            // Project
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<IProjectService, ProjectService>();

            // Area
            services.AddScoped<IAreaTypeRepository, AreaTypeRepository>();
            services.AddScoped<IAreaTypeService, AreaTypeService>();
            services.AddScoped<IAreaItemRepository, AreaItemRepository>();
            services.AddScoped<IAreaItemService, AreaItemService>();
            services.AddScoped<IAreaScopeRepository, AreaScopeRepository>();
            services.AddScoped<IAreaScopeService, AreaScopeService>();

            // Holiday (Feriados y días no laborables)
            services.AddScoped<IHolidayRepository, HolidayRepository>();
            services.AddScoped<IHolidayService, HolidayService>();

            return services;
        }
    }
}
