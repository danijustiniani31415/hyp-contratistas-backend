namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application
{
    /// <summary>
    /// Códigos estables de la respuesta del candidato a su citación (espejo de
    /// <c>gth_entrevista_respuesta.codigo</c>). Los envían los dos botones del correo de
    /// invitación, así que también son los valores que acepta el endpoint público.
    /// </summary>
    public static class EntrevistaRespuestaCodigo
    {
        /// <summary>El candidato confirma que asistirá a la entrevista.</summary>
        public const string Confirmada = "CONFIRMADA";

        /// <summary>El candidato avisa que no podrá asistir a la entrevista.</summary>
        public const string Rechazada = "RECHAZADA";

        /// <summary>
        /// Normaliza lo que llega por la URL del correo (<c>confirmar</c> / <c>rechazar</c>, y los
        /// propios códigos por si el enlace se arma con ellos) al código del catálogo. Devuelve
        /// null si no corresponde a ninguna respuesta conocida.
        /// </summary>
        public static string? Normalizar(string? valor) => valor?.Trim().ToUpperInvariant() switch
        {
            "CONFIRMAR" or "CONFIRMADA" => Confirmada,
            "RECHAZAR" or "RECHAZADA"   => Rechazada,
            _ => null,
        };
    }
}
