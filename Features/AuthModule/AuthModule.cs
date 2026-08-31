using Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Interfaces;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Application.Services;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Infrastructure.Interfaces;
using Abril_Backend.Features.AuthModule.MicrosoftLogin.Infrastructure.Repositories;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Interfaces;
using Abril_Backend.Features.AuthModule.MicrosoftProfile.Application.Services;

using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Interfaces;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Application.Services;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Infrastructure.Interfaces;
using Abril_Backend.Features.AuthModule.ContractorCredentials.Infrastructure.Repositories;

using Abril_Backend.Features.AuthModule.Role.Application.Interfaces;
using Abril_Backend.Features.AuthModule.Role.Application.Services;
using Abril_Backend.Features.AuthModule.Role.Infrastructure.Interfaces;
using Abril_Backend.Features.AuthModule.Role.Infrastructure.Repositories;
using Abril_Backend.Features.AuthModule.UserFeature.Application.Interfaces;
using Abril_Backend.Features.AuthModule.UserFeature.Application.Services;
using Abril_Backend.Features.AuthModule.UserFeature.Infrastructure.Repositories;
using Abril_Backend.Shared.Services.Graph.Interfaces;
using Abril_Backend.Shared.Services.Graph.Services;

namespace Abril_Backend.Features.AuthModule
{
    public static class AuthModule
    {
        public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IMicrosoftProfileService, MicrosoftProfileService>(client =>
            {
                client.BaseAddress = new Uri("https://graph.microsoft.com/");
            });

            services.AddScoped<IMicrosoftLoginRepository, MicrosoftLoginRepository>();
            services.AddScoped<IMicrosoftLoginService, MicrosoftLoginService>();

            // ContractorCredentials
            services.AddScoped<IContractorCredentialsRepository, ContractorCredentialsRepository>();
            services.AddScoped<IContractorCredentialsService, ContractorCredentialsService>();

            // RoleFeature
            services.AddScoped<IRoleFeatureRepository, RoleFeatureRepository>();
            services.AddScoped<IRoleFeatureService, RoleFeatureService>();

            // UserFeature
            services.AddScoped<IUserFeatureRepository, UserFeatureRepository>();
            services.AddScoped<IUserFeatureService, UserFeatureService>();

            // Validación de correos contra el directorio de Abril (Microsoft Graph, app-only).
            services.AddScoped<IGraphUserService, GraphUserService>();

            return services;
        }
    }
}
