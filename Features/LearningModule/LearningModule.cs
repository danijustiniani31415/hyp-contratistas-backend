using Abril_Backend.Features.LearningModule.Application.Interfaces;
using Abril_Backend.Features.LearningModule.Application.Services;
using Abril_Backend.Features.LearningModule.Infrastructure.Interfaces;
using Abril_Backend.Features.LearningModule.Infrastructure.Repositories;

namespace Abril_Backend.Features.LearningModule
{
    /// <summary>
    /// Centro de aprendizaje y guías (videos-guía por área/módulo). Registra el servicio y
    /// el repositorio de la feature.
    /// </summary>
    public static class LearningModule
    {
        public static IServiceCollection AddLearningModule(this IServiceCollection services)
        {
            services.AddScoped<ILearningRepository, LearningRepository>();
            services.AddScoped<ILearningService, LearningService>();
            return services;
        }
    }
}
