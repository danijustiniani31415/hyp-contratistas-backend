using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.EppModule.Infrastructure.Models
{
    /// <summary>
    /// Fase 2, punto 5 — entrega individual de EPP a un trabajador. Antes de crear una, el
    /// service valida que la persona tenga un vínculo laboral vigente cuyo TipoVinculo.RequiereEpp
    /// sea true (CONTEXT_LOGISTICA.md sección 4.1 — ej. un LOCADOR no recibe EPP, por el tipo de
    /// vínculo, no por un código hardcodeado).
    /// </summary>
    [Table("lb_entrega_epp")]
    public class EntregaEpp
    {
        public long Id { get; set; }
        public int PersonaId { get; set; }
        public int AlmacenId { get; set; }
        public long EntregadoPorUsuarioSistemaId { get; set; }
        public string? Observacion { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(PersonaId))]
        public Persona? Persona { get; set; }
        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
        [ForeignKey(nameof(EntregadoPorUsuarioSistemaId))]
        public UsuarioSistema? EntregadoPor { get; set; }

        public ICollection<EntregaEppItem> Items { get; set; } = new List<EntregaEppItem>();
    }
}
