using System.Security.Cryptography;
using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.PersonasModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule.Application.Interfaces;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;
using Abril_Backend.Infrastructure.Data;
using Abril_Backend.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Models;
using Abril_Backend.Shared.Services.Reniec.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Abril_Backend.Features.PersonasModule.Application.Services
{
    public class PersonaService : IPersonaService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;
        private readonly IPasswordHasher<UsuarioSistema> _passwordHasher;
        private readonly IEmailService _emailService;
        private readonly FrontendSettings _frontendSettings;
        private readonly IReniecService _reniecService;

        public PersonaService(
            IDbContextFactory<AppDbContext> factory,
            IPasswordHasher<UsuarioSistema> passwordHasher,
            IEmailService emailService,
            IOptions<FrontendSettings> frontendSettings,
            IReniecService reniecService)
        {
            _factory = factory;
            _passwordHasher = passwordHasher;
            _emailService = emailService;
            _frontendSettings = frontendSettings.Value;
            _reniecService = reniecService;
        }

        /// <summary>
        /// Autocompletar Nombres/Apellidos al crear una persona — mismo servicio de RENIEC
        /// (Decolecta, con rotación de tokens) que ya usa toda la Plataforma Abril, ver
        /// Controllers/PersonController.cs para el precedente. Null si el DNI no existe.
        /// </summary>
        public async Task<ReniecPersonaDto?> BuscarPorDni(string dni)
        {
            if (string.IsNullOrWhiteSpace(dni) || dni.Length != 8 || !dni.All(char.IsDigit))
                throw new AbrilException("El DNI debe tener 8 dígitos.", 400);

            var persona = await _reniecService.GetByDniAsync(dni);
            if (persona is null) return null;

            return new ReniecPersonaDto
            {
                Nombres = persona.FirstName,
                Apellidos = $"{persona.FirstLastName} {persona.SecondLastName}".Trim(),
            };
        }

        /// <summary>
        /// Qué personas con vínculo vigente todavía no tienen lo mínimo para entrar a una planilla
        /// calculada: categoría laboral (obrero/empleado), sueldo o jornal según corresponda, banco,
        /// y el tareo del mes actual registrado. No exige TODOS los campos de PersonaPlanilla —
        /// CUSP/tipo AFP-ONP quedan como "se pueden llenar después" (pedido explícito del usuario).
        /// </summary>
        public async Task<DashboardPlanillaDto> GetDashboardPlanilla()
        {
            using var ctx = _factory.CreateDbContext();
            var hoy = DateTimeOffset.UtcNow;
            var desdeMes = new DateOnly(hoy.Year, hoy.Month, 1);
            var hastaMes = new DateOnly(hoy.Year, hoy.Month, DateTime.DaysInMonth(hoy.Year, hoy.Month));

            var personas = await ctx.Persona
                .Where(p => p.Activo && p.Vinculos.Any(v => v.FechaFin == null))
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Apellidos + " " + p.Nombres,
                    CargoNombre = p.Vinculos.Where(v => v.FechaFin == null)
                        .Select(v => v.Cargo != null ? v.Cargo.Nombre : null).FirstOrDefault(),
                    ProyectoNombre = p.Vinculos.Where(v => v.FechaFin == null)
                        .Select(v => v.Proyecto != null ? v.Proyecto.Nombre : null).FirstOrDefault(),
                    Planilla = ctx.PersonaPlanilla.Where(pl => pl.PersonaId == p.Id).FirstOrDefault(),
                })
                .OrderBy(p => p.NombreCompleto)
                .ToListAsync();

            var personaIdsConTareoEsteMes = (await ctx.Tareo
                .Where(t => t.Fecha >= desdeMes && t.Fecha <= hastaMes)
                .Select(t => t.PersonaId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            var faltantes = new List<PersonaDatoFaltanteDto>();
            foreach (var p in personas)
            {
                var campos = new List<string>();
                var categoria = p.Planilla?.CategoriaLaboral;

                if (string.IsNullOrWhiteSpace(categoria)) campos.Add("Categoría laboral");
                if (string.IsNullOrWhiteSpace(p.Planilla?.Banco)) campos.Add("Banco");

                // Obrero cobra por jornal, empleado por sueldo mensual — solo se exige el que
                // corresponda según la categoría (si todavía no tiene categoría, no se puede saber
                // cuál pedir, así que no se duplica el reclamo).
                if (categoria == "OBRERO" && p.Planilla?.Jornal is null) campos.Add("Jornal");
                if (categoria == "EMPLEADO" && p.Planilla?.SueldoBase is null) campos.Add("Sueldo base");

                if (!personaIdsConTareoEsteMes.Contains(p.Id)) campos.Add("Tareo del mes actual");

                if (campos.Count > 0)
                {
                    faltantes.Add(new PersonaDatoFaltanteDto
                    {
                        PersonaId = p.Id,
                        NombreCompleto = p.NombreCompleto,
                        CargoNombre = p.CargoNombre,
                        ProyectoNombre = p.ProyectoNombre,
                        CamposFaltantes = campos,
                    });
                }
            }

            return new DashboardPlanillaDto
            {
                TotalPersonasActivas = personas.Count,
                TotalConDatosFaltantes = faltantes.Count,
                TotalConDatosCompletos = personas.Count - faltantes.Count,
                Faltantes = faltantes,
            };
        }

        public async Task<List<CargoDetailDto>> ListCargos()
        {
            using var ctx = _factory.CreateDbContext();
            return await ctx.Cargo
                .OrderBy(c => c.Nombre)
                .Select(c => new CargoDetailDto { Id = c.Id, Nombre = c.Nombre, Activo = c.Activo })
                .ToListAsync();
        }

        public async Task<CargoDetailDto> CrearCargo(CargoCreateDto dto)
        {
            var nombre = dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("El nombre del cargo es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            if (await ctx.Cargo.AnyAsync(c => c.Nombre == nombre))
                throw new AbrilException($"Ya existe un cargo \"{nombre}\".", 409);

            var cargo = new Cargo { Nombre = nombre, Activo = true };
            ctx.Cargo.Add(cargo);
            await ctx.SaveChangesAsync();
            return new CargoDetailDto { Id = cargo.Id, Nombre = cargo.Nombre, Activo = cargo.Activo };
        }

        public async Task<CargoDetailDto> ActualizarCargo(int id, CargoUpdateDto dto)
        {
            var nombre = dto.Nombre.Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                throw new AbrilException("El nombre del cargo es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var cargo = await ctx.Cargo.FindAsync(id)
                ?? throw new AbrilException("Cargo no encontrado.", 404);

            if (nombre != cargo.Nombre && await ctx.Cargo.AnyAsync(c => c.Nombre == nombre && c.Id != id))
                throw new AbrilException($"Ya existe un cargo \"{nombre}\".", 409);

            cargo.Nombre = nombre;
            cargo.Activo = dto.Activo;
            await ctx.SaveChangesAsync();
            return new CargoDetailDto { Id = cargo.Id, Nombre = cargo.Nombre, Activo = cargo.Activo };
        }

        public async Task<PersonaListResponseDto> List(string? search, int? cargoId, short? tipoVinculoId, string? estado, int page, int pageSize)
        {
            using var ctx = _factory.CreateDbContext();

            var query = ctx.Persona.Where(p => p.Activo).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.Nombres.ToLower().Contains(s) ||
                    p.Apellidos.ToLower().Contains(s) ||
                    p.NumeroDocumento.Contains(s));
            }

            // Los 3 filtros de abajo se evalúan contra el vínculo ACTUAL de la persona (vigente,
            // o el más reciente si está cesada) — mismo criterio que se usa para mostrarlo en la
            // tabla, así el filtro "Cargo: Bodeguero" no incluye a alguien que fue bodeguero hace
            // 2 vínculos y hoy ya es otra cosa.
            if (cargoId.HasValue)
                query = query.Where(p => p.Vinculos
                    .OrderByDescending(v => v.FechaFin == null).ThenByDescending(v => v.FechaInicio)
                    .Select(v => (int?)v.CargoId).FirstOrDefault() == cargoId);

            if (tipoVinculoId.HasValue)
                query = query.Where(p => p.Vinculos
                    .OrderByDescending(v => v.FechaFin == null).ThenByDescending(v => v.FechaInicio)
                    .Select(v => (short?)v.TipoVinculoId).FirstOrDefault() == tipoVinculoId);

            if (!string.IsNullOrWhiteSpace(estado))
            {
                if (estado == "SIN_VINCULO")
                    query = query.Where(p => !p.Vinculos.Any());
                else
                    query = query.Where(p => p.Vinculos
                        .OrderByDescending(v => v.FechaFin == null).ThenByDescending(v => v.FechaInicio)
                        .Select(v => (string?)v.Estado).FirstOrDefault() == estado);
            }

            var total = await query.CountAsync();

            var personas = await query
                .OrderBy(p => p.Apellidos).ThenBy(p => p.Nombres)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new
                {
                    p.Id,
                    NombreCompleto = p.Apellidos + " " + p.Nombres,
                    p.NumeroDocumento,
                    // Vigente primero; si no tiene (CESADO), cae al vínculo más reciente por
                    // fecha_inicio — antes esto quedaba en blanco/"SIN_VINCULO" para cualquier
                    // persona cesada aunque sí tuviera historial, cargo y estado reales.
                    Vinculo = p.Vinculos
                        .OrderByDescending(v => v.FechaFin == null)
                        .ThenByDescending(v => v.FechaInicio)
                        .Select(v => new { v.Estado, v.TipoVinculo!.Nombre, Cargo = v.Cargo!.Nombre })
                        .FirstOrDefault(),
                    TieneUsuario = ctx.UsuarioSistema.Any(u => u.PersonaId == p.Id),
                })
                .ToListAsync();

            return new PersonaListResponseDto
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                TotalPages = (int)Math.Ceiling(total / (double)pageSize),
                Data = personas.Select(p => new PersonaListItemDto
                {
                    Id = p.Id,
                    NombreCompleto = p.NombreCompleto,
                    NumeroDocumento = p.NumeroDocumento,
                    TipoVinculoNombre = p.Vinculo?.Nombre,
                    CargoNombre = p.Vinculo?.Cargo,
                    EstadoVinculo = p.Vinculo?.Estado ?? "SIN_VINCULO",
                    TieneUsuario = p.TieneUsuario,
                }).ToList(),
            };
        }

        public async Task<PersonaDetailDto> GetById(int id)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(id)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> Create(PersonaCreateDto dto)
        {
            using var ctx = _factory.CreateDbContext();

            var yaExiste = await ctx.Persona.AnyAsync(p =>
                p.TipoDocumento == dto.TipoDocumento && p.NumeroDocumento == dto.NumeroDocumento);
            if (yaExiste)
                throw new AbrilException($"Ya existe una persona con {dto.TipoDocumento} {dto.NumeroDocumento}.", 409);

            var tipoVinculo = await ctx.TipoVinculo.FindAsync(dto.TipoVinculoId)
                ?? throw new AbrilException("Tipo de vínculo no encontrado.", 404);
            // Empresa contratista solo aplica al vínculo CONTRATISTA — Planilla/Locador/Practicante
            // son siempre H&P directo. Se limpia en vez de rechazar: el front ya no debería
            // mandarlo, pero si llega igual no hay razón para romper el alta por eso.
            var empresaContratistaId = tipoVinculo.Codigo == "CONTRATISTA" ? dto.EmpresaContratistaId : null;

            var persona = new Persona
            {
                Nombres = dto.Nombres,
                Apellidos = dto.Apellidos,
                TipoDocumento = dto.TipoDocumento,
                NumeroDocumento = dto.NumeroDocumento,
                Telefono = dto.Telefono,
                EmailPersonal = dto.EmailPersonal,
                CreadoEn = DateTimeOffset.UtcNow,
                ActualizadoEn = DateTimeOffset.UtcNow,
            };
            ctx.Persona.Add(persona);
            await ctx.SaveChangesAsync();

            ctx.VinculoLaboral.Add(new VinculoLaboral
            {
                PersonaId = persona.Id,
                TipoVinculoId = dto.TipoVinculoId,
                EmpresaContratistaId = empresaContratistaId,
                CargoId = dto.CargoId,
                ProyectoId = dto.ProyectoId,
                FechaInicio = dto.FechaInicio,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            });

            // Correlativo autogenerado (H&P-E001, H&P-O001...) — mismo patrón que PED-2026-000001 en
            // Pedidos: secuencia de Postgres, no MAX+1 (evita colisiones entre altas concurrentes).
            // EMPLEADO y OBRERO son series independientes en la empresa real (el Excel real trae
            // H&P-E01..E13 y H&P-O01..O51 por separado) — cada categoría tiene su propia secuencia.
            // Sin categoría todavía (se puede completar después): se asume EMPLEADO por ahora; si
            // luego se marca como OBRERO, el código ya asignado no se regenera (es inmutable).
            var p = dto.Planilla;
            var (secuencia, prefijo) = p?.CategoriaLaboral == "OBRERO"
                ? ("lb_trabajador_correlativo_obrero", "H&P-O")
                : ("lb_trabajador_correlativo", "H&P-E");
            var correlativo = await ctx.Database
                .SqlQuery<long>($"""SELECT nextval({secuencia}) AS "Value" """)
                .SingleAsync();
            var codigoTrabajador = $"{prefijo}{correlativo:D3}";

            ctx.PersonaPlanilla.Add(new PersonaPlanilla
            {
                PersonaId = persona.Id,
                CodigoTrabajador = codigoTrabajador,
                Banco = p?.Banco,
                NumeroCuenta = p?.NumeroCuenta,
                Cusp = p?.Cusp,
                TipoAfpOnp = p?.TipoAfpOnp,
                CategoriaLaboral = p?.CategoriaLaboral,
                SueldoBase = p?.SueldoBase,
                Jornal = p?.Jornal,
                AsignacionFamiliar = p?.AsignacionFamiliar ?? false,
                Sctr = p?.Sctr ?? false,
                ActualizadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        /// <summary>
        /// Edita los datos básicos de una persona ya creada — nombres, documento, teléfono y
        /// correo. El vínculo laboral (cargo/proyecto/tipo) NO se toca acá, tiene su propio flujo
        /// (NuevoVinculo) porque un cambio de cargo es un evento con fecha, no una corrección.
        /// </summary>
        public async Task<PersonaDetailDto> ActualizarDatos(int personaId, PersonaUpdateDto dto)
        {
            var nombres = dto.Nombres?.Trim() ?? "";
            var apellidos = dto.Apellidos?.Trim() ?? "";
            var numeroDocumento = dto.NumeroDocumento?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
                throw new AbrilException("Nombres y apellidos son obligatorios.", 400);
            if (string.IsNullOrWhiteSpace(numeroDocumento))
                throw new AbrilException("El número de documento es obligatorio.", 400);

            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var documentoCambio = dto.TipoDocumento != persona.TipoDocumento || numeroDocumento != persona.NumeroDocumento;
            if (documentoCambio && await ctx.Persona.AnyAsync(p =>
                    p.Id != personaId && p.TipoDocumento == dto.TipoDocumento && p.NumeroDocumento == numeroDocumento))
                throw new AbrilException($"Ya existe otra persona con {dto.TipoDocumento} {numeroDocumento}.", 409);

            persona.Nombres = nombres;
            persona.Apellidos = apellidos;
            persona.TipoDocumento = dto.TipoDocumento;
            persona.NumeroDocumento = numeroDocumento;
            persona.Telefono = string.IsNullOrWhiteSpace(dto.Telefono) ? null : dto.Telefono.Trim();
            persona.EmailPersonal = string.IsNullOrWhiteSpace(dto.EmailPersonal) ? null : dto.EmailPersonal.Trim();
            persona.ActualizadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            // [DECIDIDO 2026-09-24] correo personal y correo de acceso son un solo correo para el
            // usuario final — si la persona ya tiene acceso ACTIVO, editar su correo personal acá
            // también actualiza su login, con el mismo aviso de seguridad al correo anterior que
            // usa CambiarEmail. Evita que ambos campos queden desalineados sin que nadie lo note.
            await SincronizarEmailLogin(ctx, persona, persona.EmailPersonal);

            return await BuildDetail(ctx, persona);
        }

        private async Task SincronizarEmailLogin(AppDbContext ctx, Persona persona, string? nuevoEmailPersonal)
        {
            if (string.IsNullOrWhiteSpace(nuevoEmailPersonal)) return;

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == persona.Id);
            if (usuario is null) return;

            var nuevoEmail = nuevoEmailPersonal.Trim().ToLower();
            if (nuevoEmail == usuario.EmailLogin) return;

            var emailEnUso = await ctx.UsuarioSistema.AnyAsync(u => u.EmailLogin == nuevoEmail && u.Id != usuario.Id);
            if (emailEnUso)
                throw new AbrilException(
                    $"No se pudo actualizar el correo de acceso: '{nuevoEmail}' ya está en uso por otro usuario.", 409);

            var emailAnterior = usuario.EmailLogin;
            usuario.EmailLogin = nuevoEmail;
            await ctx.SaveChangesAsync();

            // Si todavía no activó su cuenta, nadie llegó a loguearse con el correo viejo — no hay
            // a quién avisar, y avisar ahí solo generaría ruido/confusión.
            if (usuario.Estado != "ACTIVO") return;

            var html = $@"<h2>Tu correo de acceso cambió</h2>
<p>Hola {persona.Nombres}, el correo con el que ingresas a la plataforma de HP Constructores / Las Bravas cambió de <strong>{emailAnterior}</strong> a <strong>{nuevoEmail}</strong> (se actualizó junto con tu correo personal).</p>
<p>Si tú o un administrador autorizado hicieron este cambio, no necesitas hacer nada.</p>
<p>Si no reconoces este cambio, contacta de inmediato al administrador del sistema.</p>";

            await _emailService.SendAsync(
                to: new List<string> { emailAnterior },
                subject: "Tu correo de acceso cambió - HP Constructores Generales",
                body: html,
                isHtml: true);
        }

        public async Task<PersonaDetailDto> ActualizarPlanilla(int personaId, PersonaPlanillaDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var planilla = await ctx.PersonaPlanilla.FindAsync(personaId);
            if (planilla is null)
            {
                // No debería pasar (Create ya inserta la fila con su correlativo), pero por si
                // esta persona es de antes de que existiera Fase 1: le da un código recién acá.
                // Misma distinción de series que en Create — EMPLEADO y OBRERO son independientes.
                var (secuencia, prefijo) = dto.CategoriaLaboral == "OBRERO"
                    ? ("lb_trabajador_correlativo_obrero", "H&P-O")
                    : ("lb_trabajador_correlativo", "H&P-E");
                var correlativo = await ctx.Database
                    .SqlQuery<long>($"""SELECT nextval({secuencia}) AS "Value" """)
                    .SingleAsync();
                planilla = new PersonaPlanilla { PersonaId = personaId, CodigoTrabajador = $"{prefijo}{correlativo:D3}" };
                ctx.PersonaPlanilla.Add(planilla);
            }

            // CodigoTrabajador NO se toca acá — es correlativo autogenerado, nunca editable.
            planilla.Banco = dto.Banco;
            planilla.NumeroCuenta = dto.NumeroCuenta;
            planilla.Cusp = dto.Cusp;
            planilla.TipoAfpOnp = dto.TipoAfpOnp;
            planilla.CategoriaLaboral = dto.CategoriaLaboral;
            planilla.SueldoBase = dto.SueldoBase;
            planilla.Jornal = dto.Jornal;
            planilla.AsignacionFamiliar = dto.AsignacionFamiliar;
            planilla.Sctr = dto.Sctr;
            planilla.ActualizadoEn = DateTimeOffset.UtcNow;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> NuevoVinculo(int personaId, NuevoVinculoDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var tipoVinculo = await ctx.TipoVinculo.FindAsync(dto.TipoVinculoId)
                ?? throw new AbrilException("Tipo de vínculo no encontrado.", 404);
            var empresaContratistaId = tipoVinculo.Codigo == "CONTRATISTA" ? dto.EmpresaContratistaId : null;

            using var tx = await ctx.Database.BeginTransactionAsync();

            // [DECIDIDO en CONTEXT_LOGISTICA.md] máximo un vínculo vigente por persona — cerrar
            // el anterior en la misma transacción antes de abrir el nuevo.
            var vigente = await ctx.VinculoLaboral
                .FirstOrDefaultAsync(v => v.PersonaId == personaId && v.FechaFin == null);
            if (vigente != null)
            {
                vigente.FechaFin = dto.FechaInicio.AddDays(-1) >= vigente.FechaInicio
                    ? dto.FechaInicio.AddDays(-1)
                    : dto.FechaInicio;
                vigente.Estado = "CESADO";
                vigente.MotivoCese = dto.MotivoCeseAnterior ?? "Reemplazado por nuevo vínculo";
                await ctx.SaveChangesAsync();
            }

            ctx.VinculoLaboral.Add(new VinculoLaboral
            {
                PersonaId = personaId,
                TipoVinculoId = dto.TipoVinculoId,
                EmpresaContratistaId = empresaContratistaId,
                CargoId = dto.CargoId,
                ProyectoId = dto.ProyectoId,
                FechaInicio = dto.FechaInicio,
                Estado = "ACTIVO",
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();
            await tx.CommitAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> CrearUsuario(int personaId, CrearUsuarioDto dto, long? otorgadoPor)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var email = dto.EmailLogin.Trim().ToLower();

            // [REVISADO en CONTEXT_LOGISTICA.md] 1 persona = máximo 1 usuario_sistema — si ya
            // tiene uno (aunque esté INACTIVO), se reactiva en vez de crear uno nuevo.
            var usuarioExistente = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId);
            UsuarioSistema usuario;
            if (usuarioExistente != null)
            {
                if (usuarioExistente.Estado == "ACTIVO")
                    throw new AbrilException("Esta persona ya tiene un usuario de sistema activo.", 409);

                // Estado se pasa a ACTIVO recién al final, si el correo de activación sale bien —
                // si se deja en ACTIVO acá y el envío falla (ej. caída del proveedor de correo), la
                // persona queda con usuario "activo" pero sin enlace utilizable, y el 409 de arriba
                // bloquea cualquier reintento.
                usuarioExistente.EmailLogin = email;
                usuario = usuarioExistente;
            }
            else
            {
                var emailEnUso = await ctx.UsuarioSistema.AnyAsync(u => u.EmailLogin == email);
                if (emailEnUso)
                    throw new AbrilException("Ese correo ya está en uso por otro usuario.", 409);

                usuario = new UsuarioSistema
                {
                    PersonaId = personaId,
                    EmailLogin = email,
                    Estado = "INACTIVO",
                    CreadoEn = DateTimeOffset.UtcNow,
                };
                ctx.UsuarioSistema.Add(usuario);
            }

            // Contraseña interna aleatoria e inutilizable — nadie la conoce nunca. La persona
            // la define ella misma con el enlace de activación de abajo (mismo mecanismo que
            // "olvidé mi contraseña"). Así el admin jamás maneja ni ve una contraseña ajena.
            usuario.PasswordHash = _passwordHasher.HashPassword(usuario, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            await ctx.SaveChangesAsync();

            var tokensPrevios = await ctx.LbUsuarioPasswordToken
                .Where(t => t.UsuarioSistemaId == usuario.Id && !t.Usado)
                .ToListAsync();
            foreach (var t in tokensPrevios) t.Usado = true;
            if (tokensPrevios.Count > 0) await ctx.SaveChangesAsync();

            var token = GenerarToken();
            ctx.LbUsuarioPasswordToken.Add(new LbUsuarioPasswordToken
            {
                UsuarioSistemaId = usuario.Id,
                Token = token,
                ExpiraEn = DateTime.UtcNow.AddHours(48),
                Usado = false,
                CreadoEn = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            var link = $"{_frontendSettings.LbSetPasswordUrl}?token={token}";
            var html = $@"<h2>Bienvenido a HP Constructores Generales</h2>
<p>Hola {persona.Nombres}, se creó tu acceso al sistema.</p>
<p>Haz clic en el siguiente enlace para crear tu contraseña y empezar a usarlo:</p>
<a href='{link}' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Activar mi cuenta</a>
<p>Este enlace expira en 48 horas.</p>";

            await _emailService.SendAsync(
                to: new List<string> { email },
                subject: "Activa tu cuenta - HP Constructores Generales",
                body: html,
                isHtml: true);

            // Recién acá se confirma el acceso — si SendAsync lanza, el usuario queda INACTIVO
            // y el próximo intento cae en la rama de reactivación de arriba en vez del 409.
            usuario.Estado = "ACTIVO";
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> CambiarEmail(int personaId, CambiarEmailDto dto)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona no tiene usuario de sistema.", 404);

            var nuevoEmail = dto.NuevoEmail.Trim().ToLower();
            if (nuevoEmail == usuario.EmailLogin)
                throw new AbrilException("Ese ya es el correo actual.", 400);

            var emailEnUso = await ctx.UsuarioSistema.AnyAsync(u => u.EmailLogin == nuevoEmail && u.Id != usuario.Id);
            if (emailEnUso)
                throw new AbrilException("Ese correo ya está en uso por otro usuario.", 409);

            var emailAnterior = usuario.EmailLogin;
            usuario.EmailLogin = nuevoEmail;
            // Un solo correo por persona (ver SincronizarEmailLogin) — el sync también corre en
            // este sentido, si no el correo personal se queda desalineado sin que nadie lo note.
            persona.EmailPersonal = nuevoEmail;
            await ctx.SaveChangesAsync();

            var html = $@"<h2>Tu correo de acceso cambió</h2>
<p>Hola {persona.Nombres}, el correo con el que ingresas a la plataforma de HP Constructores / Las Bravas cambió de <strong>{emailAnterior}</strong> a <strong>{nuevoEmail}</strong>.</p>
<p>Si tú o un administrador autorizado hicieron este cambio, no necesitas hacer nada.</p>
<p>Si no reconoces este cambio, contacta de inmediato al administrador del sistema.</p>";

            await _emailService.SendAsync(
                to: new List<string> { emailAnterior },
                subject: "Tu correo de acceso cambió - HP Constructores Generales",
                body: html,
                isHtml: true);

            return await BuildDetail(ctx, persona);
        }

        /// <summary>
        /// Botón único "Reenviar credenciales" en la ficha de la persona — no le pide al admin
        /// distinguir si la persona nunca activó su cuenta o ya la activó y perdió el acceso; en
        /// ambos casos genera un link nuevo al correo de acceso actual (mismo mecanismo de token
        /// que la invitación inicial y que "olvidé mi contraseña").
        /// </summary>
        public async Task<PersonaDetailDto> ReenviarCredenciales(int personaId)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona no tiene usuario de sistema.", 404);

            var tokensPrevios = await ctx.LbUsuarioPasswordToken
                .Where(t => t.UsuarioSistemaId == usuario.Id && !t.Usado)
                .ToListAsync();
            foreach (var t in tokensPrevios) t.Usado = true;
            if (tokensPrevios.Count > 0) await ctx.SaveChangesAsync();

            var yaActivo = usuario.Estado == "ACTIVO";
            var token = GenerarToken();
            ctx.LbUsuarioPasswordToken.Add(new LbUsuarioPasswordToken
            {
                UsuarioSistemaId = usuario.Id,
                Token = token,
                ExpiraEn = DateTime.UtcNow.AddHours(yaActivo ? 2 : 48),
                Usado = false,
                CreadoEn = DateTime.UtcNow,
            });
            await ctx.SaveChangesAsync();

            var link = $"{_frontendSettings.LbSetPasswordUrl}?token={token}";
            var html = yaActivo
                ? $@"<h2>Restablece tu contraseña</h2>
<p>Hola {persona.Nombres}, un administrador solicitó un enlace para restablecer tu contraseña en HP Constructores / Las Bravas.</p>
<p>Haz clic en el siguiente enlace para crear una nueva contraseña:</p>
<a href='{link}' style='background:#1E3A5F;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Restablecer contraseña</a>
<p>Este enlace expira en 2 horas.</p>"
                : $@"<h2>Bienvenido a HP Constructores Generales</h2>
<p>Hola {persona.Nombres}, se creó tu acceso al sistema.</p>
<p>Haz clic en el siguiente enlace para crear tu contraseña y empezar a usarlo:</p>
<a href='{link}' style='background:#0F172A;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;margin:16px 0'>Activar mi cuenta</a>
<p>Este enlace expira en 48 horas.</p>";

            await _emailService.SendAsync(
                to: new List<string> { usuario.EmailLogin },
                subject: yaActivo ? "Restablece tu contraseña - HP Constructores Generales" : "Activa tu cuenta - HP Constructores Generales",
                body: html,
                isHtml: true);

            if (!yaActivo)
            {
                usuario.Estado = "ACTIVO";
                await ctx.SaveChangesAsync();
            }

            return await BuildDetail(ctx, persona);
        }

        /// <summary>Token opaco de un solo uso — mismo formato que LbAuthService.GenerarToken.</summary>
        private static string GenerarToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        public async Task<PersonaDetailDto> NuevaAsignacion(int personaId, NuevaAsignacionDto dto, long? otorgadoPor)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona todavía no tiene usuario de sistema — créalo primero.", 400);

            var rol = await ctx.Rol.FindAsync(dto.RolId)
                ?? throw new AbrilException("Rol no encontrado.", 404);

            if (dto.ProyectoId != null && !await ctx.Proyecto.AnyAsync(p => p.Id == dto.ProyectoId))
                throw new AbrilException("Proyecto no encontrado.", 404);

            // El scope (global vs. un solo proyecto) lo decide quién otorga el acceso, no el rol:
            // el mismo rol (ej. LOGISTICA) puede darse global a alguien en Lima y acotado a Las
            // Bravas a otra persona. dto.ProyectoId == null = global.
            ctx.UsuarioAsignacion.Add(new UsuarioAsignacion
            {
                UsuarioSistemaId = usuario.Id,
                RolId = dto.RolId,
                ProyectoId = dto.ProyectoId,
                AlmacenId = dto.AlmacenId,
                Notificar = dto.Notificar,
                FechaInicio = DateOnly.FromDateTime(DateTime.UtcNow),
                OtorgadoPorUsuarioSistemaId = otorgadoPor,
                CreadoEn = DateTimeOffset.UtcNow,
            });
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> RevocarAsignacion(int personaId, long asignacionId)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona no tiene usuario de sistema.", 404);

            var asignacion = await ctx.UsuarioAsignacion
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.UsuarioSistemaId == usuario.Id && a.FechaFin == null)
                ?? throw new AbrilException("Asignación no encontrada o ya revocada.", 404);

            asignacion.FechaFin = DateOnly.FromDateTime(DateTime.UtcNow);
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<PersonaDetailDto> ToggleNotificarAsignacion(int personaId, long asignacionId, bool notificar)
        {
            using var ctx = _factory.CreateDbContext();
            var persona = await ctx.Persona.FindAsync(personaId)
                ?? throw new AbrilException("Persona no encontrada.", 404);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == personaId)
                ?? throw new AbrilException("Esta persona no tiene usuario de sistema.", 404);

            var asignacion = await ctx.UsuarioAsignacion
                .FirstOrDefaultAsync(a => a.Id == asignacionId && a.UsuarioSistemaId == usuario.Id && a.FechaFin == null)
                ?? throw new AbrilException("Asignación no encontrada o ya revocada.", 404);

            asignacion.Notificar = notificar;
            await ctx.SaveChangesAsync();

            return await BuildDetail(ctx, persona);
        }

        public async Task<CatalogosPersonasDto> GetCatalogos()
        {
            using var ctx = _factory.CreateDbContext();
            return new CatalogosPersonasDto
            {
                TiposVinculo = await ctx.TipoVinculo.Where(t => t.Activo)
                    .Select(t => new TipoVinculoCatalogoItemDto { Id = t.Id, Nombre = t.Nombre, Codigo = t.Codigo }).ToListAsync(),
                Cargos = await ctx.Cargo.Where(c => c.Activo)
                    .Select(c => new CatalogoItemDto { Id = c.Id, Nombre = c.Nombre }).ToListAsync(),
                EmpresasContratistas = await ctx.EmpresaContratista.Where(e => e.Activo)
                    .Select(e => new CatalogoItemDto { Id = e.Id, Nombre = e.RazonSocial }).ToListAsync(),
                Roles = await ctx.Rol.Where(r => r.Activo)
                    .Select(r => new RolCatalogoItemDto { Id = r.Id, Nombre = r.Nombre, EsGlobal = r.EsGlobal }).ToListAsync(),
                Proyectos = await ctx.Proyecto
                    .Select(p => new CatalogoItemDto { Id = p.Id, Nombre = p.Nombre }).ToListAsync(),
                Almacenes = await ctx.Almacen.Where(a => a.Activo)
                    .Select(a => new AlmacenCatalogoItemDto { Id = a.Id, Nombre = a.Nombre, ProyectoId = a.ProyectoId }).ToListAsync(),
            };
        }

        // ── Interno ──────────────────────────────────────────────────────
        private static async Task<PersonaDetailDto> BuildDetail(AppDbContext ctx, Persona persona)
        {
            var vinculos = await ctx.VinculoLaboral
                .Where(v => v.PersonaId == persona.Id)
                .Include(v => v.TipoVinculo)
                .Include(v => v.EmpresaContratista)
                .Include(v => v.Cargo)
                .Include(v => v.Proyecto)
                .OrderByDescending(v => v.FechaInicio)
                .Select(v => new VinculoLaboralDto
                {
                    Id = v.Id,
                    TipoVinculoNombre = v.TipoVinculo!.Nombre,
                    EmpresaContratistaNombre = v.EmpresaContratista != null ? v.EmpresaContratista.RazonSocial : null,
                    CargoNombre = v.Cargo != null ? v.Cargo.Nombre : null,
                    ProyectoNombre = v.Proyecto != null ? v.Proyecto.Nombre : null,
                    FechaInicio = v.FechaInicio,
                    FechaFin = v.FechaFin,
                    Estado = v.Estado,
                    MotivoCese = v.MotivoCese,
                })
                .ToListAsync();

            var planilla = await ctx.PersonaPlanilla.FirstOrDefaultAsync(p => p.PersonaId == persona.Id);

            var usuario = await ctx.UsuarioSistema.FirstOrDefaultAsync(u => u.PersonaId == persona.Id);

            var asignaciones = new List<AsignacionDetalleDto>();
            if (usuario != null)
            {
                asignaciones = await ctx.UsuarioAsignacion
                    .Where(a => a.UsuarioSistemaId == usuario.Id && a.FechaFin == null)
                    .Include(a => a.Rol)
                    .Include(a => a.Proyecto)
                    .Include(a => a.Almacen)
                    .OrderByDescending(a => a.FechaInicio)
                    .Select(a => new AsignacionDetalleDto
                    {
                        Id = a.Id,
                        RolNombre = a.Rol!.Nombre,
                        EsGlobal = a.Rol.EsGlobal,
                        ProyectoNombre = a.Proyecto != null ? a.Proyecto.Nombre : null,
                        AlmacenNombre = a.Almacen != null ? a.Almacen.Nombre : null,
                        FechaInicio = a.FechaInicio,
                        Notificar = a.Notificar,
                    })
                    .ToListAsync();
            }

            return new PersonaDetailDto
            {
                Id = persona.Id,
                Nombres = persona.Nombres,
                Apellidos = persona.Apellidos,
                TipoDocumento = persona.TipoDocumento,
                NumeroDocumento = persona.NumeroDocumento,
                Telefono = persona.Telefono,
                EmailPersonal = persona.EmailPersonal,
                Activo = persona.Activo,
                Planilla = planilla is null ? null : new PersonaPlanillaDto
                {
                    CodigoTrabajador = planilla.CodigoTrabajador,
                    Banco = planilla.Banco,
                    NumeroCuenta = planilla.NumeroCuenta,
                    Cusp = planilla.Cusp,
                    TipoAfpOnp = planilla.TipoAfpOnp,
                    CategoriaLaboral = planilla.CategoriaLaboral,
                    SueldoBase = planilla.SueldoBase,
                    Jornal = planilla.Jornal,
                    AsignacionFamiliar = planilla.AsignacionFamiliar,
                    Sctr = planilla.Sctr,
                },
                Vinculos = vinculos,
                UsuarioSistemaId = usuario?.Id,
                EmailLogin = usuario?.EmailLogin,
                EstadoUsuario = usuario?.Estado,
                Asignaciones = asignaciones,
            };
        }
    }
}
