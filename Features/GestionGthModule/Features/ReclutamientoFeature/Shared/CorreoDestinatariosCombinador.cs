using Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos;

namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Shared
{
    /// <summary>
    /// Arma las listas Para/CC de los correos de Reclutamiento cuyo destinatario principal es fijo
    /// y lo pone el sistema (el postulante citado, el candidato que no continúa, el solicitante de
    /// la long list) más lo que aporte la configuración de la pantalla.
    ///
    /// Vive acá y no dentro de un servicio porque lo usan los dos servicios de la feature que
    /// envían correos con principal automático (<c>ReclutamientoService</c> y
    /// <c>PostulanteFormularioService</c>) y el criterio tiene que ser el mismo en ambos: si uno
    /// deduplicara distinto, el mismo buzón terminaría en Para en un correo y en CC en el otro.
    /// </summary>
    public static class CorreoDestinatariosCombinador
    {
        /// <summary>
        /// Deduplica sin distinguir mayúsculas y, si un mismo buzón está en ambas listas, se queda
        /// solo en Para — mismo criterio que la pantalla de configuración.
        ///
        /// El principal fijo se salta cuando la pantalla lo apagó
        /// (<see cref="SolicitudDestinatariosDto.PrincipalAutomaticoActivo"/>): el correo sale solo
        /// con los destinatarios configurados y, si no quedó ninguno, cada llamador decide qué
        /// hacer con las listas vacías.
        /// </summary>
        public static (List<string> Para, List<string> Copias) Combinar(
            string? principalFijo, SolicitudDestinatariosDto configurados)
        {
            var para   = new List<string>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Agregar(string? email)
            {
                var e = email?.Trim();
                if (!string.IsNullOrWhiteSpace(e) && vistos.Add(e)) para.Add(e);
            }

            if (configurados.PrincipalAutomaticoActivo) Agregar(principalFijo);
            foreach (var e in configurados.EmailsPara) Agregar(e);

            var copias = configurados.EmailsCopias
                .Where(e => !string.IsNullOrWhiteSpace(e) && !vistos.Contains(e.Trim()))
                .ToList();

            return (para, copias);
        }
    }
}
