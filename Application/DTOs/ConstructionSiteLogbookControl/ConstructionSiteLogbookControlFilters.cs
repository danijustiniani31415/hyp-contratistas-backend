namespace Abril_Backend.Application.DTOs
{
    public class ConstructionSiteLogbookControlFiltersDTO
    {
        public List<ProjectSimpleDTO> Projects {get; set;}
        public List<UserFilterDTO> Residents {get; set;}
        public List<DateOnly> Periods {get; set;}
    }
}