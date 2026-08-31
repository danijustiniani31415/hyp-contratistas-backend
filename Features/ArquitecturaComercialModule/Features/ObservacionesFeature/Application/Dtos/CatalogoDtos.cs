namespace Abril_Backend.Features.ArquitecturaComercialModule.Features.ObservacionesFeature.Application.Dtos;

public class CatalogoItemDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; }
}

public class CreateCatalogoItemDTO
{
    public string Nombre { get; set; } = string.Empty;
}

public class UpdateCatalogoItemDTO
{
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
}
