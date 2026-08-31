namespace Abril_Backend.Features.CostsModule.Shared.Models
{
    /// <summary>
    /// Forma de valorización de una partida (numeral 5.1 del contrato).
    /// Cada fila es una línea del desglose de valorización: un porcentaje + el concepto
    /// que aparecerá en la cláusula (p. ej. 60 + "por instalación"). El contrato concatena
    /// las líneas activas ordenadas por SortOrder en una sola oración.
    /// </summary>
    public class WorkItemValorizationForm
    {
        public int    WorkItemValorizationFormId { get; set; }
        public int    WorkItemId                 { get; set; }

        /// <summary>Concepto que sigue al porcentaje en la cláusula (p. ej. "por instalación").</summary>
        public string Concept                    { get; set; } = null!;

        /// <summary>Porcentaje de valorización (p. ej. 60).</summary>
        public decimal Percentage                { get; set; }

        public int    SortOrder                  { get; set; }
        public bool   State                      { get; set; }

        public DateTimeOffset  CreatedDatetime   { get; set; }
        public int             CreatedUserId     { get; set; }
        public DateTimeOffset? UpdatedDatetime   { get; set; }
        public int?            UpdatedUserId     { get; set; }

        // Navegación
        public WorkItem WorkItem { get; set; } = null!;
    }
}
