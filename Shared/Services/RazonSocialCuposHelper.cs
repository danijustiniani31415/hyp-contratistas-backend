using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Shared.Services
{
    /// <summary>
    /// Las razones sociales activas del grupo con sus cupos disponibles: el desplegable que aparece
    /// cada vez que hay que decidir bajo cuál de las empresas de Abril entra una persona.
    ///
    /// Vive acá y no en un repositorio porque lo preguntan dos módulos —Reclutamiento
    /// (GestionGthModule), al asignarle la razón social al requerimiento, y Salud Ocupacional
    /// (SsomaModule), al programarle el EMO de ingreso a un finalista que llegó sin ninguna—, y las
    /// dos pantallas tienen que contar exactamente lo mismo. Con la cuenta duplicada bastaría con
    /// tocar una para que la otra ofreciera cupos que ya no existen.
    /// </summary>
    public static class RazonSocialCuposHelper
    {
        /// <summary>
        /// Tope de trabajadores por razón social para el cálculo de cupos. Cuentan los de Staff,
        /// Oficina Central y Personal Externo
        /// (<see cref="ObraOficinaStaffIds.ConsumenCupoRazonSocial"/>); el personal de Obra y los
        /// practicantes no consumen cupo.
        /// </summary>
        public const int TopeCupos = 20;

        /// <summary>
        /// Razones sociales operativas del grupo (<c>contributor.operativo = true</c>) ordenadas por
        /// nombre, cada una con lo que le queda de su tope. Dos roundtrips fijos: el catálogo y la
        /// ocupación agrupada, sin N+1 aunque haya una docena de empresas.
        /// </summary>
        public static async Task<List<RazonSocialCupoDto>> ListarAsync(AppDbContext ctx)
        {
            var razones = await ctx.Contributor
                .Where(c => c.State && c.Active && c.Operativo)
                .OrderBy(c => c.ContributorName)
                .Select(c => new { c.ContributorId, c.ContributorName })
                .ToListAsync();

            // Ocupación por razón social desde la base maestra: trabajadores no retirados de
            // Staff, Oficina Central o Personal Externo. El personal de Obra NO consume el tope
            // (el tope de 20 es de planilla de escritorio, y contando obreros toda razón social
            // con un proyecto en curso quedaba en 0 cupos). Los practicantes tampoco consumen.
            //
            // El practicante se detecta por `categoria_maestra_id`, no por el texto libre
            // `workers.categoria`: ese campo guarda el nivel del puesto (Operario, Arquitecto…)
            // y se desincroniza — había practicantes con "Arquitecto" contando cupo y empleados
            // que habían sido practicantes y seguían con el texto viejo sin contar. Los que no
            // tienen categoría maestra sí consumen (no son practicantes).
            var ocupados = await ctx.Worker
                .Where(w => w.ContributorId != null
                            && WorkersEstadoIds.NoRetirados.Contains(w.WorkersEstadoId)
                            && ObraOficinaStaffIds.ConsumenCupoRazonSocial.Contains(w.ObraOficinaStaffId ?? 0)
                            && w.CategoriaMaestraId != CategoriaMaestraIds.PracticantePrePro)
                .GroupBy(w => w.ContributorId!.Value)
                .Select(g => new { ContributorId = g.Key, Total = g.Count() })
                .ToListAsync();
            var ocupadosPorRazon = ocupados.ToDictionary(o => o.ContributorId, o => o.Total);

            return razones.Select(c => new RazonSocialCupoDto
            {
                Id     = c.ContributorId,
                Nombre = c.ContributorName,
                CuposDisponibles = Math.Max(0,
                    TopeCupos - ocupadosPorRazon.GetValueOrDefault(c.ContributorId)),
            }).ToList();
        }

        /// <summary>
        /// ¿Es una razón social del grupo a la que se puede asignar gente hoy? Es la revalidación de
        /// lo que ofrece <see cref="ListarAsync"/>: lo que no está en la lista tampoco se acepta,
        /// venga de donde venga el id.
        /// </summary>
        public static Task<bool> EsValidaAsync(AppDbContext ctx, int contributorId) =>
            ctx.Contributor.AnyAsync(c => c.ContributorId == contributorId
                                       && c.State && c.Active && c.Operativo);
    }

    /// <summary>Opción del desplegable "Razón social activa", con sus cupos disponibles.</summary>
    public class RazonSocialCupoDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Cupos disponibles = tope (<see cref="RazonSocialCuposHelper.TopeCupos"/>) − trabajadores
        /// vigentes de la razón social en la base maestra que son de Staff, Oficina Central o
        /// Personal Externo (el personal de Obra y los practicantes no consumen cupo). Nunca
        /// negativo (se muestra 0).
        /// </summary>
        public int CuposDisponibles { get; set; }
    }
}
