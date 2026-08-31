using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Shared.Services.Sunat.Dtos;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces
{
    public interface ICatalogosService
    {
        Task<List<ClinicaDto>> ListClinicas(bool soloActivos);
        Task<ClinicaDto> GetClinicaById(int id);
        Task<ClinicaDto> CreateClinica(ClinicaUpsertDto dto);
        Task<ClinicaDto> UpdateClinica(int id, ClinicaUpsertDto dto);

        Task<List<MedicoOcupacionalDto>> ListMedicos(bool soloActivos);
        Task<MedicoOcupacionalDto> CreateMedico(MedicoOcupacionalUpsertDto dto);
        Task<MedicoOcupacionalDto> UpdateMedico(int id, MedicoOcupacionalUpsertDto dto);
        Task<byte[]> GenerarAutorizacionFirmaPdfAsync(int medicoId);
        Task SetPinFirmaAsync(int medicoId, string pin, string? callerEmail);
        Task<string> SetFirmaDigitalAsync(int medicoId, Stream fileStream, string fileName, string? callerEmail);
        Task<string> SetAutorizacionFirmadaAsync(int medicoId, Stream fileStream, string fileName, string? callerEmail);

        Task<List<EmoTipoDto>> ListEmoTipos(bool soloActivos);
        Task<EmoTipoDto> CreateEmoTipo(EmoTipoUpsertDto dto);
        Task<EmoTipoDto> UpdateEmoTipo(int id, EmoTipoUpsertDto dto);

        Task<List<ExamenTipoDto>> ListExamenTipos(bool soloActivos);
        Task<ExamenTipoDto> CreateExamenTipo(ExamenTipoUpsertDto dto);
        Task<ExamenTipoDto> UpdateExamenTipo(int id, ExamenTipoUpsertDto dto);

        Task<List<RestriccionTipoDto>> ListRestriccionTipos(bool soloActivos);
        Task<RestriccionTipoDto> CreateRestriccionTipo(RestriccionTipoUpsertDto dto);
        Task<RestriccionTipoDto> UpdateRestriccionTipo(int id, RestriccionTipoUpsertDto dto);

        Task<List<AgenteRiesgoDto>> ListAgentesRiesgo(bool soloActivos);
        Task<AgenteRiesgoDto> CreateAgenteRiesgo(AgenteRiesgoUpsertDto dto);
        Task<AgenteRiesgoDto> UpdateAgenteRiesgo(int id, AgenteRiesgoUpsertDto dto);

        Task<List<EmpresaCatalogoDto>> ListEmpresas(bool soloActivas);
        Task<SunatContributorDto?> GetEmpresaByRuc(string ruc);
        Task<EmpresaCatalogoDto> CreateEmpresa(EmpresaCreateDto dto, int? userId);

        // Clinica Emails
        Task<List<ClinicaEmailDto>> ListClinicaEmails(int clinicaId);
        Task<ClinicaEmailDto> CreateClinicaEmail(int clinicaId, ClinicaEmailCreateDto dto);
        Task DeleteClinicaEmail(int clinicaId, int emailId);
    }
}
