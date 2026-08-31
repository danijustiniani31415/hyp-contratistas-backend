namespace Abril_Backend.Features.Contractors.ContractorManagement.Application.Dtos
{
    public class ContractorUserItemDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
    }
}
