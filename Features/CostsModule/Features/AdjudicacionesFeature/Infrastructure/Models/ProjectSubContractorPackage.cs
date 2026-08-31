namespace Abril_Backend.Features.Costs.Adjudicaciones.Infrastructure.Models {
    /// <summary>
    /// Paquete generado automáticamente para el SC: fusión PDF de Hoja Resumen +
    /// Contrato + Pagaré. No tiene estado de documento ni observación porque es
    /// autogenerado, no subido manualmente.
    ///
    /// Relación: ProjectSubContractor → Package (FK en ProjectSubContractor,
    /// NO al revés). Sigue el mismo patrón que Contract, SummarySheet, Budget, etc.
    /// </summary>
    public class ProjectSubContractorPackage {
        public int ProjectSubContractorPackageId { get; set; }
        public string? FileUrl { get; set; }
        public string? OriginalFileName { get; set; }
        public string? SharepointItemId { get; set; }
        public DateTimeOffset CreatedDatetime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDatetime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
