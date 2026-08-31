namespace Abril_Backend.Application.DTOs.ArquitecturaComercial;

public class TareasPorArquitectoDTO
{
    public int     UserId      { get; set; }
    public string  Nombre      { get; set; } = "";
    public int     Hitos       { get; set; }
    public int     Entregables { get; set; }
    public int     Consultas   { get; set; }
    public int     Total       { get; set; }
    public decimal AvancePct   { get; set; }
}

public class AvanceSemanalDTO
{
    public string  Semana     { get; set; } = "";
    public decimal Programado { get; set; }
    public decimal Real       { get; set; }
}

public class EficienciaSpiDTO
{
    public string  Semana   { get; set; } = "";
    public decimal Spi      { get; set; }
    public decimal Esperado { get; set; } = 1.0m;
}

public class CategoriaItemDTO
{
    public int    Id     { get; set; }
    public string Nombre { get; set; } = "";
}
