namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Destinatario de un correo configurable del módulo de Reclutamiento
    /// (tabla <c>gth_correo_destinatario</c>). Se define por BD y se edita desde la pantalla
    /// de Configuración para poder alternar entre un correo de pruebas y el de producción
    /// sin redeploy. Cada fila pertenece a un <see cref="GthCorreoTipo"/> (APROBACION_GG /
    /// SOLICITUD / LONG_LIST / …): así conviven los destinatarios de todos los correos del
    /// módulo en la misma tabla.
    ///
    /// Hay dos clases de fila, y se distinguen por <see cref="Codigo"/>:
    ///  • <b>Dinámica</b> (<c>Codigo</c> no nulo, <c>Email</c> nulo): el correo no está
    ///    guardado, se resuelve al momento de enviar (Gerente General, gerente del área,
    ///    área de GTH). Solo se prende y apaga.
    ///  • <b>Correo adicional</b> (<c>Codigo</c> nulo, <c>Email</c> obligatorio): se escribe
    ///    a mano desde la pantalla y se puede editar y eliminar.
    ///
    /// <c>es_copia</c> = false → destinatario principal (Para/To); true → copia (CC).
    /// <c>active</c> = false → sigue configurado pero no recibe el correo.
    /// </summary>
    public class GthCorreoDestinatario
    {
        public int GthCorreoDestinatarioId { get; set; }
        /// <summary>FK a <see cref="GthCorreoTipo"/>: a qué correo pertenece este destinatario.</summary>
        public int GthCorreoTipoId { get; set; }
        /// <summary>
        /// Código estable del destinatario dinámico (GERENTE_GENERAL, GERENTE_AREA, GTH_AREA).
        /// Null en los correos adicionales escritos a mano.
        /// </summary>
        public string? Codigo { get; set; }
        /// <summary>
        /// Correo destinatario (se guarda en minúsculas). Null en los destinatarios dinámicos:
        /// esos lo resuelven al enviar.
        /// </summary>
        public string? Email { get; set; }
        /// <summary>Nombre para mostrar en la pantalla ("Gerente General", "Jefatura de obra", …).</summary>
        public string? Nombre { get; set; }
        /// <summary>Texto de ayuda que explica de dónde sale el correo.</summary>
        public string? Descripcion { get; set; }
        /// <summary>false = Para (To); true = Copia (CC).</summary>
        public bool EsCopia { get; set; }
        /// <summary>Orden de la fila en la pantalla. Los dinámicos van primero, los adicionales en 100+.</summary>
        public int Orden { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        /// <summary>Interruptor de la fila: false = configurado pero no recibe el correo.</summary>
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
