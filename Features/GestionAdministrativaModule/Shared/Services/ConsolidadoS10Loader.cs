using Abril_Backend.Features.GestionAdministrativa.Shared.Dtos;
using Abril_Backend.Features.GestionAdministrativa.Shared.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Services
{
    /// <summary>
    /// Resuelve en lote el Consolidado del S10 vigente de N solicitudes de salida, para las tablas
    /// y detalles de las dos pantallas de salidas (evita el N+1). Es un loader estático sobre el
    /// contexto —igual que <see cref="GaAreaTreeLoader"/>— para que lo puedan usar tanto los
    /// repositorios como <see cref="ConsolidadoS10Service"/> sin duplicar la regla de precedencia.
    ///
    /// Precedencia: si la salida tiene su propio consolidado, ese manda; si no, hereda el de su
    /// planilla de rendición (el caso normal, un consolidado por planilla).
    /// </summary>
    public static class ConsolidadoS10Loader
    {
        /// <param name="rendicionPorSolicitud">solicitudId → rendicionId (null si no está rendida).</param>
        public static async Task<Dictionary<int, ConsolidadoS10Dto>> LoadAsync(
            AppDbContext ctx,
            IReadOnlyDictionary<int, int?> rendicionPorSolicitud)
        {
            if (rendicionPorSolicitud.Count == 0) return new();

            var solicitudIds = rendicionPorSolicitud.Keys.ToList();
            var rendicionIds = rendicionPorSolicitud.Values
                .Where(r => r != null)
                .Select(r => r!.Value)
                .Distinct()
                .ToList();

            var filas = await ctx.GaConsolidadoS10
                .Where(c => c.State && (
                    (c.SolicitudId != null && solicitudIds.Contains(c.SolicitudId.Value)) ||
                    (c.RendicionId != null && rendicionIds.Contains(c.RendicionId.Value))))
                .ToListAsync();

            if (filas.Count == 0) return new();

            var porSolicitud = filas.Where(c => c.SolicitudId != null)
                .ToDictionary(c => c.SolicitudId!.Value, c => c);
            var porRendicion = filas.Where(c => c.RendicionId != null)
                .ToDictionary(c => c.RendicionId!.Value, c => c);

            var result = new Dictionary<int, ConsolidadoS10Dto>(solicitudIds.Count);
            foreach (var (solicitudId, rendicionId) in rendicionPorSolicitud)
            {
                if (porSolicitud.TryGetValue(solicitudId, out var propio))
                {
                    result[solicitudId] = ToDto(propio);
                    continue;
                }
                if (rendicionId != null && porRendicion.TryGetValue(rendicionId.Value, out var deRendicion))
                    result[solicitudId] = ToDto(deRendicion);
            }
            return result;
        }

        public static ConsolidadoS10Dto ToDto(GaConsolidadoS10 c) => new()
        {
            Id          = c.Id,
            Ambito      = c.RendicionId != null
                            ? ConsolidadoS10Ambito.Rendicion.ToString()
                            : ConsolidadoS10Ambito.Solicitud.ToString(),
            PdfUrl      = c.PdfUrl,
            PdfFilename = c.PdfFilename,
            UploadedAt  = c.UploadedAt,
        };
    }
}
