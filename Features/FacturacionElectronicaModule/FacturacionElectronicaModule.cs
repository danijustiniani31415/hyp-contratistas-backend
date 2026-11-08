using Abril_Backend.Features.FacturacionElectronicaModule.Application.Interfaces;
using Abril_Backend.Features.FacturacionElectronicaModule.Application.Services;
using Abril_Backend.Features.FacturacionElectronicaModule.Infrastructure.Sunat;

namespace Abril_Backend.Features.FacturacionElectronicaModule
{
    /// <summary>
    /// Facturación electrónica SUNAT (factura, nota de crédito, nota de débito, baja). Depende de que
    /// AddGuiasRemisionModule esté registrado: reutiliza su IXmlSigner (firma XMLDSig y certificado).
    /// </summary>
    public static class FacturacionElectronicaModule
    {
        public static IServiceCollection AddFacturacionElectronicaModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SunatCpeSettings>(configuration.GetSection("SunatCpe"));
            services.AddScoped<ICpeXmlBuilder, CpeUblXmlBuilder>();
            services.AddScoped<ICpeService, CpeService>();
            services.AddHttpClient<ISunatCpeClient, SunatCpeSoapClient>(c => c.Timeout = TimeSpan.FromSeconds(60))
                .ConfigureHttpClient(client => client.DefaultRequestHeaders.UserAgent.ParseAdd("HPConstructoresGenerales-LasBravas/1.0"));
            return services;
        }
    }
}
