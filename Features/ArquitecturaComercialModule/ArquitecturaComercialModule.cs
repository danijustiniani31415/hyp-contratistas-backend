using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Services;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Infrastructure.Repositories;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Application.Services;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.ArquitecturaComercialModule.Features.RevisionesFeature.Infrastructure.Repositories;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Services;

namespace Abril_Backend.Features.ArquitecturaComercialModule;

public static class ArquitecturaComercialModule
{
    public static IServiceCollection AddArquitecturaComercialModule(this IServiceCollection services)
    {
        services.AddScoped<IGraphSharePointService, GraphSharePointService>();

        services.AddScoped<IObservacionRepository, ObservacionRepository>();
        services.AddScoped<IObservacionSharePointService, ObservacionSharePointService>();
        services.AddScoped<IObservacionService, ObservacionService>();

        services.AddScoped<ICatalogoRepository, CatalogoRepository>();
        services.AddScoped<ICatalogoService, CatalogoService>();

        services.AddScoped<IRevisionRepository, RevisionRepository>();
        services.AddScoped<IRevisionSharePointService, RevisionSharePointService>();
        services.AddScoped<IRevisionService, RevisionService>();

        return services;
    }
}
