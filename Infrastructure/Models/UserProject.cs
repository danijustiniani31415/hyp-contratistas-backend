namespace Abril_Backend.Infrastructure.Models {
    public class UserProject {
        public int UserProjectId {get; set;}
        /// <summary>
        /// Ahora NULLABLE: las asignaciones de Lecciones Aprendidas son por TRABAJADOR
        /// (<see cref="WorkerId"/>); un trabajador puede no tener usuario en app_user.
        /// Se conserva para compatibilidad con otras funcionalidades (Evaluaciones, etc.).
        /// </summary>
        public int? UserId {get; set;}
        /// <summary>FK a workers.id. Vínculo principal trabajador↔proyecto.</summary>
        public int? WorkerId {get; set;}
        public int ProjectId { get; set; }
        public DateTime CreatedDateTime {get; set;}
        public int CreatedUserId {get; set;}
        public DateTime? UpdatedDateTime {get; set;}
        public int? UpdatedUserId {get; set;}
        public bool Active {get; set;}
        public bool State {get; set;}
    }
}