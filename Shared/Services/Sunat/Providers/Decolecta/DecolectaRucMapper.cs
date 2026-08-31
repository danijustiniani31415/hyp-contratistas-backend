using Abril_Backend.Shared.Services.Sunat.Dtos;

namespace Abril_Backend.Shared.Services.Sunat.Providers.Decolecta
{
    internal static class DecolectaRucMapper
    {
        internal static SunatContributorDto ToSunatContributorDto(DecolectaRucResponse response) => new()
        {
            ContributorRuc = response.NumeroDocumento,
            ContributorName = response.RazonSocial,
            ContributorAddress = response.Direccion,
            ContributorEconomicActivityDescription = response.ActividadEconomica,
            ContributorDistrict   = response.Distrito,
            ContributorProvince   = response.Provincia,
            ContributorDepartment = response.Departamento
        };
    }
}
