using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.GestionAdministrativa.Shared.Models
{
    /// <summary>
    /// Catálogo de los correos configurables del flujo de solicitud de salidas. Cada fila es
    /// uno de los correos que el sistema envía (REVISOR, CONFIRMACION, APROBADA, RECHAZADA) y
    /// sobre el que se pueden definir reglas de inclusión/exclusión de destinatarios
    /// (<see cref="GaCorreoRegla"/>). Sirve para no hardcodear los destinatarios en código.
    /// </summary>
    [Table("ga_correo_evento")]
    public class GaCorreoEvento
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>Clave estable del correo (REVISOR, CONFIRMACION, APROBADA, RECHAZADA).</summary>
        [Column("codigo")]
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Nombre para mostrar en la pantalla de configuración.</summary>
        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Descripción de cuándo se envía y a quién (informativa para la UI).</summary>
        [Column("descripcion")]
        public string? Descripcion { get; set; }

        /// <summary>Orden de visualización.</summary>
        [Column("orden")]
        public int Orden { get; set; }

        /// <summary>
        /// Interruptor maestro: si false, este correo NO se envía (su configuración se conserva).
        /// Solo se puede apagar desde la pantalla si <see cref="PermiteDesactivarEnvio"/>.
        /// </summary>
        [Column("active")]
        public bool Active { get; set; } = true;

        /// <summary>
        /// Etiqueta del destinatario principal que calcula el backend (el revisor en REVISOR,
        /// el solicitante en los demás). Solo informativa: la pantalla la muestra junto al
        /// interruptor del principal.
        /// </summary>
        [Column("destinatario_principal_nombre")]
        public string? DestinatarioPrincipalNombre { get; set; }

        /// <summary>
        /// Si false, el correo no se manda a su destinatario principal: solo a los destinatarios
        /// configurados en <see cref="GaCorreoRegla"/>. Si no queda ninguno, no se envía nada.
        /// Solo se puede apagar desde la pantalla si <see cref="PermiteDesactivarPrincipal"/>.
        /// </summary>
        [Column("destinatario_principal_activo")]
        public bool DestinatarioPrincipalActivo { get; set; } = true;

        /// <summary>true = la pantalla muestra el interruptor maestro (<see cref="Active"/>) de este correo.</summary>
        [Column("permite_desactivar_envio")]
        public bool PermiteDesactivarEnvio { get; set; }

        /// <summary>true = la pantalla muestra el interruptor de <see cref="DestinatarioPrincipalActivo"/>.</summary>
        [Column("permite_desactivar_principal")]
        public bool PermiteDesactivarPrincipal { get; set; }

        /// <summary>Soft delete: false = eliminado (se conserva para auditoría).</summary>
        [Column("state")]
        public bool State { get; set; } = true;

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
