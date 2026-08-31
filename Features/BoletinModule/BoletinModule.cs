using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Interfaces;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Services;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Infrastructure.Repositories;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using Abril_Backend.Shared.Services.Graph.Services;

namespace Abril_Backend.Features.BoletinModule
{
    public static class BoletinModule
    {
        public static IServiceCollection AddBoletinModule(this IServiceCollection services)
        {
            // THE BIRTHDAY CLUB — calendario de cumpleaños del boletín
            services.AddScoped<ICumpleanosRepository, CumpleanosRepository>();
            services.AddScoped<ICumpleanosService, CumpleanosService>();

            // Fotos de usuarios vía Graph con permiso de aplicación (lo implementa GraphUserService).
            services.AddScoped<IUserPhotoService, GraphUserService>();

            return services;
        }
    }
}
