using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    /// <summary>
    /// 1 persona = máximo 1 usuario de sistema (uq_usuario_sistema_persona). Si una persona vuelve
    /// a necesitar login tras haberlo perdido, REACTIVAR esta fila (Estado = "ACTIVO"), nunca
    /// insertar una nueva — violaría el constraint a propósito. PasswordHash se genera con
    /// IPasswordHasher&lt;T&gt; de ASP.NET Identity (mismo patrón que Infrastructure/Repositories/
    /// UserRepository.cs) — no introducir BCrypt ni otra librería.
    /// </summary>
    [Table("lb_usuario_sistema")]
    public class UsuarioSistema
    {
        public long Id { get; set; }
        public int PersonaId { get; set; }
        public string EmailLogin { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string Estado { get; set; } = "ACTIVO";
        public DateTimeOffset? UltimoAcceso { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }
        public ICollection<UsuarioAsignacion> Asignaciones { get; set; } = new List<UsuarioAsignacion>();
    }
}
