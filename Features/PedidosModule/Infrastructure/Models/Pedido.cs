using System.ComponentModel.DataAnnotations.Schema;
using Abril_Backend.Features.PersonasModule.Infrastructure.Models;

namespace Abril_Backend.Features.PedidosModule.Infrastructure.Models
{
    /// <summary>
    /// Fase 2, punto 4 — no tenía DDL en CONTEXT_LOGISTICA.md, diseñado siguiendo los mismos
    /// patrones de Fase 1 (trazabilidad de quién aprueba/entrega, permisos en vez de código de
    /// rol hardcodeado). Flujo (2026-09-21, dos niveles de aprobación): PENDIENTE (creado, ej. por
    /// el Almacenero de mina) → PENDIENTE_GERENTE (visado por el Residente) → APROBADO/RECHAZADO
    /// (Gerencia/Logística) → ENTREGADO (Almacenero que despacha, descuenta stock vía
    /// lb_movimiento) — o CANCELADO por el propio solicitante mientras siga PENDIENTE.
    /// </summary>
    [Table("lb_pedido")]
    public class Pedido
    {
        public long Id { get; set; }
        public string Codigo { get; set; } = null!;
        public int ProyectoId { get; set; }
        public int AlmacenId { get; set; }
        public long SolicitanteUsuarioSistemaId { get; set; }
        public string Estado { get; set; } = "PENDIENTE";
        public string? Observacion { get; set; }
        public string? MotivoRechazo { get; set; }
        public long? VisadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset? VisadoEn { get; set; }
        public long? AprobadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset? AprobadoEn { get; set; }
        public long? EntregadoPorUsuarioSistemaId { get; set; }
        public DateTimeOffset? EntregadoEn { get; set; }
        public DateTimeOffset CreadoEn { get; set; }

        [ForeignKey(nameof(ProyectoId))]
        public Proyecto? Proyecto { get; set; }
        [ForeignKey(nameof(AlmacenId))]
        public Almacen? Almacen { get; set; }
        [ForeignKey(nameof(SolicitanteUsuarioSistemaId))]
        public UsuarioSistema? Solicitante { get; set; }
        [ForeignKey(nameof(VisadoPorUsuarioSistemaId))]
        public UsuarioSistema? VisadoPor { get; set; }
        [ForeignKey(nameof(AprobadoPorUsuarioSistemaId))]
        public UsuarioSistema? AprobadoPor { get; set; }
        [ForeignKey(nameof(EntregadoPorUsuarioSistemaId))]
        public UsuarioSistema? EntregadoPor { get; set; }

        public ICollection<PedidoItem> Items { get; set; } = new List<PedidoItem>();
    }
}
