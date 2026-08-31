using Abril_Backend.Application.Exceptions;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Interfaces;
using Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Interfaces;
using Abril_Backend.Shared.Services.Graph.Interfaces;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Services
{
    /// <inheritdoc cref="IWorkerEmailValidator"/>
    public class WorkerEmailValidator : IWorkerEmailValidator
    {
        /// <summary>Dominio de los buzones del tenant de Abril.</summary>
        private const string DominioAbril = "@abril.pe";

        private readonly IWorkerSearchRepository _repo;
        private readonly IGraphUserService _graphUserService;

        public WorkerEmailValidator(IWorkerSearchRepository repo, IGraphUserService graphUserService)
        {
            _repo = repo;
            _graphUserService = graphUserService;
        }

        public async Task<EmailCorporativoValidacionDto> ValidarCorporativoAsync(
            string? email, int? workerId, bool? esCorporativo, bool obligatorio = false)
        {
            var contexto = await _repo.GetContextoEmailCorporativo(Normalizar(email), workerId);
            return await ValidarCorporativoAsync(email, contexto, esCorporativo, obligatorio, omitirSiNoCambio: false);
        }

        public async Task<WorkerCorreosDto> ValidarYNormalizarAsync(
            string? emailCorporativo, string? emailPersonal, int? workerId, bool? esCorporativo)
        {
            // Único roundtrip: clasificación y correos actuales del trabajador + dueño del corporativo.
            var contexto = await _repo.GetContextoEmailCorporativo(Normalizar(emailCorporativo), workerId);

            var resultado = await ValidarCorporativoAsync(
                emailCorporativo, contexto, esCorporativo, obligatorio: false, omitirSiNoCambio: true);

            if (!resultado.Valido)
                throw new AbrilException(resultado.Mensaje ?? "El correo corporativo no es válido.", StatusCodeDe(resultado.Motivo));

            var correos = new WorkerCorreosDto
            {
                Corporativo = resultado.Email,
                Personal = ValidarPersonal(emailPersonal, contexto.WorkerEmailPersonalActual),
            };

            ExigirAlMenosUnCorreo(correos, contexto, esNuevo: workerId is null);

            return correos;
        }

        /// <summary>
        /// El correo personal es un dato de contacto: solo se valida el formato. Puede repetirse
        /// entre trabajadores (el de RR.HH. de una contratista lo comparten sus fichas) y no se
        /// contrasta contra el tenant porque no es un buzón de Abril. Si no cambió respecto al ya
        /// guardado se acepta tal cual, para no bloquear ediciones por datos legados.
        /// </summary>
        private static string? ValidarPersonal(string? email, string? emailActual)
        {
            var normalizado = Normalizar(email);
            if (normalizado is null) return null;
            if (normalizado == Normalizar(emailActual)) return normalizado;

            if (!FormatoValido(normalizado))
                throw new AbrilException(
                    "El correo personal no tiene un formato válido (ejemplo: nombre@gmail.com).", 400);

            return normalizado;
        }

        /// <summary>
        /// Todo trabajador debe quedar con al menos un correo. Al editar solo se exige si la ficha ya
        /// tenía alguno: hay miles de fichas legadas sin ningún correo y no deben quedar imposibles
        /// de editar, pero tampoco se permite borrar el último correo de las que sí lo tienen.
        /// </summary>
        private static void ExigirAlMenosUnCorreo(WorkerCorreosDto correos, EmailCorporativoContextoDto contexto, bool esNuevo)
        {
            if (correos.Corporativo is not null || correos.Personal is not null) return;

            if (esNuevo)
                throw new AbrilException(
                    "Debes registrar al menos un correo: el corporativo o el personal.", 400);

            var teniaCorreo = Normalizar(contexto.WorkerEmailActual) is not null
                           || Normalizar(contexto.WorkerEmailPersonalActual) is not null;

            if (teniaCorreo)
                throw new AbrilException(
                    "El trabajador debe conservar al menos un correo: el corporativo o el personal.", 400);
        }

        /// <summary>
        /// Núcleo de la validación del corporativo. No vuelve a consultar la BD (recibe el contexto);
        /// solo consulta Graph cuando el correo es efectivamente corporativo.
        /// </summary>
        /// <param name="omitirSiNoCambio">
        /// Si true y el correo es idéntico al ya guardado para ese trabajador, se acepta sin
        /// revalidar. Necesario al guardar: hay fichas antiguas con correos que hoy no pasarían la
        /// validación (correos personales en trabajadores de Staff, cuentas ya dadas de baja en el
        /// tenant) y no deben bloquear la edición de otros campos.
        /// </param>
        private async Task<EmailCorporativoValidacionDto> ValidarCorporativoAsync(
            string? email, EmailCorporativoContextoDto contexto, bool? esCorporativo, bool obligatorio, bool omitirSiNoCambio)
        {
            var normalizado = Normalizar(email);

            if (normalizado is null)
            {
                return obligatorio
                    ? Invalido(EmailCorporativoMotivo.Obligatorio,
                        "El correo corporativo es obligatorio para el personal de Staff y Oficina Central.", null)
                    : Valido(null);
            }

            if (!FormatoValido(normalizado))
                return Invalido(EmailCorporativoMotivo.Formato,
                    "El correo no tiene un formato válido (ejemplo: nombre.apellido@abril.pe).", normalizado);

            if (omitirSiNoCambio && normalizado == Normalizar(contexto.WorkerEmailActual))
                return Valido(normalizado);

            // La clasificación explícita (la que envía el formulario) manda; si no viene, se usa la
            // guardada en BD. Un correo del dominio de Abril es un buzón del tenant en cualquier caso.
            var corporativo = (esCorporativo ?? EsClasificacionCorporativa(contexto.WorkerContrataCasa, contexto.WorkerObraOficinaStaffId))
                              || normalizado.EndsWith(DominioAbril, StringComparison.Ordinal);

            // Un correo no corporativo en esta columna es un dato legado (antes de existir el campo
            // de correo personal): se deja pasar sin exigir tenant ni unicidad.
            if (!corporativo)
                return Valido(normalizado);

            if (contexto.OcupadoPorWorkerId.HasValue)
                return new EmailCorporativoValidacionDto
                {
                    Valido = false,
                    Motivo = EmailCorporativoMotivo.YaTomado,
                    Mensaje = $"El correo {normalizado} ya está asignado a {DescribirOcupante(contexto)}. " +
                              "Cada correo corporativo puede pertenecer a un solo trabajador.",
                    Email = normalizado,
                    OcupadoPorWorkerId = contexto.OcupadoPorWorkerId,
                    OcupadoPorNombre = contexto.OcupadoPorNombre,
                };

            // El buzón debe existir en el directorio de Abril (tenant de Microsoft). Se consulta con
            // permiso de aplicación: los correos inexistentes no vuelven en el diccionario.
            var perfiles = await _graphUserService.GetUsersByEmailsAsync(new List<string> { normalizado });

            if (!perfiles.TryGetValue(normalizado, out var perfil) || string.IsNullOrWhiteSpace(perfil.Mail))
                return Invalido(EmailCorporativoMotivo.NoExisteEnTenant,
                    $"El correo {normalizado} no existe en el directorio de Abril (Microsoft). " +
                    "Verifica que esté bien escrito, o regístralo como correo personal si no es una cuenta de Abril.",
                    normalizado);

            // Se persiste el correo canónico del directorio (en minúsculas, como el resto del
            // sistema), así coincide con el que llega en el login SSO.
            return new EmailCorporativoValidacionDto
            {
                Valido = true,
                Email = Normalizar(perfil.Mail) ?? normalizado,
                NombreEnTenant = perfil.DisplayName,
                VerificadoEnTenant = true,
            };
        }

        /// <summary>
        /// Un correo es corporativo cuando el trabajador es personal de Abril de Staff u Oficina
        /// Central: ahí el formulario muestra el campo "Email corporativo". Obra y contratistas solo
        /// tienen correo personal.
        /// </summary>
        public static bool EsClasificacionCorporativa(string? contrataCasa, int? obraOficinaStaffId)
        {
            if (!string.Equals(contrataCasa?.Trim(), "Casa", StringComparison.OrdinalIgnoreCase))
                return false;

            return Abril_Backend.Shared.Constants.ObraOficinaStaffIds.StaffUOficinaCentral.Contains(obraOficinaStaffId ?? 0);
        }

        /// <summary>Minúsculas y sin espacios; null si el campo viene vacío.</summary>
        private static string? Normalizar(string? email)
        {
            var limpio = email?.Trim().ToLowerInvariant();
            return string.IsNullOrEmpty(limpio) ? null : limpio;
        }

        /// <summary>Verificación mínima: algo@algo.algo, un solo arroba y sin espacios.</summary>
        private static bool FormatoValido(string email)
        {
            var arroba = email.IndexOf('@');
            return arroba > 0
                && arroba == email.LastIndexOf('@')
                && arroba < email.Length - 1
                && !email.Contains(' ')
                && email.IndexOf('.', arroba) > arroba + 1;
        }

        private static string DescribirOcupante(EmailCorporativoContextoDto contexto)
        {
            var nombre = string.IsNullOrWhiteSpace(contexto.OcupadoPorNombre)
                ? "otro trabajador"
                : contexto.OcupadoPorNombre!.Trim();

            return string.IsNullOrWhiteSpace(contexto.OcupadoPorDni)
                ? nombre
                : $"{nombre} (doc. {contexto.OcupadoPorDni!.Trim()})";
        }

        private static EmailCorporativoValidacionDto Valido(string? email) =>
            new() { Valido = true, Email = email };

        private static EmailCorporativoValidacionDto Invalido(string motivo, string mensaje, string? email) =>
            new() { Valido = false, Motivo = motivo, Mensaje = mensaje, Email = email };

        private static int StatusCodeDe(string? motivo) => motivo switch
        {
            EmailCorporativoMotivo.YaTomado => 409,
            EmailCorporativoMotivo.NoExisteEnTenant => 404,
            _ => 400,
        };
    }
}
