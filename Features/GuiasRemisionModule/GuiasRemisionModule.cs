using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Application.Services;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule
{
    /// <summary>Guías de Remisión Electrónica — Fase 3 (CONTEXT_LOGISTICA.md sección 3, punto 8).</summary>
    public static class GuiasRemisionModule
    {
        public static IServiceCollection AddGuiasRemisionModule(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<SunatGreSettings>(configuration.GetSection("SunatGre"));
            services.AddScoped<IGuiaRemisionService, GuiaRemisionService>();
            services.AddScoped<IGuiaRemisionXmlBuilder, GuiaRemisionUblXmlBuilder>();
            services.AddScoped<IXmlSigner, XmlDsigSigner>();
            services.AddHttpClient<ISunatGreClient, SunatGreRestClient>()
                .ConfigureHttpClient(client =>
                {
                    // .NET HttpClient no manda User-Agent por defecto — algunos firewalls/WAF
                    // (como el que está delante de la API de SUNAT, se nota por la cookie "TS...")
                    // rechazan de entrada cualquier request sin ese header, tratándolo como bot.
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("HPConstructoresGenerales-LasBravas/1.0");
                })
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var settings = sp.GetRequiredService<IOptions<SunatGreSettings>>().Value;
                    var handler = new HttpClientHandler();
                    if (settings.Ambiente != "Produccion")
                    {
                        // [SOLO BETA] El ambiente de pruebas de SUNAT (api-cpe-test.sunat.gob.pe)
                        // sirve un certificado que ni siquiera Chrome valida (NET::ERR_CERT_AUTHORITY_INVALID,
                        // confirmado navegando directo) — es un problema del lado de SUNAT en su sandbox,
                        // no de esta app ni de la red del usuario. Se desactiva la validación SOLO
                        // cuando Ambiente != "Produccion"; en Producción esta rama nunca corre.
                        handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
                    }
                    return handler;
                });
            return services;
        }
    }
}
