namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo del estado de revisión de un candidato de la long list
    /// (tabla <c>gth_candidato_estado</c>): PENDIENTE (aún sin decidir), APROBADO o
    /// RECHAZADO por el solicitante. <c>codigo</c> es la clave estable usada en código.
    /// Por ahora los candidatos nacen y permanecen en PENDIENTE; la decisión del
    /// solicitante (aprobar/rechazar) se implementará después.
    /// </summary>
    public class GthCandidatoEstado
    {
        public int GthCandidatoEstadoId { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public int Orden { get; set; }

        public DateTimeOffset CreatedDateTime { get; set; }
        public int? CreatedUserId { get; set; }
        public DateTimeOffset? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; } = true;
        public bool State { get; set; } = true;
    }
}
