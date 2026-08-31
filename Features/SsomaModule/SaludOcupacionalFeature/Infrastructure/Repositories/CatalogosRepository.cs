using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Repositories
{
    public class CatalogosRepository : ICatalogosRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CatalogosRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        // ===== Clinicas =====
        public async Task<List<ClinicaDto>> ListClinicas(bool soloActivos)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.SsClinica.AsQueryable();
            if (soloActivos) q = q.Where(c => c.Activo);
            return await q
                .OrderBy(c => c.Nombre)
                .Select(c => new ClinicaDto
                {
                    Id = c.Id,
                    Nombre = c.Nombre,
                    Ruc = c.Ruc,
                    Direccion = c.Direccion,
                    Telefono = c.Telefono,
                    Email = c.Email,
                    Activo = c.Activo
                })
                .ToListAsync();
        }

        public async Task<ClinicaDto> GetClinicaById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var c = await ctx.SsClinica.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new AbrilException("Clínica no encontrada.", 404);
            return new ClinicaDto
            {
                Id = c.Id,
                Nombre = c.Nombre,
                Ruc = c.Ruc,
                Direccion = c.Direccion,
                Telefono = c.Telefono,
                Email = c.Email,
                Activo = c.Activo
            };
        }

        public async Task<ClinicaDto> CreateClinica(ClinicaUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = new SsClinica
            {
                Nombre = dto.Nombre,
                Ruc = dto.Ruc,
                Direccion = dto.Direccion,
                Telefono = dto.Telefono,
                Email = dto.Email,
                Activo = dto.Activo,
                CreatedAt = DateTimeOffset.UtcNow
            };
            ctx.SsClinica.Add(ent);
            await ctx.SaveChangesAsync();
            return new ClinicaDto
            {
                Id = ent.Id,
                Nombre = ent.Nombre,
                Ruc = ent.Ruc,
                Direccion = ent.Direccion,
                Telefono = ent.Telefono,
                Email = ent.Email,
                Activo = ent.Activo
            };
        }

        public async Task<ClinicaDto> UpdateClinica(int id, ClinicaUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsClinica.FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new AbrilException("Clínica no encontrada.", 404);
            ent.Nombre = dto.Nombre;
            ent.Ruc = dto.Ruc;
            ent.Direccion = dto.Direccion;
            ent.Telefono = dto.Telefono;
            ent.Email = dto.Email;
            ent.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new ClinicaDto
            {
                Id = ent.Id,
                Nombre = ent.Nombre,
                Ruc = ent.Ruc,
                Direccion = ent.Direccion,
                Telefono = ent.Telefono,
                Email = ent.Email,
                Activo = ent.Activo
            };
        }

        // ===== Medicos =====
        public async Task<List<MedicoOcupacionalDto>> ListMedicos(bool soloActivos)
        {
            using var ctx = _factory.CreateDbContext();
            var q =
                from m in ctx.SsMedicoOcupacional
                join c in ctx.SsClinica on m.ClinicaId equals c.Id into cl
                from c in cl.DefaultIfEmpty()
                select new { m, c };

            if (soloActivos) q = q.Where(x => x.m.Activo);

            return await q
                .OrderBy(x => x.m.ApellidoNombre)
                .Select(x => new MedicoOcupacionalDto
                {
                    Id = x.m.Id,
                    ApellidoNombre = x.m.ApellidoNombre,
                    Dni = x.m.Dni,
                    Cmp = x.m.Cmp,
                    Especialidad = x.m.Especialidad,
                    ClinicaId = x.m.ClinicaId,
                    ClinicaNombre = x.c != null ? x.c.Nombre : null,
                    Email = x.m.Email,
                    Celular = x.m.Celular,
                    Activo = x.m.Activo
                })
                .ToListAsync();
        }

        public async Task<MedicoOcupacionalDto> CreateMedico(MedicoOcupacionalUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = new SsMedicoOcupacional
            {
                ApellidoNombre = dto.ApellidoNombre,
                Dni = dto.Dni,
                Cmp = dto.Cmp,
                Especialidad = dto.Especialidad,
                ClinicaId = dto.ClinicaId,
                Email = dto.Email,
                Celular = dto.Celular,
                Activo = dto.Activo,
                CreatedAt = DateTimeOffset.UtcNow
            };
            ctx.SsMedicoOcupacional.Add(ent);
            await ctx.SaveChangesAsync();
            return await GetMedicoById(ent.Id);
        }

        public async Task<MedicoOcupacionalDto> UpdateMedico(int id, MedicoOcupacionalUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsMedicoOcupacional.FirstOrDefaultAsync(m => m.Id == id)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            ent.ApellidoNombre = dto.ApellidoNombre;
            ent.Dni = dto.Dni;
            ent.Cmp = dto.Cmp;
            ent.Especialidad = dto.Especialidad;
            ent.ClinicaId = dto.ClinicaId;
            ent.Email = dto.Email;
            ent.Celular = dto.Celular;
            ent.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return await GetMedicoById(id);
        }

        private async Task<MedicoOcupacionalDto> GetMedicoById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            return await (
                from m in ctx.SsMedicoOcupacional
                join c in ctx.SsClinica on m.ClinicaId equals c.Id into cl
                from c in cl.DefaultIfEmpty()
                where m.Id == id
                select new MedicoOcupacionalDto
                {
                    Id = m.Id,
                    ApellidoNombre = m.ApellidoNombre,
                    Dni = m.Dni,
                    Cmp = m.Cmp,
                    Especialidad = m.Especialidad,
                    ClinicaId = m.ClinicaId,
                    ClinicaNombre = c != null ? c.Nombre : null,
                    Email = m.Email,
                    Celular = m.Celular,
                    Activo = m.Activo
                }).FirstAsync();
        }

        public async Task<string?> GetMedicoEmailAsync(int medicoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsMedicoOcupacional
                .Where(m => m.Id == medicoId)
                .Select(m => m.Email)
                .FirstOrDefaultAsync();
        }

        public async Task SetPinFirmaAsync(int medicoId, string pinHash)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsMedicoOcupacional.FirstOrDefaultAsync(m => m.Id == medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            ent.PinFirmaHash = pinHash;
            ent.PinFirmaIntentosFallidos = 0;
            ent.PinFirmaBloqueadoHasta = null;
            await ctx.SaveChangesAsync();
        }

        public async Task<AutorizacionFirmaDetalleDto?> GetAutorizacionFirmaDetalleAsync(int medicoId)
        {
            using var ctx = _factory.CreateDbContext();
            var row = await ctx.SsMedicoOcupacional
                .Where(m => m.Id == medicoId)
                .Select(m => new AutorizacionFirmaDetalleDto
                {
                    MedicoId = m.Id,
                    MedicoNombre = m.ApellidoNombre,
                    MedicoDni = m.Dni,
                    MedicoCmp = m.Cmp,
                    MedicoEspecialidad = m.Especialidad,
                    FirmaDigitalUrl = m.FirmaDigitalUrl,
                    Email = m.Email
                })
                .FirstOrDefaultAsync();

            if (row != null)
                row.MedicoDni = await ResolverDniPorEmailAsync(ctx, row.MedicoDni, row.Email);

            return row;
        }

        /// <summary>
        /// Si el médico no tiene DNI cargado en su propio catálogo, lo busca en
        /// workers/person por el mismo correo — evita duplicar el dato a mano cuando el
        /// médico ya es un trabajador/persona registrado en el sistema.
        /// </summary>
        private static async Task<string?> ResolverDniPorEmailAsync(AppDbContext ctx, string? dniActual, string? email)
        {
            if (!string.IsNullOrWhiteSpace(dniActual)) return dniActual;
            if (string.IsNullOrWhiteSpace(email)) return dniActual;

            var correo = email.Trim();

            var porWorker = await ctx.Worker
                .Where(w => w.EmailCorporativo != null && w.EmailCorporativo.ToLower() == correo.ToLower())
                .Select(w => w.Person != null ? w.Person.DocumentIdentityCode : null)
                .FirstOrDefaultAsync();
            if (!string.IsNullOrWhiteSpace(porWorker)) return porWorker;

            var porPerson = await ctx.Person
                .Where(p => p.Email != null && p.Email.ToLower() == correo.ToLower())
                .Select(p => p.DocumentIdentityCode)
                .FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(porPerson) ? porPerson : dniActual;
        }

        public async Task<MedicoFirmaArchivosDto?> GetMedicoFirmaArchivosAsync(int medicoId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsMedicoOcupacional
                .Where(m => m.Id == medicoId)
                .Select(m => new MedicoFirmaArchivosDto
                {
                    Email = m.Email,
                    FirmaDigitalUrl = m.FirmaDigitalUrl,
                    UrlAutorizacionFirmada = m.UrlAutorizacionFirmada
                })
                .FirstOrDefaultAsync();
        }

        public async Task SetFirmaDigitalAsync(int medicoId, string url)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsMedicoOcupacional.FirstOrDefaultAsync(m => m.Id == medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            ent.FirmaDigitalUrl = url;
            await ctx.SaveChangesAsync();
        }

        public async Task SetAutorizacionFirmadaAsync(int medicoId, string url)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsMedicoOcupacional.FirstOrDefaultAsync(m => m.Id == medicoId)
                ?? throw new AbrilException("Médico no encontrado.", 404);
            ent.UrlAutorizacionFirmada = url;
            ent.FechaAutorizacionFirma = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();
        }

        // ===== EMO Tipos =====
        public async Task<List<EmoTipoDto>> ListEmoTipos(bool soloActivos)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.SsEmoTipo.AsQueryable();
            if (soloActivos) q = q.Where(t => t.Activo);
            return await q
                .OrderBy(t => t.Nombre)
                .Select(t => new EmoTipoDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    VigenciaMeses = t.VigenciaMeses,
                    RequiereNuevo = t.RequiereNuevo,
                    Descripcion = t.Descripcion,
                    Activo = t.Activo
                })
                .ToListAsync();
        }

        public async Task<EmoTipoDto> CreateEmoTipo(EmoTipoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = new SsEmoTipo
            {
                Nombre = dto.Nombre,
                VigenciaMeses = dto.VigenciaMeses,
                RequiereNuevo = dto.RequiereNuevo,
                Descripcion = dto.Descripcion,
                Activo = dto.Activo
            };
            ctx.SsEmoTipo.Add(ent);
            await ctx.SaveChangesAsync();
            return new EmoTipoDto
            {
                Id = ent.Id,
                Nombre = ent.Nombre,
                VigenciaMeses = ent.VigenciaMeses,
                RequiereNuevo = ent.RequiereNuevo,
                Descripcion = ent.Descripcion,
                Activo = ent.Activo
            };
        }

        public async Task<EmoTipoDto> UpdateEmoTipo(int id, EmoTipoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsEmoTipo.FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new AbrilException("Tipo de EMO no encontrado.", 404);
            ent.Nombre = dto.Nombre;
            ent.VigenciaMeses = dto.VigenciaMeses;
            ent.RequiereNuevo = dto.RequiereNuevo;
            ent.Descripcion = dto.Descripcion;
            ent.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new EmoTipoDto
            {
                Id = ent.Id,
                Nombre = ent.Nombre,
                VigenciaMeses = ent.VigenciaMeses,
                RequiereNuevo = ent.RequiereNuevo,
                Descripcion = ent.Descripcion,
                Activo = ent.Activo
            };
        }

        // ===== Examen Tipos =====
        public async Task<List<ExamenTipoDto>> ListExamenTipos(bool soloActivos)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.SsExamenTipo.AsQueryable();
            if (soloActivos) q = q.Where(t => t.Activo);
            return await q
                .OrderBy(t => t.Nombre)
                .Select(t => new ExamenTipoDto
                {
                    Id = t.Id,
                    Nombre = t.Nombre,
                    Codigo = t.Codigo,
                    Categoria = t.Categoria,
                    Activo = t.Activo
                })
                .ToListAsync();
        }

        public async Task<ExamenTipoDto> CreateExamenTipo(ExamenTipoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = new SsExamenTipo
            {
                Nombre = dto.Nombre,
                Codigo = dto.Codigo,
                Categoria = dto.Categoria,
                Activo = dto.Activo
            };
            ctx.SsExamenTipo.Add(ent);
            await ctx.SaveChangesAsync();
            return new ExamenTipoDto
            {
                Id = ent.Id,
                Nombre = ent.Nombre,
                Codigo = ent.Codigo,
                Categoria = ent.Categoria,
                Activo = ent.Activo
            };
        }

        public async Task<ExamenTipoDto> UpdateExamenTipo(int id, ExamenTipoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsExamenTipo.FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new AbrilException("Tipo de examen no encontrado.", 404);
            ent.Nombre = dto.Nombre;
            ent.Codigo = dto.Codigo;
            ent.Categoria = dto.Categoria;
            ent.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new ExamenTipoDto
            {
                Id = ent.Id,
                Nombre = ent.Nombre,
                Codigo = ent.Codigo,
                Categoria = ent.Categoria,
                Activo = ent.Activo
            };
        }

        // ===== Restriccion Tipos =====
        public async Task<List<RestriccionTipoDto>> ListRestriccionTipos(bool soloActivos)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.SsRestriccionTipo.AsQueryable();
            if (soloActivos) q = q.Where(t => t.Activo);
            return await q
                .OrderBy(t => t.Descripcion)
                .Select(t => new RestriccionTipoDto
                {
                    Id = t.Id,
                    Descripcion = t.Descripcion,
                    Categoria = t.Categoria,
                    Activo = t.Activo
                })
                .ToListAsync();
        }

        public async Task<RestriccionTipoDto> CreateRestriccionTipo(RestriccionTipoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = new SsRestriccionTipo
            {
                Descripcion = dto.Descripcion,
                Categoria = dto.Categoria,
                Activo = dto.Activo
            };
            ctx.SsRestriccionTipo.Add(ent);
            await ctx.SaveChangesAsync();
            return new RestriccionTipoDto
            {
                Id = ent.Id,
                Descripcion = ent.Descripcion,
                Categoria = ent.Categoria,
                Activo = ent.Activo
            };
        }

        public async Task<RestriccionTipoDto> UpdateRestriccionTipo(int id, RestriccionTipoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsRestriccionTipo.FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new AbrilException("Tipo de restricción no encontrado.", 404);
            ent.Descripcion = dto.Descripcion;
            ent.Categoria = dto.Categoria;
            ent.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new RestriccionTipoDto
            {
                Id = ent.Id,
                Descripcion = ent.Descripcion,
                Categoria = ent.Categoria,
                Activo = ent.Activo
            };
        }

        // ===== Clinica Emails =====
        public async Task<List<ClinicaEmailDto>> ListClinicaEmails(int clinicaId)
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.SsClinicaEmail
                .AsNoTracking()
                .Where(e => e.ClinicaId == clinicaId)
                .OrderBy(e => e.Nombre)
                .ThenBy(e => e.Email)
                .Select(e => new ClinicaEmailDto
                {
                    Id = e.Id,
                    Email = e.Email,
                    Nombre = e.Nombre,
                    Activo = e.Activo
                })
                .ToListAsync();
        }

        public async Task<ClinicaEmailDto> CreateClinicaEmail(int clinicaId, ClinicaEmailCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var clinicaExiste = await ctx.SsClinica.AnyAsync(c => c.Id == clinicaId);
            if (!clinicaExiste)
                throw new AbrilException("Clínica no encontrada.", 404);
            var ent = new SsClinicaEmail
            {
                ClinicaId = clinicaId,
                Email = dto.Email.Trim(),
                Nombre = dto.Nombre?.Trim(),
                Activo = true
            };
            ctx.SsClinicaEmail.Add(ent);
            await ctx.SaveChangesAsync();
            return new ClinicaEmailDto
            {
                Id = ent.Id,
                Email = ent.Email,
                Nombre = ent.Nombre,
                Activo = ent.Activo
            };
        }

        public async Task DeleteClinicaEmail(int clinicaId, int emailId)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsClinicaEmail
                .FirstOrDefaultAsync(e => e.Id == emailId && e.ClinicaId == clinicaId)
                ?? throw new AbrilException("Email de clínica no encontrado.", 404);
            ctx.SsClinicaEmail.Remove(ent);
            await ctx.SaveChangesAsync();
        }

        // ===== Agente de Riesgo =====
        public async Task<List<AgenteRiesgoDto>> ListAgentesRiesgo(bool soloActivos)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.SsAgenteRiesgo.AsQueryable();
            if (soloActivos) q = q.Where(a => a.Activo);
            return await q
                .OrderBy(a => a.Tipo).ThenBy(a => a.Nombre)
                .Select(a => new AgenteRiesgoDto
                {
                    Id = a.Id,
                    Nombre = a.Nombre,
                    Tipo = a.Tipo,
                    Activo = a.Activo
                })
                .ToListAsync();
        }

        public async Task<AgenteRiesgoDto> CreateAgenteRiesgo(AgenteRiesgoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = new SsAgenteRiesgo
            {
                Nombre = dto.Nombre,
                Tipo = dto.Tipo,
                Activo = dto.Activo
            };
            ctx.SsAgenteRiesgo.Add(ent);
            await ctx.SaveChangesAsync();
            return new AgenteRiesgoDto { Id = ent.Id, Nombre = ent.Nombre, Tipo = ent.Tipo, Activo = ent.Activo };
        }

        public async Task<AgenteRiesgoDto> UpdateAgenteRiesgo(int id, AgenteRiesgoUpsertDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var ent = await ctx.SsAgenteRiesgo.FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new AbrilException("Agente de riesgo no encontrado.", 404);
            ent.Nombre = dto.Nombre;
            ent.Tipo = dto.Tipo;
            ent.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new AgenteRiesgoDto { Id = ent.Id, Nombre = ent.Nombre, Tipo = ent.Tipo, Activo = ent.Activo };
        }

        // ===== Empresas (razones sociales) =====
        // Lee desde la tabla `contributor`. Mapea los campos al shape de EmpresaCatalogoDto
        // que es el que consume tanto SSOMA como Configuración → Razones Sociales.
        public async Task<List<EmpresaCatalogoDto>> ListEmpresas(bool soloActivas)
        {
            using var ctx = _factory.CreateDbContext();
            var q = ctx.Contributor.Where(e => e.State).AsQueryable();
            if (soloActivas) q = q.Where(e => e.Active == true);
            return await q
                .OrderBy(e => e.ContributorName)
                .Select(e => new EmpresaCatalogoDto
                {
                    Id               = e.ContributorId,
                    Nombre           = e.ContributorName,
                    Ruc              = e.ContributorRuc,
                    Direccion        = e.ContributorAddress,
                    PartidaRegistral = e.LegalEntityRegistryNumber,
                    TipoActividad    = e.ContributorEconomicActivityDescription ?? "",
                    Activo           = e.Active,
                    EsAbril          = e.EsAbril
                })
                .ToListAsync();
        }

        public async Task<EmpresaCatalogoDto> CreateEmpresa(EmpresaCreateDto dto, int? userId)
        {
            using var ctx = _factory.CreateDbContext();

            var ruc = dto.Ruc.Trim();
            var exists = await ctx.Contributor.AnyAsync(c => c.ContributorRuc == ruc && c.State);
            if (exists)
                throw new AbrilException("Ya existe una razón social registrada con ese RUC.", 409);

            var entity = new Abril_Backend.Features.CostsModule.Shared.Models.Contributor
            {
                ContributorRuc = ruc,
                ContributorName = dto.Nombre.Trim(),
                ContributorAddress = dto.Direccion.Trim(),
                ContributorEconomicActivityDescription = dto.TipoActividad.Trim(),
                ContributorDistrict = dto.Distrito.Trim(),
                ContributorProvince = dto.Provincia.Trim(),
                ContributorDepartment = dto.Departamento.Trim(),
                LegalEntityRegistryNumber = string.IsNullOrWhiteSpace(dto.PartidaRegistral) ? null : dto.PartidaRegistral.Trim(),
                CreatedDateTime = DateTimeOffset.UtcNow,
                CreatedUserId = userId,
                Active = true,
                State = true,
                EsAbril = false
            };
            ctx.Contributor.Add(entity);
            await ctx.SaveChangesAsync();

            return new EmpresaCatalogoDto
            {
                Id = entity.ContributorId,
                Nombre = entity.ContributorName,
                Ruc = entity.ContributorRuc,
                Direccion = entity.ContributorAddress,
                PartidaRegistral = entity.LegalEntityRegistryNumber,
                TipoActividad = entity.ContributorEconomicActivityDescription ?? "",
                Activo = entity.Active,
                EsAbril = entity.EsAbril
            };
        }
    }
}
