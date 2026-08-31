using Abril_Backend.Shared.Services.Sunat.Dtos;

namespace Abril_Backend.Shared.Services.Sunat.Interfaces
{
    public interface ISunatService
    {
        Task<SunatContributorDto?> GetByRucAsync(string ruc);
    }
}
