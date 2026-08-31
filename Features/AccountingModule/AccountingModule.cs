using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Application.Services;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.InvoicesFeature.Infrastructure.Repositories;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Services;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Interfaces;
using Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Infrastructure.Repositories;
using Abril_Backend.Shared.Services.SharePoint.Interfaces;
using Abril_Backend.Shared.Services.SharePoint.Services;

namespace Abril_Backend.Features.AccountingModule
{
    public static class AccountingModule
    {
        public static IServiceCollection AddAccountingModule(this IServiceCollection services)
        {
            // Servicio compartido de SharePoint/OneDrive (idempotente: ya lo registra CostsModule,
            // se vuelve a registrar aquí para que el módulo sea autocontenido).
            services.AddScoped<IGraphSharePointService, GraphSharePointService>();

            // Feature: Facturas
            services.AddScoped<IInvoiceRepository, InvoiceRepository>();
            services.AddScoped<IInvoiceService, InvoiceService>();

            // Feature: Configuración → Carpeta de facturas (OneDrive/SharePoint)
            services.AddScoped<IInvoiceFolderRepository, InvoiceFolderRepository>();
            services.AddScoped<IInvoiceFolderService, InvoiceFolderService>();

            // Feature: Configuración → Firma del Gerente General

            return services;
        }
    }
}
