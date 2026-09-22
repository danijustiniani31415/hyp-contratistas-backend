using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Application.Services;
using Abril_Backend.Features.PersonasModule.Infrastructure.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Features.PersonasModule.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;

namespace Abril_Backend.Features.PersonasModule
{
    /// <summary>
    /// Fase 1 de Las Bravas / HP Constructores: modelo de Personas (persona → vínculo laboral →
    /// usuario sistema → rol/permiso/scope). Ver CONTEXT_LOGISTICA.md sección 4.
    /// </summary>
    public static class PersonasModule
    {
        public static IServiceCollection AddPersonasModule(this IServiceCollection services)
        {
            services.AddScoped<IPasswordHasher<UsuarioSistema>, PasswordHasher<UsuarioSistema>>();
            services.AddScoped<ILbJwtService, LbJwtService>();
            services.AddScoped<ILbAuthService, LbAuthService>();
            services.AddScoped<IPersonaService, PersonaService>();
            services.AddScoped<IRolPermisoService, RolPermisoService>();
            services.AddScoped<ITareoService, TareoService>();
            services.AddScoped<ICatalogoValorService, CatalogoValorService>();
            services.AddScoped<IPlanillaCalculoService, PlanillaCalculoService>();
            return services;
        }
    }
}
