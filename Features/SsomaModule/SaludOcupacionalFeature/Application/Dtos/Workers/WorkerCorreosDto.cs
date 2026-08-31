namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    /// <summary>
    /// Los dos correos de un trabajador ya validados y normalizados, listos para persistir.
    /// Null significa "sin correo" en esa columna.
    /// </summary>
    public class WorkerCorreosDto
    {
        /// <summary>Va a <c>workers.email_corporativo</c>.</summary>
        public string? Corporativo { get; set; }

        /// <summary>Va a <c>person.email</c>.</summary>
        public string? Personal { get; set; }
    }
}
