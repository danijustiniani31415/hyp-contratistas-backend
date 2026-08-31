namespace Abril_Backend.Infrastructure.Models {
    public class Lesson {
        public int LessonId {get; set;}
        public string? LessonCode {get; set;}
        public string Period {get;set;} // ejm: 12-2025
        public DateTimeOffset? PeriodDate {get;set;} // todos los periodDate son Period pero con 01 para filtrar más rápido: 2025-12-01
        public string? ProblemDescription {get; set;}
        public string? ReasonDescription {get; set;}
        public string? LessonDescription {get; set;}
        public string? ImpactDescription {get; set;}
        public int? ProjectId {get; set;}
        public int AreaId {get; set;}
        // Nota: la columna legacy `phase_stage_sub_stage_sub_specialty_id` queda
        // huérfana en BD (sin mapping EF) — se conserva para no perder histórico.
        public int? CatalogItemId {get; set;}
        /// <summary>Rama seleccionada en lecciones (modelo nuevo). Nullable para no romper lecciones viejas.</summary>
        public int? LessonAreaId {get; set;}
        /// <summary>
        /// FK a <c>workers_obra_oficina_staff</c>: Obra / Staff / Oficina Central.
        /// Antes esta diferenciación se guardaba implícitamente en el último nodo
        /// del árbol de áreas (tipo "Área Obra_Oficina"); ahora es un campo propio
        /// de la lección que el autor elige en un desplegable aparte.
        /// </summary>
        public int? ObraOficinaStaffId {get; set;}
        public int StateId {get; set;}

        // ── Flujo de aprobación por jefatura ────────────────────────────────
        /// <summary>PENDIENTE | APROBADA | RECHAZADA. Las lecciones nuevas nacen PENDIENTE.</summary>
        public string ApprovalStatus {get; set;} = "PENDIENTE";
        /// <summary>user_id del jefe que aprobó/rechazó (null si aún pendiente).</summary>
        public int? ReviewedByUserId {get; set;}
        public DateTimeOffset? ReviewedAt {get; set;}
        /// <summary>Comentario del jefe al rechazar (opcional).</summary>
        public string? RejectionComment {get; set;}

        public DateTimeOffset CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTimeOffset? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}