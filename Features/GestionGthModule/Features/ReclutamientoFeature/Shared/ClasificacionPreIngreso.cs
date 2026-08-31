using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// Clasificación de la ficha de pre-ingreso que se abre al aprobar a un finalista. Vive acá
    /// porque son dos los caminos que abren esa ficha —el flujo normal
    /// (<c>ReclutamientoRepository.ResolverFichaFinalistaAsync</c>) y el FFT
    /// (<see cref="FftFlujo"/>)— y tienen que dejar exactamente los mismos datos.
    ///
    /// Importa que se resuelva ya, al aprobar al finalista, y no recién en el onboarding: la
    /// matriz de destinatarios de los correos de EMO (Configuración de EMOs) elige su columna a
    /// partir de estos dos campos, y el EMO de ingreso se programa antes de que exista contrato.
    /// Sin ellos <c>EmoCorreoPerfilCodigo.Resolver</c> lee la ficha como si fuera de una
    /// contratista y no le notifica a nadie — ni a la clínica.
    /// </summary>
    internal static class ClasificacionPreIngreso
    {
        /// <summary>
        /// Valor de la columna denormalizada <c>workers.contrata_casa</c> para el personal propio.
        /// Quien entra por Reclutamiento siempre lo es: se contrata bajo una razón social de Abril
        /// (la del requerimiento), nunca bajo una contratista. Es un literal de texto y no un FK
        /// —deuda anterior de esa columna—, así que va como constante para no escribirlo a mano.
        /// </summary>
        public const string ContrataCasaPropia = "Casa";

        /// <summary>
        /// Nombre del proyecto que representa a la oficina principal. La clasificación sale del
        /// proyecto elegido en la Solicitud de Personal, igual que en
        /// <c>HabTrabajadorRepository.CambiarObraAsync</c>: ese proyecto es, por definición,
        /// personal de Oficina Central. La comparación es sin distinguir mayúsculas a propósito:
        /// en dev el registro se llama "Oficina Central" y en producción "OFICINA CENTRAL".
        /// </summary>
        private const string ProyectoOficinaCentral = "Oficina Central";

        /// <summary>
        /// Clasificación obra/oficina/staff que le toca al finalista según el proyecto que se pidió
        /// en la Solicitud de Personal: el proyecto de la oficina principal es Oficina Central y
        /// cualquier otro proyecto es Staff (personal de oficina técnica destacado en obra).
        ///
        /// Por Reclutamiento solo entran esos dos: Obra y Personal Externo no se contratan por este
        /// flujo, así que Staff es el valor correcto para todo proyecto real y no un simple
        /// "por si acaso".
        /// </summary>
        public static async Task<int> ResolverObraOficinaStaffIdAsync(AppDbContext ctx, int projectId)
        {
            var nombreProyecto = await ctx.Project.AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.ProjectDescription)
                .FirstOrDefaultAsync();

            return string.Equals(nombreProyecto?.Trim(), ProyectoOficinaCentral,
                                 StringComparison.OrdinalIgnoreCase)
                ? ObraOficinaStaffIds.OficinaCentral
                : ObraOficinaStaffIds.Staff;
        }
    }
}
