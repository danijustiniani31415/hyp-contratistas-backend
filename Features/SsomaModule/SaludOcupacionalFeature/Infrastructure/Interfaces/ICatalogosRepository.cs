using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces
{
    public interface ICatalogosRepository
    {
        // Clinicas
        Task<List<ClinicaDto>> ListClinicas(bool soloActivos);
        Task<ClinicaDto> GetClinicaById(int id);
        Task<ClinicaDto> CreateClinica(ClinicaUpsertDto dto);
        Task<ClinicaDto> UpdateClinica(int id, ClinicaUpsertDto dto);

        // Medicos
        Task<List<MedicoOcupacionalDto>> ListMedicos(bool soloActivos);
        Task<MedicoOcupacionalDto> CreateMedico(MedicoOcupacionalUpsertDto dto);
        Task<MedicoOcupacionalDto> UpdateMedico(int id, MedicoOcupacionalUpsertDto dto);
        Task<AutorizacionFirmaDetalleDto?> GetAutorizacionFirmaDetalleAsync(int medicoId);
        Task<string?> GetMedicoEmailAsync(int medicoId);
        Task SetPinFirmaAsync(int medicoId, string pinHash);
        Task<MedicoFirmaArchivosDto?> GetMedicoFirmaArchivosAsync(int medicoId);
        Task SetFirmaDigitalAsync(int medicoId, string url);
        Task SetAutorizacionFirmadaAsync(int medicoId, string url);

        // EMO Tipos
        Task<List<EmoTipoDto>> ListEmoTipos(bool soloActivos);
        Task<EmoTipoDto> CreateEmoTipo(EmoTipoUpsertDto dto);
        Task<EmoTipoDto> UpdateEmoTipo(int id, EmoTipoUpsertDto dto);

        // Examen Tipos
        Task<List<ExamenTipoDto>> ListExamenTipos(bool soloActivos);
        Task<ExamenTipoDto> CreateExamenTipo(ExamenTipoUpsertDto dto);
        Task<ExamenTipoDto> UpdateExamenTipo(int id, ExamenTipoUpsertDto dto);

        // Restriccion Tipos
        Task<List<RestriccionTipoDto>> ListRestriccionTipos(bool soloActivos);
        Task<RestriccionTipoDto> CreateRestriccionTipo(RestriccionTipoUpsertDto dto);
        Task<RestriccionTipoDto> UpdateRestriccionTipo(int id, RestriccionTipoUpsertDto dto);

        // Agente de Riesgo
        Task<List<AgenteRiesgoDto>> ListAgentesRiesgo(bool soloActivos);
        Task<AgenteRiesgoDto> CreateAgenteRiesgo(AgenteRiesgoUpsertDto dto);
        Task<AgenteRiesgoDto> UpdateAgenteRiesgo(int id, AgenteRiesgoUpsertDto dto);

        // Empresas
        Task<List<EmpresaCatalogoDto>> ListEmpresas(bool soloActivas);
        Task<EmpresaCatalogoDto> CreateEmpresa(EmpresaCreateDto dto, int? userId);

        // Clinica Emails
        Task<List<ClinicaEmailDto>> ListClinicaEmails(int clinicaId);
        Task<ClinicaEmailDto> CreateClinicaEmail(int clinicaId, ClinicaEmailCreateDto dto);
        Task DeleteClinicaEmail(int clinicaId, int emailId);
    }
}
