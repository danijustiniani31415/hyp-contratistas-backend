namespace Abril_Backend.Application.DTOs
{
    public class UserWithoutLessonsDTO
    {
        /// <summary>Usuario asociado al trabajador (null si nunca se registró en app_user).</summary>
        public int? UserId { get; set; }
        public int WorkerId { get; set; }
        public string? UserFullName { get; set; }
        /// <summary>Correo corporativo del trabajador (worker.email_corporativo, @abril.pe).</summary>
        public string? Email {get;set;}
        public List<ProjectSimpleDTO>? Projects { get; set; }
    }
}