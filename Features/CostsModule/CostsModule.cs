using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Repositories;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Interfaces;
using Abril_Backend.Features.Costs.Adjudicaciones.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.StaffProjectEmailFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemCategoryFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkItemFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.ProjectLinkFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.CostosPresupuestosEmailFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.CronogramaFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Services;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Infrastructure.Repositories;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Interfaces;
using Abril_Backend.Features.CostsModule.Features.Configuration.WorkSpecialtyFeature.Application.Services;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using Abril_Backend.Shared.Services.Graph.Services;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Options;
using Abril_Backend.Shared.Services.SharePoint.Services;

namespace Abril_Backend.Features.Costs
{
    public static class CostsModule
    {
        public static IServiceCollection AddCostsModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OneDriveOptions>(configuration.GetSection("OneDrive"));
            // Shared
            services.AddScoped<IGraphUserService, GraphUserService>();
            services.AddScoped<IGraphSharePointService, GraphSharePointService>();

            // Adjudicaciones
            services.AddScoped<IProjectSubContractorRepository, ProjectSubContractorRepository>();
            services.AddScoped<IProjectSubContractorService, ProjectSubContractorService>();
            services.AddScoped<IAdjudicacionOneDriveStorage, AdjudicacionOneDriveStorage>();

            // StaffProjectEmail
            services.AddScoped<IStaffProjectEmailRepository, StaffProjectEmailRepository>();
            services.AddScoped<IStaffProjectEmailService, StaffProjectEmailService>();

            // WorkItemCategory
            services.AddScoped<IWorkItemCategoryRepository, WorkItemCategoryRepository>();
            services.AddScoped<IWorkItemCategoryService, WorkItemCategoryService>();

            // WorkItem
            services.AddScoped<IWorkItemRepository, WorkItemRepository>();
            services.AddScoped<IWorkItemService, WorkItemService>();

            // ProjectLink
            services.AddScoped<IProjectLinkRepository, ProjectLinkRepository>();
            services.AddScoped<IProjectLinkService, ProjectLinkService>();

            // CostosPresupuestosEmail
            services.AddScoped<ICostosPresupuestosEmailRepository, CostosPresupuestosEmailRepository>();
            services.AddScoped<ICostosPresupuestosEmailService, CostosPresupuestosEmailService>();

            // Cronograma de adjudicaciones
            services.AddScoped<ICostosCronogramaRepository, CostosCronogramaRepository>();
            services.AddScoped<ICostosCronogramaService, CostosCronogramaService>();

            // Carpeta de adjudicaciones por proyecto (OneDrive/SharePoint)
            services.AddScoped<IAdjudicacionFolderRepository, AdjudicacionFolderRepository>();
            services.AddScoped<IAdjudicacionFolderService, AdjudicacionFolderService>();

            // Especialidades
            services.AddScoped<IWorkSpecialtyRepository, WorkSpecialtyRepository>();
            services.AddScoped<IWorkSpecialtyService, WorkSpecialtyService>();

            return services;
        }
    }
}
