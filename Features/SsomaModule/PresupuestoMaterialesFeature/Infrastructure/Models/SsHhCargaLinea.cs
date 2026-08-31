using Abril_Backend.Shared.Models;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Infrastructure.Models;

/// <summary>
/// Línea semanal de HH de planilla (Año/Periodo Semanal/Trabajador/Ocupación/Horas laboradas),
/// carga acumulativa idempotente igual que <see cref="SsConsumoLinea"/>: se identifica por
/// proyecto+año+semana+trabajador+ocupación+partida+ocurrencia para poder actualizar
/// regularizaciones y dar de baja lo que ya no aparece en el acumulado, sin duplicar HH.
/// </summary>
public class SsHhCargaLinea
{
    public long Id { get; set; }
    public int CargaId { get; set; }
    public int ProjectId { get; set; }
    public int Anio { get; set; }
    public int SemanaNum { get; set; }
    public string Trabajador { get; set; } = null!;
    public string? Ocupacion { get; set; }
    public string? PartidaControl { get; set; }
    public decimal HorasLaboradas { get; set; }
    public decimal? CostoHhNormal { get; set; }
    public decimal? Parcial { get; set; }
    /// <summary>Desambigua líneas con la misma (proyecto, año, semana, trabajador, ocupación, partida) repetidas en un mismo archivo.</summary>
    public int Ocurrencia { get; set; } = 1;
    public bool Activo { get; set; } = true;
    public string? MotivoInactivo { get; set; }
    public DateTimeOffset? ActualizadoEn { get; set; }
    public DateTimeOffset CreadoEn { get; set; } = DateTimeOffset.UtcNow;

    public SsHhCarga Carga { get; set; } = null!;
    public Project Proyecto { get; set; } = null!;
}
