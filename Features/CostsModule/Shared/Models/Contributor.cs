using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.CostsModule.Shared.Models {
    public class Contributor
    {
        public int ContributorId { get; set; }
        public string ContributorRuc { get; set; } = null!;
        public string ContributorName { get; set; } = null!;
        public string ContributorAddress { get; set; } = null!;
        public string? ContributorEconomicActivityDescription { get; set; }
        public string? ContributorDistrict { get; set; }
        public string? ContributorProvince { get; set; }
        public string? ContributorDepartment { get; set; }
        public int? LegalRepresentativePersonId { get; set; }
        public string? LegalEntityRegistryNumber { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
        public bool EsAbril { get; set; }
        /// <summary>
        /// Razón social activa/inactiva a nivel de negocio (operación vigente del grupo).
        /// Distinto de <see cref="Active"/> (visibilidad en desplegables del sistema) y de
        /// <see cref="State"/> (soft delete). Misma esencia que <c>Project.Operativo</c>.
        /// </summary>
        public bool Operativo { get; set; }
        public string? ContributorNombreComercial { get; set; }
        public string? SpPasswordTemp { get; set; }
        [Column("email_administrador")]
        public string? EmailAdministrador { get; set; }
    }
}
