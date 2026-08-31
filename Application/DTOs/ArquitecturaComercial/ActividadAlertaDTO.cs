namespace Abril_Backend.Application.DTOs.ArquitecturaComercial;

public class ActividadAlertaDTO
{
    public int      Id            { get; set; }
    public string   Nombre        { get; set; } = "";
    public string   Proyecto      { get; set; } = "";
    public string?  Responsable1  { get; set; }
    public string?  Responsable2  { get; set; }
    public string?  EmailResp1    { get; set; }
    public string?  EmailResp2    { get; set; }
    public string?  FechaInicio   { get; set; }
    public string?  FechaFin      { get; set; }
    public string?  Estado        { get; set; }
    public decimal? Spi           { get; set; }
    public string   Tipo          { get; set; } = "";
    public string?  Categoria     { get; set; }
    public int      DiasRestantes { get; set; }
}
