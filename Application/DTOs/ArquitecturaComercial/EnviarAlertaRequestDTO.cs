namespace Abril_Backend.Application.DTOs.ArquitecturaComercial;

public class EnviarAlertaRequestDTO
{
    public List<int> ActividadIds { get; set; } = [];
    public string    TipoAlerta   { get; set; } = "";
}
