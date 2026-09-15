using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// Token de un solo uso para restablecer contraseña de <see cref="UsuarioSistema"/> — mismo
    /// patrón que el legacy de Abril (SsResetToken / UserPasswordToken): tabla propia en vez de
    /// columnas en el usuario, así puede haber historial y nunca hay que limpiar un token viejo
    /// a mano para pedir uno nuevo (se invalidan los anteriores al pedir uno nuevo).
    /// </summary>
    [Table("lb_usuario_password_token")]
    public class LbUsuarioPasswordToken
    {
        public long Id { get; set; }
        public long UsuarioSistemaId { get; set; }
        public string Token { get; set; } = null!;
        public DateTime ExpiraEn { get; set; }
        public bool Usado { get; set; } = false;
        public DateTime CreadoEn { get; set; }

        [ForeignKey(nameof(UsuarioSistemaId))]
        public UsuarioSistema? UsuarioSistema { get; set; }
    }
}
