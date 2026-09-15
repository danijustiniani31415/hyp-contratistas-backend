using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.PersonasModule.Infrastructure.Models
{
    [Table("lb_persona")]
    public class Persona
    {
        public int Id { get; set; }
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string TipoDocumento { get; set; } = "DNI";
        public string NumeroDocumento { get; set; } = null!;
        public DateOnly? FechaNacimiento { get; set; }
        public string? Telefono { get; set; }
        public string? EmailPersonal { get; set; }
        public string? FotoUrl { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset CreadoEn { get; set; }
        public DateTimeOffset ActualizadoEn { get; set; }

        public ICollection<VinculoLaboral> Vinculos { get; set; } = new List<VinculoLaboral>();
        public UsuarioSistema? UsuarioSistema { get; set; }
    }
}
