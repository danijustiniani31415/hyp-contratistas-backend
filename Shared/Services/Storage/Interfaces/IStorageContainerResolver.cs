namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IStorageContainerResolver
    {
        string GetLessonsContainerName();
        string GetIvtContainerName();
        string GetConstructionSiteLogbookContainerName();
        string GetResidentIncidentContainerName();
        string GetProjectSubContractorContainerName();
        string GetProjectFotosContainerName();
        string GetProjectCroquisContainerName();
        string GetVecinoRequisitosContainerName();
        string GetVecinoEntregablesContainerName();
        string GetVecinoPropiedadImagenesContainerName();
        string GetInvoicesContainerName();
        string GetActasReunionContainerName();
        string GetTareosContainerName();
    }
}