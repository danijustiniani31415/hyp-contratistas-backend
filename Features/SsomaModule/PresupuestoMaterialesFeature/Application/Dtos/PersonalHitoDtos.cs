namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Dtos;

/// <summary>Fila de dotación de personal SSOMA (Prevencionista/Monitor/Vigía) asignada a un hito
/// crítico real del cronograma del proyecto.</summary>
public class PersonalHitoDto
{
    public int Id { get; set; }
    public int HitoId { get; set; }
    public string HitoDescripcion { get; set; } = "";
    public DateOnly? HitoFecha { get; set; }
    public bool EsHitoCritico { get; set; }
    public string Rol { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Semanas { get; set; }
    public decimal CostoMensual { get; set; }
    public decimal Total { get; set; }
}

/// <summary>Hito crítico disponible para asignarle personal (aunque todavía no tenga fila cargada).</summary>
public class HitoCriticoDisponibleDto
{
    public int HitoId { get; set; }
    public string HitoDescripcion { get; set; } = "";
    public DateOnly? HitoFecha { get; set; }
}

public class PersonalHitoItemInputDto
{
    public int HitoId { get; set; }
    public string Rol { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Semanas { get; set; }
    public decimal CostoMensual { get; set; }
}

public class PersonalHitoGuardarDto
{
    public List<PersonalHitoItemInputDto> Items { get; set; } = [];
}
