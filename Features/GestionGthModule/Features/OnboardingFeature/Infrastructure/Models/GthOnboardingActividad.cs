namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del checklist operativo del onboarding (tabla <c>gth_onboarding_actividad</c>): las
    /// actividades obligatorias de cada fase, en el orden del requerimiento funcional (punto 9.2).
    ///
    /// Es lo que pinta el checklist del modal de detalle y de donde salen los contadores de avance
    /// («2 de 19 tareas»): el % del onboarding se calcula sobre estas actividades, no sobre las
    /// fases, porque una fase puede tener una sola actividad y otra ocho.
    ///
    /// <c>codigo</c> es la clave estable con la que la pantalla reconoce cada actividad y le pone su
    /// tarjeta propia (la de la carta oferta firmada tiene adjunto y aprobación; las automáticas
    /// muestran el resumen del aviso que se envió).
    /// </summary>
    public class GthOnboardingActividad
    {
        public int GthOnboardingActividadId { get; set; }

        /// <summary>FK a <c>gth_onboarding_fase</c>: a qué paso del checklist pertenece.</summary>
        public int GthOnboardingFaseId { get; set; }

        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;

        /// <summary>Qué implica la actividad (texto de apoyo para la pantalla).</summary>
        public string? Descripcion { get; set; }

        /// <summary>Posición dentro de su fase.</summary>
        public int Orden { get; set; }

        /// <summary>
        /// true = la cumple el sistema solo, sin acción de GTH: se da por hecha en cuanto el
        /// colaborador entra a onboarding. Hoy son los dos avisos preventivos (TI y responsable de
        /// obra/sede) que, según el requerimiento, se disparan al registrar la solicitud.
        ///
        /// ⚠️ Ese envío todavía NO está implementado (RF-ONB-13 y RF-ONB-14, sprint S2): el checklist
        /// las muestra hechas porque así está definido el proceso, pero ningún correo sale aún — y
        /// por eso un onboarding recién abierto arranca en 2 de 19 y no en 0. Cuando se implementen
        /// no hay que tocar nada acá; si mientras tanto se prefiere que el avance refleje solo lo
        /// realmente ocurrido, basta con poner <c>automatica = false</c> en esas dos filas.
        /// </summary>
        public bool Automatica { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
