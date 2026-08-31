namespace Abril_Backend.Infrastructure.Models {
    public class MilestoneSchedule {
        public int MilestoneScheduleId {get; set;}
        /// <summary>Null cuando el hito es personalizado (no viene del catálogo global de Milestone),
        /// ej. una 2da/3ra grúa específica de este proyecto. En ese caso se usa CustomDescription.</summary>
        public int? MilestoneId {get; set;}
        /// <summary>Descripción libre para hitos personalizados (MilestoneId == null). Ignorado si
        /// MilestoneId tiene valor: en ese caso la descripción viene del catálogo Milestone.</summary>
        public string? CustomDescription {get; set;}
        public int MilestoneScheduleHistoryId {get;set;}
        public int Order {get;set;}
        public DateOnly? PlannedStartDate {get; set;}
        public DateOnly? PlannedEndDate {get; set;}
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
        public DateOnly? FechaRealFin {get; set;}
        /// <summary>Si es true, este hito representa un corte real de etapa constructiva (para
        /// segmentar consumo/dotación de personal por fase). Default false: la mayoría de hitos
        /// del cronograma son informativos (entregas, comercial) y no cortan una etapa.</summary>
        public bool EsHitoCritico {get; set;} = false;
    }
}