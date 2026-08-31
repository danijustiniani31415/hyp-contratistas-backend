namespace Abril_Backend.Features.Habilitacion.Application.Dtos.ContratistaUsuarios
{
    public class ContratistaUsuarioListDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Email { get; set; }
        public string? RolNombre { get; set; }
        public string Scope { get; set; } = "TODOS";
        public bool Activo { get; set; }
        public List<int> ProyectoIds { get; set; } = new();
        public string? Modulos { get; set; }
        public int? WorkerId { get; set; }
        public bool EsWorker => WorkerId.HasValue;
    }

    public class ContratistaUsuarioCreateDto
    {
        public string Email { get; set; } = string.Empty;
        public string RolNombre { get; set; } = string.Empty;
        public int SystemRoleId { get; set; }
        public string Scope { get; set; } = "TODOS";
        public List<int>? ProyectoIds { get; set; }
        public string? Modulos { get; set; }
        public int? WorkerId { get; set; }
        public bool EsWorker { get; set; } = false;
    }

    public class ContratistaUsuarioUpdateDto
    {
        public string? Email { get; set; }
        public string? RolNombre { get; set; }
        public string? Scope { get; set; }
        public bool? Activo { get; set; }
        public List<int>? ProyectoIds { get; set; }
        public string? Modulos { get; set; }
    }
}
