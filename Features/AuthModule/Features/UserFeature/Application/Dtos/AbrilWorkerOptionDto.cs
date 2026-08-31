namespace Abril_Backend.Features.AuthModule.UserFeature.Application.Dtos
{
    /// <summary>
    /// Trabajador de Abril (existe en <c>workers</c> con <c>email_corporativo</c> @abril.pe)
    /// que aún NO tiene un usuario activo en <c>app_user</c>. Sirve para poblar el
    /// desplegable de "Crear usuario para trabajador de Abril".
    /// </summary>
    public class AbrilWorkerOptionDto
    {
        public int PersonId { get; set; }
        public string FullName { get; set; } = null!;
        public string? DocumentIdentityCode { get; set; }
        public string EmailCorporativo { get; set; } = null!;
        /// <summary>FK a <c>workers_obra_oficina_staff</c> (ver <c>ObraOficinaStaffIds</c>).</summary>
        public int? ObraOficinaStaffId { get; set; }
    }
}
