using Abril_Backend.Shared.Services.Reniec.Dtos;
namespace Abril_Backend.Shared.Services.Reniec.Interfaces
{
    public interface IReniecService
    {
        Task<ReniecPersonDto?> GetByDniAsync(string dni);
    }
}