using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    public class LbAuthService : ILbAuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IPasswordHasher<UsuarioSistema> _passwordHasher;
        private readonly ILbJwtService _jwtService;

        public LbAuthService(
            IDbContextFactory<AppDbContext> factory,
            IPasswordHasher<UsuarioSistema> passwordHasher,
            ILbJwtService jwtService)
        {
            _factory = factory;
            _passwordHasher = passwordHasher;
            _jwtService = jwtService;
        }

        public async Task<LbLoginResponseDto> Login(LbLoginRequestDto request)
        {
            using var ctx = _factory.CreateDbContext();

            var usuario = await ctx.UsuarioSistema
                .Include(u => u.Persona)
                .FirstOrDefaultAsync(u => u.EmailLogin == request.Email);

            if (usuario is null)
                throw new AbrilException("Credenciales inválidas.", 401);

            if (usuario.Estado != "ACTIVO")
                throw new AbrilException("Este usuario está inactivo. Contacta al administrador.", 403);

            var resultado = _passwordHasher.VerifyHashedPassword(usuario, usuario.PasswordHash, request.Password);
            if (resultado == PasswordVerificationResult.Failed)
                throw new AbrilException("Credenciales inválidas.", 401);

            // Resolución de asignaciones vigentes + permisos — misma query de
            // CONTEXT_LOGISTICA.md sección 4.4.
            var asignaciones = await ctx.UsuarioAsignacion
                .Where(ua => ua.UsuarioSistemaId == usuario.Id && ua.FechaFin == null)
                .Include(ua => ua.Rol)
                .ToListAsync();

            var rolIds = asignaciones.Select(a => a.RolId).Distinct().ToList();
            var permisos = await ctx.RolPermiso
                .Where(rp => rolIds.Contains(rp.RolId))
                .Include(rp => rp.Permiso)
                .Select(rp => rp.Permiso!.Codigo)
                .Distinct()
                .ToListAsync();

            usuario.UltimoAcceso = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            var response = new LbLoginResponseDto
            {
                UsuarioSistemaId = usuario.Id,
                Email = usuario.EmailLogin,
                NombreCompleto = $"{usuario.Persona!.Nombres} {usuario.Persona.Apellidos}",
                Asignaciones = asignaciones.Select(a => new LbAsignacionDto
                {
                    RolCodigo = a.Rol!.Codigo,
                    EsGlobal = a.Rol.EsGlobal,
                    ProyectoId = a.ProyectoId,
                    AlmacenId = a.AlmacenId,
                }).ToList(),
                Permisos = permisos,
            };

            response.Token = _jwtService.GenerateToken(response);
            return response;
        }

        public async Task<LbLoginResponseDto> SeedAdmin(LbLoginRequestDto request)
        {
            using var ctx = _factory.CreateDbContext();

            if (await ctx.UsuarioSistema.AnyAsync())
                throw new AbrilException("Ya existe al menos un usuario — el bootstrap solo corre en una base vacía.", 409);

            var rolAdmin = await ctx.Rol.FirstOrDefaultAsync(r => r.Codigo == "GERENTE_GENERAL")
                ?? throw new AbrilException("No existe el rol GERENTE_GENERAL — corre el DDL de la sección 4 primero.", 500);

            var tipoPlanilla = await ctx.TipoVinculo.FirstOrDefaultAsync(t => t.Codigo == "PLANILLA")
                ?? throw new AbrilException("No existe el tipo de vínculo PLANILLA.", 500);

            var persona = new Persona
            {
                Nombres = "Administrador",
                Apellidos = "Inicial",
                TipoDocumento = "DNI",
                NumeroDocumento = "00000000",
                CreadoEn = DateTimeOffset.UtcNow,
                ActualizadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Persona.Add(persona);
            await ctx.SaveChangesAsync();

            ctx.VinculoLaboral.Add(new VinculoLaboral
            {
                PersonaId = persona.Id,
                TipoVinculoId = tipoPlanilla.Id,
                FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            });

            var usuario = new UsuarioSistema
            {
                PersonaId = persona.Id,
                EmailLogin = request.Email,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            };
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, request.Password);
            ctx.UsuarioSistema.Add(usuario);
            await ctx.SaveChangesAsync();

            ctx.UsuarioAsignacion.Add(new UsuarioAsignacion
            {
                UsuarioSistemaId = usuario.Id,
                RolId = rolAdmin.Id,
                FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return await Login(request);
        }
    }
}
