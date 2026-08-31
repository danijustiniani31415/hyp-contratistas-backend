namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Programacion;

public class ProgramacionHabilitacionDto
{
    public int Id { get; set; }
    public string Trabajador { get; set; } = "";
    public string Dni { get; set; } = "";
    public string Proyecto { get; set; } = "";
    public string RazonSocial { get; set; } = "";
    public string Estado { get; set; } = "";
    public string FechaProgramada { get; set; } = "";
    public string? Hora { get; set; }
    public bool Notificado { get; set; }
}

public class ProgramacionHabilitacionFiltrosDto
{
    public string? Estado { get; set; }
    public int? ProyectoId { get; set; }
    public string? Fecha { get; set; }
    public bool? SoloNoNotificados { get; set; }
}

public class PatchNotificadoDto
{
    public bool Notificado { get; set; }
}
