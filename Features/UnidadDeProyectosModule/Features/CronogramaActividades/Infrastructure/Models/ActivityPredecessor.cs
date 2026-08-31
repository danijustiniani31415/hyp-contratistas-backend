namespace Abril_Backend.Shared.Models
{
    /// <summary>
    /// Relación N:N de dependencia Fin a Inicio (FS) entre actividades del cronograma.
    /// ActivityId = actividad sucesora; PredecessorId = actividad predecesora.
    /// </summary>
    public class ActivityPredecessor
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int PredecessorId { get; set; }
    }
}
