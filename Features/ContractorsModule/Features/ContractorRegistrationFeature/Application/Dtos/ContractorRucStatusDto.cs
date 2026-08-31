namespace Abril_Backend.Features.Contractors.ContractorRegistration.Application.Dtos
{
    /// <summary>
    /// Estado de un RUC frente al registro de contratistas. Se usa tanto para el endpoint
    /// público `ruc-exists` como para decidir, en el servidor, la rama del flujo de
    /// solicitud de actualización de datos.
    /// </summary>
    public class ContractorRucStatusDto
    {
        /// <summary>Indica si ya existe un contributor vigente con ese RUC.</summary>
        public bool Exists { get; set; }
        public int? ContributorId { get; set; }
        public string? ContributorName { get; set; }
        /// <summary>Contratista activo (state=true) en estado 1/2/4; null si solo hay rechazados o ninguno.</summary>
        public int? ActiveContractorId { get; set; }
        public int? ActiveContractorStateId { get; set; }
        /// <summary>N° de contratistas históricos del contributor (para nombrar subcarpetas).</summary>
        public int ContractorCount { get; set; }
        /// <summary>N° de solicitudes de actualización históricas del contratista activo.</summary>
        public int UpdateRequestCount { get; set; }
    }
}
