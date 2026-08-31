namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion
{
    public class ProgramacionClinicaAccionDto
    {
        public int Id { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? MotivoRechazo { get; set; }
        public TimeOnly? CheckInHora { get; set; }
        public TimeOnly? HoraNueva { get; set; }
        public int? EmoResultadoId { get; set; }
        public DateOnly? NuevaFecha { get; set; }

        /// <summary>
        /// Solo aplica con Accion="Aceptar" (incluye Reprogramar, que reusa esa misma acción):
        /// permite corregir el Tipo de EMO antes de aceptar, para el caso en que la clínica
        /// se equivocó al programar. Si viene null o igual al actual, no cambia nada.
        /// </summary>
        public int? TipoEmoId { get; set; }

        /// <summary>
        /// Solo aplica con Accion="Rechazar": si viene en false, no se envía el correo de
        /// notificación de rechazo. Default true (mismo comportamiento que antes) — la clínica
        /// lo desmarca cuando el rechazo es de una fila que no corresponde avisar a nadie (p.ej.
        /// un duplicado generado por el auto-programador).
        /// </summary>
        public bool? EnviarCorreo { get; set; }
    }
}
