namespace Abril_Backend.Features.SsomaModule.ProyectoHabilitadoFeature.Application.Dtos
{
    /// <summary>Fila de la pantalla de administración: todo proyecto activo + si está habilitado para SSOMA.</summary>
    public class ProyectoHabilitadoListDto
    {
        public int ProyectoId { get; set; }
        public string ProyectoDescription { get; set; } = null!;
        public bool Habilitado { get; set; }
        public bool ProyectoActivo { get; set; }
    }

    /// <summary>Proyecto habilitado para SSOMA, para usar en selects/filtros de las features.</summary>
    public class ProyectoSsomaSimpleDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
    }

    public class ProyectoHabilitadoToggleDto
    {
        public bool Habilitado { get; set; }
    }
}
