using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.AreasYSubareasFeature.Infrastructure.Repositories;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsLearnedFeature.Infrastructure.Repositories;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonAreasFeature.Infrastructure.Repositories;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.CatalogFeature.Infrastructure.Repositories;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.ScopeFeature.Infrastructure.Repositories;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.Configuracion.LessonRemindersFeature.Infrastructure.Repositories;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Application.Services;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.MejoraContinuaModule.Features.LessonsDashboardFeature.Infrastructure.Repositories;

namespace Abril_Backend.Features.MejoraContinuaModule
{
    public static class MejoraContinuaModule
    {
        public static IServiceCollection AddMejoraContinuaModule(this IServiceCollection services)
        {
            // AreasYSubareas
            services.AddScoped<IAreaRepository, AreaRepository>();
            services.AddScoped<IAreaService, AreaService>();
            services.AddScoped<ISubAreaRepository, SubAreaRepository>();
            services.AddScoped<ISubAreaService, SubAreaService>();

            // LessonsLearned
            services.AddScoped<ILessonRepository, LessonRepository>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<ILessonJefeResolver, LessonJefeResolver>();

            // LessonAreas (filtro de areas habilitadas para Lecciones Aprendidas)
            services.AddScoped<ILessonAreaRepository, LessonAreaRepository>();
            services.AddScoped<ILessonAreaService, LessonAreaService>();

            // Catalog
            services.AddScoped<ICatalogRepository, CatalogRepository>();
            services.AddScoped<ICatalogService, CatalogService>();

            // Scope (ScopeItem + ScopeTemplate)
            services.AddScoped<IScopeRepository, ScopeRepository>();
            services.AddScoped<IScopeService, ScopeService>();

            // LessonReminders (asignación usuario-proyecto para recordatorios mensuales)
            services.AddScoped<ILessonReminderRepository, LessonReminderRepository>();
            services.AddScoped<ILessonReminderService, LessonReminderService>();

            // LessonsDashboard (dashboard de lecciones aprendidas, modelo nuevo)
            services.AddScoped<ILessonsDashboardRepository, LessonsDashboardRepository>();
            services.AddScoped<ILessonsDashboardService, LessonsDashboardService>();

            return services;
        }
    }
}
