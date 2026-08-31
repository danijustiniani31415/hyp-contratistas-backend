using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Application.Services;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Infrastructure.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.CronogramaActividades.Infrastructure.Repositories;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Application.Services;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Infrastructure.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ProjectsDashboard.Infrastructure.Repositories;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Services;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Repositories;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Application.Services;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Repositories;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Services;
// Projects (paged-with-residents) — same feature
using IProjectsRepo = Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Interfaces.IProjectsRepository;
using ProjectsRepo  = Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Infrastructure.Repositories.ProjectsRepository;
using IProjectsSvc  = Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Interfaces.IProjectsService;
using ProjectsSvc   = Abril_Backend.Features.UnidadDeProyectosModule.Features.MilestoneScheduleFeature.Application.Services.ProjectsService;

namespace Abril_Backend.Features.UnidadDeProyectosModule
{
    public static class UnidadDeProyectosModule
    {
        public static IServiceCollection AddUnidadDeProyectosModule(this IServiceCollection services)
        {
            // ProjectsDashboard
            services.AddScoped<IProjectsDashboardRepository, ProjectsDashboardRepository>();
            services.AddScoped<IProjectsDashboardService, ProjectsDashboardService>();

            // CronogramaActividades
            services.AddScoped<ICronogramaActividadesRepository, CronogramaActividadesRepository>();
            services.AddScoped<ICronogramaActividadesService, CronogramaActividadesService>();
            services.AddScoped<ICronogramaSchedulingService, CronogramaSchedulingService>();

            // MilestoneSchedule
            services.AddScoped<IMilestoneScheduleRepository, MilestoneScheduleRepository>();
            services.AddScoped<IMilestoneScheduleService, MilestoneScheduleService>();
            services.AddScoped<IMilestoneScheduleHistoryRepository, MilestoneScheduleHistoryRepository>();
            services.AddScoped<IMilestoneScheduleHistoryService, MilestoneScheduleHistoryService>();

            // Projects (paged-with-residents)
            services.AddScoped<IProjectsRepo, ProjectsRepo>();
            services.AddScoped<IProjectsSvc, ProjectsSvc>();

            // ActasReunion (usa SharePoint para los adjuntos cuando hay carpeta configurada;
            // el registro de IGraphSharePointService es idempotente: también lo hacen otros módulos)
            services.AddScoped<IGraphSharePointService, GraphSharePointService>();
            services.AddScoped<IActasReunionRepository, ActasReunionRepository>();
            services.AddScoped<IActasReunionService, ActasReunionService>();

            return services;
        }
    }
}
