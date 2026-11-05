using System.Text.RegularExpressions;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.DocumentosModule.Application.Dtos;
using Abril_Backend.Features.DocumentosModule.Application.Interfaces;
using Abril_Backend.Features.GuiasRemisionModule.Application.Services;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Shared.Services.Reniec.Interfaces;
using Abril_Backend.Shared.Services.Sunat.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.DocumentosModule.Application.Services
{
    /// <summary>
    /// Reconoce personas por DNI y empresas por RUC para no tener que teclear nombres. Primero mira lo que la
    /// empresa ya tiene registrado (gratis y más confiable) y recién después consulta RENIEC/SUNAT.
    /// </summary>
    public class DocumentoService : IDocumentoService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IReniecService _reniec;
        private readonly ISunatService _sunat;

        public DocumentoService(IDbContextFactory<AppDbContext> factory, IReniecService reniec, ISunatService sunat)
        {
            _factory = factory;
            _reniec = reniec;
            _sunat = sunat;
        }

        public async Task<DocumentoDto?> BuscarDni(string dni)
        {
            dni = dni?.Trim() ?? "";
            if (!Regex.IsMatch(dni, @"^\d{8}$"))
                throw new AbrilException("El DNI debe tener 8 dígitos.", 400);

            using (var ctx = _factory.CreateDbContext())
            {
                var persona = await ctx.Persona.AsNoTracking()
                    .Where(p => p.TipoDocumento == "DNI" && p.NumeroDocumento == dni)
                    .FirstOrDefaultAsync();
                if (persona != null)
                {
                    return new DocumentoDto
                    {
                        Origen = "TRABAJADOR",
                        Numero = dni,
                        Nombres = persona.Nombres,
                        Apellidos = persona.Apellidos,
                        PersonaId = persona.Id,
                        Aviso = persona.Activo ? null : "Esta persona figura inactiva en Personas.",
                    };
                }
            }

            var r = await _reniec.GetByDniAsync(dni);
            if (r is null) return null;
            return new DocumentoDto
            {
                Origen = "RENIEC",
                Numero = dni,
                Nombres = r.FirstName,
                Apellidos = $"{r.FirstLastName} {r.SecondLastName}".Trim(),
            };
        }

        public async Task<DocumentoDto?> BuscarRuc(string ruc)
        {
            ruc = ruc?.Trim() ?? "";
            if (!GuiaValidaciones.RucValido(ruc))
                throw new AbrilException("El RUC no es válido (11 dígitos con dígito verificador correcto).", 400);

            using (var ctx = _factory.CreateDbContext())
            {
                var transportista = await ctx.GuiaTransportista.AsNoTracking().FirstOrDefaultAsync(t => t.Ruc == ruc);
                if (transportista != null)
                    return new DocumentoDto { Origen = "TRANSPORTISTA", Numero = ruc, RazonSocial = transportista.RazonSocial, Aviso = "Ya está registrado como transportista." };

                var proveedor = await ctx.Proveedor.AsNoTracking().FirstOrDefaultAsync(p => p.Ruc == ruc);
                if (proveedor != null)
                    return new DocumentoDto { Origen = "PROVEEDOR", Numero = ruc, RazonSocial = proveedor.RazonSocial, Aviso = "Ya está registrado como proveedor." };
            }

            var s = await _sunat.GetByRucAsync(ruc);
            if (s is null) return null;
            return new DocumentoDto
            {
                Origen = "SUNAT",
                Numero = ruc,
                RazonSocial = s.ContributorName,
                Direccion = s.ContributorAddress,
            };
        }
    }
}
