using Abril_Backend.Features.PedidosModule.Application.Dtos;
using Abril_Backend.Features.PersonasModule;

namespace Abril_Backend.Features.PedidosModule.Application.Interfaces
{
    public interface IPedidoService
    {
        Task<PedidoDetailDto> Crear(PedidoCreateDto dto, long solicitanteId);

        /// <summary>soloPropios=true restringe a los pedidos creados por usuarioSistemaId (para quien no
        /// tiene PEDIDO_VER_TODOS). proyectosPermitidos null = sin restricción de proyecto; con
        /// valor = solo esos proyectos, aunque tenga PEDIDO_VER_TODOS (se lo dieron acotado).</summary>
        Task<PedidoListResponseDto> List(string? estado, int? proyectoId, bool soloPropios, HashSet<int>? proyectosPermitidos, long usuarioSistemaId, int page, int pageSize);
        Task<PedidoDetailDto> GetById(long id);

        /// <summary>Primer nivel de aprobación — el Residente visa el pedido de mina antes de que
        /// pase a Gerencia. PENDIENTE → PENDIENTE_GERENTE.</summary>
        Task<PedidoDetailDto> Visar(long id, long visadorId, LbScopeProyectos scope);
        /// <summary>Rechazo en el nivel de visado (Residente) — PENDIENTE → RECHAZADO.</summary>
        Task<PedidoDetailDto> RechazarVisado(long id, long visadorId, RechazarPedidoDto dto, LbScopeProyectos scope);

        /// <summary>Segundo nivel, definitivo — PENDIENTE_GERENTE → APROBADO.</summary>
        Task<PedidoDetailDto> Aprobar(long id, long aprobadorId, LbScopeProyectos scope);
        /// <summary>Rechazo en el nivel de Gerencia — PENDIENTE_GERENTE → RECHAZADO.</summary>
        Task<PedidoDetailDto> Rechazar(long id, long aprobadorId, RechazarPedidoDto dto, LbScopeProyectos scope);
        Task<PedidoDetailDto> Entregar(long id, long entregadorId, LbScopeProyectos scope);
        Task<PedidoDetailDto> Cancelar(long id, long solicitanteId);

        /// <summary>Vista previa (sin enviar nada) de a qué correos llegaría cada notificación del
        /// flujo si un pedido de este proyecto cambiara de estado ahora mismo — para que el admin
        /// pueda verificar que los correos son los correctos antes de confiar en el flujo real.</summary>
        Task<PedidoDestinatariosDto> GetDestinatarios(int proyectoId);

        /// <summary>Ítems de pedidos APROBADOs con saldo pendiente de compra — el panel de
        /// Logística de "qué falta comprar", cruzando todos los pedidos activos. proyectosPermitidos
        /// null = sin restricción de proyecto.</summary>
        Task<List<PendienteCompraDto>> ListPendientesDeCompra(HashSet<int>? proyectosPermitidos);

        /// <summary>Ítems ya recibidos en almacén (de Lima) listos para despachar con guía —
        /// alimenta el selector "Despachar pedido" al crear una Guía de Remisión.</summary>
        Task<List<PendienteDespachoDto>> ListPendientesDeDespacho(HashSet<int>? proyectosPermitidos);
    }
}
