namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    /// <summary>Ítem genérico para gráficos (id opcional + etiqueta + valor).</summary>
    public class AdjudicacionChartItemDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = null!;
        public decimal Value { get; set; }
    }

    /// <summary>Ítem para gráficos de doble barra: monto total adjudicado + monto del adelanto.</summary>
    public class AdjudicacionAdvanceChartItemDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = null!;
        public decimal Total { get; set; }
        public decimal Advance { get; set; }
    }

    /// <summary>
    /// Ítem del gráfico "por estado" con el detalle breve de las adjudicaciones en ese estado
    /// ("CONTRATISTA — PARTIDA"), para mostrarlo en el tooltip de la barra.
    /// </summary>
    public class AdjudicacionEstadoChartItemDto : AdjudicacionChartItemDto
    {
        public List<string> Items { get; set; } = new();
    }

    /// <summary>Monto adjudicado acumulado por moneda.</summary>
    public class AdjudicacionMoneyByCurrencyDto
    {
        public string Code { get; set; } = null!;
        public string Symbol { get; set; } = null!;
        public decimal Total { get; set; }
    }

    /// <summary>Tarjetas resumen del dashboard.</summary>
    public class AdjudicacionDashboardSummaryDto
    {
        public int Total { get; set; }
        public int Completadas { get; set; }
        public int EnProceso { get; set; }
        public int TotalProyectos { get; set; }
        public decimal MontoPenTotal { get; set; }
        public decimal MontoUsdTotal { get; set; }
        public int PlazoPromedioDias { get; set; }
    }

    /// <summary>Opción genérica para los desplegables de filtros del dashboard (id + etiqueta).</summary>
    public class AdjudicacionOptionDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = null!;
    }

    /// <summary>
    /// Catálogos para los filtros del dashboard. Solo se envían en la primera carga
    /// (no se re-piden cuando el usuario cambia un filtro).
    /// </summary>
    public class AdjudicacionDashboardFiltersDto
    {
        public List<AdjudicacionOptionDto> Projects { get; set; } = new();
        public List<AdjudicacionOptionDto> ContractTypes { get; set; } = new();
        public List<AdjudicacionOptionDto> ContractModalities { get; set; } = new();
        public List<AdjudicacionOptionDto> PaymentMethods { get; set; } = new();
        public List<AdjudicacionOptionDto> Statuses { get; set; } = new();
    }

    /// <summary>Datos completos del dashboard de adjudicaciones (un solo endpoint).</summary>
    public class AdjudicacionDashboardDto
    {
        /// <summary>Catálogos de filtros (solo en la primera carga; null cuando el cliente ya los tiene).</summary>
        public AdjudicacionDashboardFiltersDto? Filters { get; set; }
        public AdjudicacionDashboardSummaryDto Summary { get; set; } = new();
        public List<AdjudicacionEstadoChartItemDto> PorEstado { get; set; } = new();
        public List<AdjudicacionMoneyByCurrencyDto> MontoPorMoneda { get; set; } = new();
        /// <summary>Top subcontratistas por monto adjudicado en soles (PEN).</summary>
        public List<AdjudicacionChartItemDto> TopSubcontratistasPen { get; set; } = new();
        /// <summary>Top subcontratistas por monto adjudicado en dólares (USD).</summary>
        public List<AdjudicacionChartItemDto> TopSubcontratistasUsd { get; set; } = new();
        /// <summary>
        /// Top subcontratistas por monto en soles, solo contratos con adelanto
        /// ("Contrato con adelanto" y "Pago a cuenta"), con monto total + monto del adelanto.
        /// </summary>
        public List<AdjudicacionAdvanceChartItemDto> TopSubcontratistasAdelantoPen { get; set; } = new();
        /// <summary>Igual que <see cref="TopSubcontratistasAdelantoPen"/> pero en dólares (USD).</summary>
        public List<AdjudicacionAdvanceChartItemDto> TopSubcontratistasAdelantoUsd { get; set; } = new();
        /// <summary>
        /// Adjudicaciones actualmente en el paso 2 (datos del contrato) por trabajador de Oficina
        /// Técnica del proyecto (staff_project_email tipo Oficina Técnica). Items = "RAZÓN SOCIAL — PARTIDA".
        /// </summary>
        public List<AdjudicacionEstadoChartItemDto> PendientesOtPaso2 { get; set; } = new();
        /// <summary>Igual que <see cref="PendientesOtPaso2"/> pero para el paso 4 (por enviar al SC).</summary>
        public List<AdjudicacionEstadoChartItemDto> PendientesOtPaso4 { get; set; } = new();
    }
}
