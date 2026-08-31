using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos;
using Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Interfaces;
using Abril_Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Repositories
{
    /// <inheritdoc cref="ICartaOfertaFirmaRepository"/>
    public class CartaOfertaFirmaRepository : ICartaOfertaFirmaRepository
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public CartaOfertaFirmaRepository(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        /// <summary>Los timestamps se guardan en UTC y se sirven al frontend en hora de Perú.</summary>
        private static readonly TimeSpan PeruOffset = TimeSpan.FromHours(-5);

        /// <summary>
        /// Mensaje único para "el token no sirve". Es a propósito el mismo para un token inexistente,
        /// uno de un onboarding dado de baja y uno sin carta cargada: desde afuera no se puede
        /// distinguir un token inventado de uno que existió, y así probar tokens no informa nada.
        /// </summary>
        private const string TokenInvalido =
            "El enlace no es válido o ya no está disponible. Escríbele a Gestión de Talento Humano para que te envíe uno nuevo.";

        public async Task<CartaOfertaFirmaPublicoDto> GetPublicoByToken(string token)
        {
            using var ctx = _factory.CreateDbContext();

            var fila = await (
                from ob in ctx.GthOnboarding
                where ob.State && ob.CartaOfertaToken == token
                join c in ctx.GthCandidato on ob.GthCandidatoId equals c.GthCandidatoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                join s in ctx.GthSolicitud on r.GthSolicitudId equals s.GthSolicitudId
                join p in ctx.Puesto on r.PuestoId equals p.PuestoId
                join pr in ctx.Project on r.ProjectId equals pr.ProjectId
                    // Razón social, ficha maestra y jefe directo son todos opcionales en la vacante.
                join co in ctx.Contributor on r.ContributorId equals (int?)co.ContributorId into coJoin
                from co in coJoin.DefaultIfEmpty()
                join pe in ctx.Person on ob.PersonId equals (int?)pe.PersonId into peJoin
                from pe in peJoin.DefaultIfEmpty()
                join w in ctx.Worker on s.SolicitanteWorkerId equals (int?)w.Id into wJoin
                from w in wJoin.DefaultIfEmpty()
                select new
                {
                    ob.CartaOfertaNombre,
                    ob.CartaOfertaUrl,
                    ob.FechaIngreso,
                    ob.CartaFirmadaUrl,
                    ob.CartaFirmadaPostulanteDateTime,
                    ob.CartaFirmadaSubidaDateTime,
                    ob.CartaFirmadaAprobadaDateTime,
                    CandidatoNombre = c.Nombre,
                    PersonNombre    = pe == null ? null : pe.FullName,
                    // La firma vive en la ficha de la base maestra: son las mismas columnas que usa la
                    // firma del Gerente General en Contabilidad.
                    FirmaBytes      = pe == null ? null : pe.SignatureImageBytes,
                    FirmaMime       = pe == null ? null : pe.SignatureMime,
                    FirmaFecha      = pe == null ? null : pe.SignatureUpdatedDateTime,
                    Puesto          = p.Nombre,
                    Area            = s.AreaNombre,
                    Empresa         = co == null ? null : co.ContributorName,
                    ProyectoObra    = pr.ProjectDescription,
                    JefeDirecto     = w == null ? null : (w.Person != null ? w.Person.FullName : w.ApellidoNombre),
                })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException(TokenInvalido, 404);

            // Sin carta cargada no hay nada que mostrar ni firmar: la página no tendría contenido.
            if (string.IsNullOrWhiteSpace(fila.CartaOfertaUrl))
                throw new AbrilException(TokenInvalido, 404);

            return new CartaOfertaFirmaPublicoDto
            {
                Nombre       = string.IsNullOrWhiteSpace(fila.PersonNombre)
                    ? fila.CandidatoNombre : fila.PersonNombre!,
                Puesto       = fila.Puesto,
                Area         = fila.Area,
                Empresa      = fila.Empresa,
                ProyectoObra = fila.ProyectoObra,
                JefeDirecto  = fila.JefeDirecto,
                FechaIngreso = fila.FechaIngreso,
                CartaNombre  = fila.CartaOfertaNombre,

                FirmaDataUrl = fila.FirmaBytes == null
                    ? null
                    : $"data:{fila.FirmaMime ?? "image/png"};base64,{Convert.ToBase64String(fila.FirmaBytes)}",
                FirmaActualizadaEn = fila.FirmaFecha?.ToOffset(PeruOffset).DateTime,

                YaFirmada = !string.IsNullOrWhiteSpace(fila.CartaFirmadaUrl),
                // La fecha de firma del postulante manda; si la carta la subió GTH a mano, se muestra
                // la de esa subida, que es cuando el documento firmado entró al expediente.
                FirmadaEn = (fila.CartaFirmadaPostulanteDateTime ?? fila.CartaFirmadaSubidaDateTime)
                    ?.ToOffset(PeruOffset).DateTime,
                Aprobada  = fila.CartaFirmadaAprobadaDateTime != null,
            };
        }

        public async Task<CartaOfertaFirmaContextoDto> PrepararPorToken(string token)
        {
            using var ctx = _factory.CreateDbContext();

            var fila = await (
                from ob in ctx.GthOnboarding
                where ob.State && ob.CartaOfertaToken == token
                join c in ctx.GthCandidato on ob.GthCandidatoId equals c.GthCandidatoId
                join r in ctx.GthRequerimiento on c.GthRequerimientoId equals r.GthRequerimientoId
                select new
                {
                    Ob = ob,
                    r.Codigo,
                    CandidatoNombre = c.Nombre,
                    // El nombre con el que se armó el file digital: el que declaró el postulante en su
                    // formulario y, si no hay formulario, el que registró GTH. NO el de la base
                    // maestra, que puede haber cambiado después del envío y llevaría el documento
                    // firmado a una carpeta distinta de la que tiene la carta original.
                    FormularioNombre = ctx.GthPostulanteFormulario
                        .Where(f => f.State && f.GthCandidatoId == c.GthCandidatoId)
                        .Select(f => f.NombresCompletos)
                        .FirstOrDefault(),
                    Dni = ctx.Person
                        .Where(x => x.PersonId == ob.PersonId)
                        .Select(x => x.DocumentIdentityCode)
                        .FirstOrDefault()
                        ?? ctx.GthPostulanteFormulario
                            .Where(f => f.State && f.GthCandidatoId == c.GthCandidatoId)
                            .Select(f => f.NumeroDocumento)
                            .FirstOrDefault(),
                })
                .FirstOrDefaultAsync()
                ?? throw new AbrilException(TokenInvalido, 404);

            var onboarding = fila.Ob;

            if (string.IsNullOrWhiteSpace(onboarding.CartaOfertaUrl))
                throw new AbrilException(TokenInvalido, 404);

            // El enlace solo se manda cuando la ficha existe, así que esto no debería pasar; la guarda
            // está para no dejar que un null se convierta en un person_id 0 al firmar.
            if (onboarding.PersonId == null)
                throw new AbrilException(
                    "Tu ficha no está completa en nuestros registros, así que todavía no podemos guardar tu firma. Escríbele a Gestión de Talento Humano.",
                    409);

            return new CartaOfertaFirmaContextoDto
            {
                OnboardingId = onboarding.GthOnboardingId,
                PersonId     = onboarding.PersonId.Value,
                Codigo       = fila.Codigo,
                Nombre       = string.IsNullOrWhiteSpace(fila.FormularioNombre)
                    ? fila.CandidatoNombre : fila.FormularioNombre!,
                Dni                = fila.Dni ?? string.Empty,
                CartaOfertaNombre  = onboarding.CartaOfertaNombre,
                CartaOfertaUrl     = onboarding.CartaOfertaUrl,
                CartaOfertaDriveId = onboarding.CartaOfertaDriveId,
                CartaOfertaItemId  = onboarding.CartaOfertaItemId,
                CartaFirmadaNombre  = onboarding.CartaFirmadaNombre,
                CartaFirmadaUrl     = onboarding.CartaFirmadaUrl,
                CartaFirmadaDriveId = onboarding.CartaFirmadaDriveId,
                CartaFirmadaItemId  = onboarding.CartaFirmadaItemId,
                Carpeta            = string.IsNullOrWhiteSpace(onboarding.FileDigitalDriveId)
                                  || string.IsNullOrWhiteSpace(onboarding.FileDigitalItemId)
                    ? null
                    : new FileDigitalCarpetaDto
                    {
                        DriveId = onboarding.FileDigitalDriveId!,
                        ItemId  = onboarding.FileDigitalItemId!,
                        Ruta    = onboarding.FileDigitalRuta,
                    },
                YaFirmada = !string.IsNullOrWhiteSpace(onboarding.CartaFirmadaUrl),
                Aprobada  = onboarding.CartaFirmadaAprobadaDateTime != null,
            };
        }

        public async Task<CartaOfertaFirmaGuardarResultDto> GuardarFirma(int personId, byte[] imageBytes, string mime)
        {
            using var ctx = _factory.CreateDbContext();

            var person = await ctx.Person.FirstOrDefaultAsync(x => x.PersonId == personId)
                ?? throw new AbrilException(
                    "No encontramos tu ficha en nuestros registros. Escríbele a Gestión de Talento Humano.", 404);

            var now = DateTimeOffset.UtcNow;
            person.SignatureImageBytes      = imageBytes;
            person.SignatureMime            = mime;
            person.SignatureUpdatedDateTime = now;

            await ctx.SaveChangesAsync();

            return new CartaOfertaFirmaGuardarResultDto
            {
                Message            = "Firma registrada.",
                FirmaDataUrl       = $"data:{mime};base64,{Convert.ToBase64String(imageBytes)}",
                FirmaActualizadaEn = now.ToOffset(PeruOffset).DateTime,
            };
        }

        public async Task<(byte[] Bytes, string Mime)?> GetFirmaBytes(int personId)
        {
            using var ctx = _factory.CreateDbContext();

            var p = await ctx.Person
                .Where(x => x.PersonId == personId && x.SignatureImageBytes != null)
                .Select(x => new { x.SignatureImageBytes, x.SignatureMime })
                .FirstOrDefaultAsync();

            return p == null ? null : (p.SignatureImageBytes!, p.SignatureMime ?? "image/png");
        }

        public async Task<DateTime> GuardarCartaFirmadaPorPostulante(
            int onboardingId, CartaOfertaPersistDto carta, FileDigitalCarpetaDto? carpeta)
        {
            using var ctx = _factory.CreateDbContext();

            var ob = await ctx.GthOnboarding.FirstOrDefaultAsync(o => o.GthOnboardingId == onboardingId && o.State)
                ?? throw new AbrilException(TokenInvalido, 404);

            var now = DateTimeOffset.UtcNow;

            ob.CartaFirmadaNombre = carta.Nombre;
            ob.CartaFirmadaUrl    = carta.Url;
            ob.CartaFirmadaItemId = carta.ItemId;
            ob.CartaFirmadaDriveId = carta.DriveId;
            ob.CartaFirmadaSubidaDateTime = now;
            // Queda en null a propósito: el que firmó es el postulante, que no es un usuario del
            // sistema. La fecha de abajo es la que dice que la firma vino del enlace y no de GTH.
            ob.CartaFirmadaSubidaUserId       = null;
            ob.CartaFirmadaPostulanteDateTime = now;

            // Reemplazar el documento anula la aprobación anterior, igual que cuando GTH lo reemplaza:
            // lo que se aprobó ya no es lo que está adjunto.
            ob.CartaFirmadaAprobadaDateTime = null;
            ob.CartaFirmadaAprobadaUserId   = null;

            // Onboardings abiertos antes de que se persistiera el file digital: se completa con la
            // carpeta que acaba de resolver el servicio.
            if (carpeta != null && string.IsNullOrWhiteSpace(ob.FileDigitalItemId))
            {
                ob.FileDigitalDriveId = carpeta.DriveId;
                ob.FileDigitalItemId  = carpeta.ItemId;
                ob.FileDigitalRuta    = carpeta.Ruta;
            }

            ob.UpdatedDateTime = now;
            // Sin updated_user_id: no hay usuario del sistema detrás de esta escritura.

            await ctx.SaveChangesAsync();

            return now.ToOffset(PeruOffset).DateTime;
        }
    }
}
