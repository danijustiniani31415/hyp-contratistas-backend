using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GuiasRemisionModule.Application.Dtos;
using Abril_Backend.Features.GuiasRemisionModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models;
using Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Sunat;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.GuiasRemisionModule.Application.Services
{
    public class GuiaMaestrosService : IGuiaMaestrosService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly SunatGreSettings _settings;

        public GuiaMaestrosService(IDbContextFactory<AppDbContext> factory, IOptions<SunatGreSettings> settings)
        {
            _factory = factory;
            _settings = settings.Value;
        }

        // ── Transportistas ──
        public async Task<List<TransportistaDto>> ListarTransportistas()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.GuiaTransportista.AsNoTracking().OrderBy(t => t.RazonSocial)
                .Select(t => new TransportistaDto { Id = t.Id, Ruc = t.Ruc, RazonSocial = t.RazonSocial, NroMtc = t.NroMtc, Activo = t.Activo })
                .ToListAsync();
        }

        public async Task<TransportistaDto> GuardarTransportista(long? id, TransportistaDto dto)
        {
            var ruc = dto.Ruc?.Trim();
            var razon = dto.RazonSocial?.Trim();
            if (!GuiaValidaciones.RucValido(ruc))
                throw new AbrilException("El RUC del transportista no es válido (11 dígitos con dígito verificador correcto).", 400);
            if (ruc == _settings.Ruc)
                throw new AbrilException("En transporte público el transportista es otra empresa; si llevas tú la mercadería, usa transporte privado con tu vehículo.", 400);
            if (string.IsNullOrWhiteSpace(razon))
                throw new AbrilException("La razón social del transportista es obligatoria.", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.GuiaTransportista.AnyAsync(t => t.Ruc == ruc && t.Id != (id ?? 0)))
                throw new AbrilException("Ya existe un transportista con ese RUC.", 409);

            GuiaTransportista t;
            if (id.HasValue)
                t = await ctx.GuiaTransportista.FirstOrDefaultAsync(x => x.Id == id.Value)
                    ?? throw new AbrilException("Transportista no encontrado.", 404);
            else
            {
                t = new GuiaTransportista();
                ctx.GuiaTransportista.Add(t);
            }
            t.Ruc = ruc!;
            t.RazonSocial = razon!;
            t.NroMtc = string.IsNullOrWhiteSpace(dto.NroMtc) ? null : dto.NroMtc.Trim();
            t.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new TransportistaDto { Id = t.Id, Ruc = t.Ruc, RazonSocial = t.RazonSocial, NroMtc = t.NroMtc, Activo = t.Activo };
        }

        // ── Vehículos ──
        public async Task<List<VehiculoDto>> ListarVehiculos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.GuiaVehiculo.AsNoTracking().OrderBy(v => v.Placa)
                .Select(v => new VehiculoDto { Id = v.Id, Placa = v.Placa, NroCirculacion = v.NroCirculacion, Activo = v.Activo })
                .ToListAsync();
        }

        public async Task<VehiculoDto> GuardarVehiculo(long? id, VehiculoDto dto)
        {
            var placa = GuiaValidaciones.NormalizarPlaca(dto.Placa);
            if (!GuiaValidaciones.PlacaValida(placa))
                throw new AbrilException("La placa debe tener 6 o 7 letras/números, sin guiones (ej. AKY719).", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.GuiaVehiculo.AnyAsync(v => v.Placa == placa && v.Id != (id ?? 0)))
                throw new AbrilException("Ya existe un vehículo con esa placa.", 409);

            GuiaVehiculo v;
            if (id.HasValue)
                v = await ctx.GuiaVehiculo.FirstOrDefaultAsync(x => x.Id == id.Value)
                    ?? throw new AbrilException("Vehículo no encontrado.", 404);
            else
            {
                v = new GuiaVehiculo();
                ctx.GuiaVehiculo.Add(v);
            }
            v.Placa = placa!;
            v.NroCirculacion = string.IsNullOrWhiteSpace(dto.NroCirculacion) ? null : dto.NroCirculacion.Trim();
            v.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new VehiculoDto { Id = v.Id, Placa = v.Placa, NroCirculacion = v.NroCirculacion, Activo = v.Activo };
        }

        // ── Conductores ──
        public async Task<List<ConductorDto>> ListarConductores()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.GuiaConductor.AsNoTracking().OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres)
                .Select(c => new ConductorDto { Id = c.Id, NumDoc = c.NumDoc, Nombres = c.Nombres, Apellidos = c.Apellidos, Licencia = c.Licencia, PersonaId = c.PersonaId, Activo = c.Activo })
                .ToListAsync();
        }

        public async Task<ConductorDto> GuardarConductor(long? id, ConductorDto dto)
        {
            var dni = dto.NumDoc?.Trim();
            var nombres = dto.Nombres?.Trim().ToUpperInvariant();
            var apellidos = dto.Apellidos?.Trim().ToUpperInvariant();
            var licencia = GuiaValidaciones.NormalizarLicencia(dto.Licencia);

            if (!GuiaValidaciones.DniValido(dni))
                throw new AbrilException("El DNI debe tener 8 dígitos.", 400);
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
                throw new AbrilException("Apellidos y nombres del conductor son obligatorios (por separado).", 400);
            if (!GuiaValidaciones.LicenciaValida(licencia))
                throw new AbrilException("La licencia de conducir debe tener entre 8 y 12 letras/números (ej. P41713102).", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.GuiaConductor.AnyAsync(c => c.NumDoc == dni && c.Id != (id ?? 0)))
                throw new AbrilException("Ya existe un conductor con ese DNI.", 409);

            GuiaConductor c;
            if (id.HasValue)
                c = await ctx.GuiaConductor.FirstOrDefaultAsync(x => x.Id == id.Value)
                    ?? throw new AbrilException("Conductor no encontrado.", 404);
            else
            {
                c = new GuiaConductor();
                ctx.GuiaConductor.Add(c);
            }
            c.TipoDoc = "1";
            c.NumDoc = dni!;
            c.Nombres = nombres!;
            c.Apellidos = apellidos!;
            c.Licencia = licencia!;
            // El vínculo con Personas se decide por el DNI, no por lo que mande el cliente: si ese DNI es de un
            // trabajador registrado queda enlazado; si no, es un conductor externo.
            c.PersonaId = await ctx.Persona.AsNoTracking()
                .Where(p => p.TipoDocumento == "DNI" && p.NumeroDocumento == dni)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync();
            c.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new ConductorDto { Id = c.Id, NumDoc = c.NumDoc, Nombres = c.Nombres, Apellidos = c.Apellidos, Licencia = c.Licencia, PersonaId = c.PersonaId, Activo = c.Activo };
        }
    }
}
