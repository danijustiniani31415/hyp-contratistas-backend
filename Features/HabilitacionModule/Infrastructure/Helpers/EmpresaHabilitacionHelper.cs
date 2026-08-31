using Abril_Backend.Features.Habilitacion.Infrastructure.Models;

namespace Abril_Backend.Features.Habilitacion.Infrastructure.Helpers
{
    /// <summary>
    /// Calcula si una empresa está habilitada en un proyecto considerando SOLO sus entregables
    /// SSOMA (Responsable == "SSOMA") — replica el criterio que ya usa la pantalla Empresa
    /// (empresa.ts: getEstadoBadge) pero excluyendo los entregables Administracion, que no deben
    /// bloquear el ingreso de trabajadores a obra.
    /// </summary>
    public static class EmpresaHabilitacionHelper
    {
        // Mismos ids que HabilitacionDateHelper.ItemsSctrVidaLey — SCTR/VidaLey de empresa se
        // gestionan en su propia pestaña y no forman parte del cómputo genérico de habilitación.
        public static readonly HashSet<int> ItemsSctrVidaLey = new() { 15, 16 };

        private static readonly HashSet<string> EstadosAprobadoEquiv = new(StringComparer.OrdinalIgnoreCase)
        {
            "Aprobado", "No Aplica", "En Plazo"
        };

        /// <summary>
        /// Para cada (EmpresaId, ProyectoId) presente en <paramref name="registros"/>, indica si
        /// la empresa está habilitada SSOMA: sin rechazos y con todos sus ítems SSOMA vigentes
        /// (Aprobado/No Aplica/En Plazo) en el registro más reciente de cada ítem.
        /// </summary>
        /// <param name="registros">Filas de SsHabEmpresa ya filtradas a items SSOMA activos (sin
        /// SCTR/VidaLey) — se recomienda incluir <c>Item</c> o pasar itemsPorId aparte.</param>
        /// <param name="itemsSsomaIds">Ids de SsItemEmpresa activos con Responsable == "SSOMA"
        /// (excluyendo <see cref="ItemsSctrVidaLey"/>) — universo esperado de ítems por empresa.</param>
        public static Dictionary<(int EmpresaId, int ProyectoId), bool> CalcularHabilitadas(
            List<SsHabEmpresa> registros, HashSet<int> itemsSsomaIds)
        {
            var resultado = new Dictionary<(int, int), bool>();

            var porEmpresaProyecto = registros
                .Where(r => itemsSsomaIds.Contains(r.ItemId))
                .GroupBy(r => (r.EmpresaId, r.ProyectoId));

            foreach (var grupo in porEmpresaProyecto)
            {
                // Registro "vigente" por ítem: el más reciente por (Anio, Mes) — para ítems no
                // mensuales Anio/Mes son null y solo hay una fila.
                var vigentesPorItem = grupo
                    .GroupBy(r => r.ItemId)
                    .Select(g => g.OrderByDescending(r => r.Anio).ThenByDescending(r => r.Mes).First())
                    .ToList();

                var total = vigentesPorItem.Count;
                var rechazados = vigentesPorItem.Count(r => string.Equals(r.Estado, "Rechazado", StringComparison.OrdinalIgnoreCase));
                var aprobadosEquiv = vigentesPorItem.Count(r => EstadosAprobadoEquiv.Contains(r.Estado));

                resultado[grupo.Key] = rechazados == 0 && aprobadosEquiv == total && total > 0;
            }

            return resultado;
        }
    }
}
