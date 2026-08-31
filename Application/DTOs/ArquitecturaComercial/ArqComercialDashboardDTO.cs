namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class ArqComercialKpiDTO
    {
        public int TotalActividades { get; set; }
        public int Culminadas { get; set; }
        public int EnProceso { get; set; }
        public int Vencidas { get; set; }
        public int Pendientes { get; set; }
        public double EficienciaMedia { get; set; }
        public double ProgresoGlobal { get; set; }
    }

    public class ArqComercialAlertDTO
    {
        public int VencidasSinCerrar { get; set; }
        public int VencenEstaSemana { get; set; }
        public int ArrancanEstaSemana { get; set; }
        public int HitosProximos14Dias { get; set; }
    }

    public class ArqComercialChartItemDTO
    {
        public string Label { get; set; } = string.Empty;
        public double Value { get; set; }
    }

    public class ProyeccionAvanceDTO
    {
        public List<string> Labels { get; set; } = new();
        public List<double> Programado { get; set; } = new();
        public List<double> Real { get; set; } = new();
        public List<double> Proyeccion { get; set; } = new();
    }

    public class EficienciaSemanalDTO
    {
        public string Semana { get; set; } = string.Empty;
        public double Valor { get; set; }
    }

    public class SupervisorProgresoDTO
    {
        public int UserId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public double Progreso { get; set; }
        public int Total { get; set; }
        public int Completadas { get; set; }
        /// <summary>Actividades vencidas de semanas anteriores (no de la semana en control) que
        /// el supervisor sigue arrastrando sin cerrar. Informativo — no afecta el IES.</summary>
        public int DeudaAnterior { get; set; }
        /// <summary>true si no tenía nada que vencer ni arrancar esta semana — se muestra aparte,
        /// no participa del IES ni del promedio del equipo.</summary>
        public bool SinCompromisos { get; set; }
        /// <summary>Componente SPI del IES (35% del puntaje): ritmo real vs. planificado de lo que debía cerrar.</summary>
        public double CompSpi { get; set; }
        /// <summary>Componente tasa de cierre del IES (35%): % de lo que debía cerrar esta semana y cerró.</summary>
        public double CompCierre { get; set; }
        /// <summary>Componente puntualidad de inicio del IES (20%): % de lo que debía arrancar y arrancó a tiempo.</summary>
        public double CompInicio { get; set; }
        /// <summary>Motivos concretos por los que el IES no llegó a 100% (para mostrar en el desglose del ranking).</summary>
        public List<string> Motivos { get; set; } = new();
    }

    /// <summary>Histórico completo de un supervisor (todo el tiempo, no solo la semana en control) —
    /// para el modal "Eficiencia a lo largo del tiempo" del ranking.</summary>
    public class SupervisorHistoricoDTO
    {
        public string Nombre { get; set; } = string.Empty;
        public int TotalActividades { get; set; }
        public int Culminadas { get; set; }
        public int EnProceso { get; set; }
        public int Vencidas { get; set; }
        public int Pendientes { get; set; }
        /// <summary>Culminadas ÷ total, histórico completo (el indicador "de siempre", antes de
        /// restringir el ranking a la semana en control).</summary>
        public double EficienciaHistorica { get; set; }
        /// <summary>SPI promedio histórico de sus actividades con SPI válido.</summary>
        public double SpiPromedio { get; set; }
        /// <summary>Tasa de cierre semanal (culminadas ÷ vencían esa semana) de las últimas 8 semanas —
        /// para ver si viene mejorando o empeorando.</summary>
        public List<EficienciaSemanalDTO> TendenciaSemanal { get; set; } = new();
    }

    public class HitoCriticoDTO
    {
        public int    Id            { get; set; }
        public string Nombre        { get; set; } = string.Empty;
        public string Proyecto      { get; set; } = string.Empty;
        public string FechaLimite   { get; set; } = string.Empty;
        public int    DiasRestantes { get; set; }
        public string Estado        { get; set; } = string.Empty;
        public int    Semana        { get; set; }
    }

    public class SemanaDashboardDTO
    {
        public int    Numero { get; set; }
        public string Label  { get; set; } = string.Empty;
        public string Inicio { get; set; } = string.Empty;
        public string Fin    { get; set; } = string.Empty;
    }

    public class CategoriaDashboardItemDTO
    {
        public int    Id         { get; set; }
        public string Nombre     { get; set; } = string.Empty;
        public int    Total      { get; set; }
        public int    Culminadas { get; set; }
        public int    EnProceso  { get; set; }
        public int    Vencidas   { get; set; }
        public int    Pendientes { get; set; }
        public double Progreso   { get; set; }
    }

    public class ArqComercialDashboardDTO
    {
        public ArqComercialKpiDTO Kpis { get; set; } = new();
        public ArqComercialAlertDTO Alertas { get; set; } = new();
        public ProyeccionAvanceDTO ProyeccionAvance { get; set; } = new();
        public List<ArqComercialChartItemDTO> RankingEficiencia { get; set; } = new();
        public List<ArqComercialChartItemDTO> DistribucionEstado { get; set; } = new();
        public List<EficienciaSemanalDTO> TendenciaEficiencia { get; set; } = new();
        public List<SupervisorProgresoDTO> Supervisores { get; set; } = new();
        public List<HitoCriticoDTO> HitosCriticos { get; set; } = new();
        public TareasPorArquitectoDTO[]       TareasPorArquitectoDetalle  { get; set; } = [];
        public AvanceSemanalDTO[]             AvanceSemanal               { get; set; } = [];
        public EficienciaSpiDTO[]             EficienciaSpi               { get; set; } = [];
        public CategoriaItemDTO[]             Categorias                  { get; set; } = [];
        public List<CategoriaDashboardItemDTO> DistribucionPorCategoria   { get; set; } = [];
        public List<ArqComercialChartItemDTO> DistribucionTipos           { get; set; } = [];
        public SemanaDashboardDTO             SemanaActual                { get; set; } = new();
        public string                          RangoUltimasSemanas        { get; set; } = string.Empty;
    }

    public class ArqComercialFilterOptionDTO
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }

    public class ArqComercialProjectOptionDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class ArqComercialFiltersDTO
    {
        public List<ArqComercialFilterOptionDTO> Semanas { get; set; } = new();
        public List<ArqComercialFilterOptionDTO> Meses { get; set; } = new();
        public List<ArqComercialProjectOptionDTO> Proyectos { get; set; } = new();
    }
}
