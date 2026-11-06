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

        /// <summary>Primer nivel de aprobación — el Residente visa el pedido de mina, resolviendo
        /// cada ítem (aprobado completo, aprobado con cantidad reducida, o rechazado). Si al menos
        /// un ítem queda vivo: PENDIENTE → PENDIENTE_GERENTE. Si todos se rechazan: PENDIENTE →
        /// RECHAZADO.</summary>
        Task<PedidoDetailDto> Visar(long id, long visadorId, PedidoDecisionDto dto, LbScopeProyectos scope);

        /// <summary>Segundo nivel, definitivo — Gerencia resuelve cada ítem que el Residente dejó
        /// VISADO (no puede tocar los que el Residente ya rechazó, y no puede aprobar más cantidad
        /// de la que el Residente visó). Si al menos un ítem queda aprobado: PENDIENTE_GERENTE →
        /// APROBADO. Si todos terminan rechazados: PENDIENTE_GERENTE → RECHAZADO.</summary>
        Task<PedidoDetailDto> Aprobar(long id, long aprobadorId, PedidoDecisionDto dto, LbScopeProyectos scope);
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

        /// <summary>Bitácora de decisiones por ítem de este pedido (quién redujo/rechazó cada
        /// línea y cuándo) — Fase E, control fino de auditoría.</summary>
        Task<List<PedidoItemHistorialDto>> GetHistorial(long pedidoId);

        Task<List<ContratoDto>> ListContratos(int proyectoId);
        Task<ContratoDto> CrearContrato(ContratoCreateDto dto);
        Task<List<PartidaDto>> ListPartidas(int proyectoId);
        Task<PartidaDto> CrearPartida(PartidaCreateDto dto);
        Task<List<MonedaDto>> ListMonedas();
        Task<ConsumoPartidaDto> GetConsumoPartida(int partidaId);
    }
}
