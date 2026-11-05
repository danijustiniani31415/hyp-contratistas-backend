using Abril_Backend.Features.DocumentosModule.Application.Interfaces;
using Abril_Backend.Features.DocumentosModule.Application.Services;

namespace Abril_Backend.Features.DocumentosModule
{
    /// <summary>Reconocimiento de personas (DNI) y empresas (RUC): local primero, RENIEC/SUNAT después.</summary>
    public static class DocumentosModule
    {
        public static IServiceCollection AddDocumentosModule(this IServiceCollection services)
        {
            services.AddScoped<IDocumentoService, DocumentoService>();
            return services;
        }
    }
}
