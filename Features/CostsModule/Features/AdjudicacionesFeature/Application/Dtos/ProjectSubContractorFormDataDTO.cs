using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos {
    public class ProjectSubContractorFormDataDTO {
        public List<ProjectSimpleDTO> Projects {get;set;}
        public List<ContractTypeSimpleDTO> ContractTypes {get;set;}
        public List<ContractModalitySimpleDTO> ContractModalities {get;set;}
        public List<PaymentMethodSimpleDTO> PaymentMethods {get;set;}
        public List<PaymentFormSimpleDTO> PaymentForms {get;set;}
        public List<CurrencySimpleDTO> Currencies {get;set;}
        public List<WorkItemSimpleDTO> WorkItems {get;set;}
        public List<WorkItemCategorySimpleDTO> WorkItemCategories { get;set; }
        public List<WorkSpecialtySimpleDTO> WorkSpecialties { get;set; } = new();
        public List<ProjectSubContractorStatusSimpleDTO> ProjectSubContractorStatuses { get;set; } = new();
        public List<ContributorFactoryDTO> Contributors {get;set;}
    }
}
