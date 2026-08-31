namespace Abril_Backend.Features.Evaluaciones.Application.Dtos
{
    // ─── CREATE ────────────────────────────────────────────────────────────────
    public class EvContratistaEvaluacionCreateDto
    {
        public int ProyectoId { get; set; }
        public int ContributorId { get; set; }
        public string? Comentario { get; set; }
        public List<EvContratistaDetalleCreateDto> Detalles { get; set; } = [];
    }

    public class EvContratistaDetalleCreateDto
    {
        public int? PlantillaId { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int? Puntaje { get; set; }
        public bool EsNa { get; set; } = false;
    }

    public class EvContratistaNoAplicaCreateDto
    {
        public string Motivo { get; set; } = string.Empty;
        // Si se indican, marca "no aplica" solo para esa empresa/proyecto puntual.
        // Si se dejan null, marca que no corresponde evaluar a ningun contratista este periodo.
        public int? ProyectoId { get; set; }
        public int? ContributorId { get; set; }
    }

    // ─── INICIO (pantalla evaluar) ──────────────────────────────────────────────
    public class EvContratistaInicioDto
    {
        public EvPeriodoDto? Periodo { get; set; }
        public string? MiAreaNombre { get; set; }
        public string? MiPuestoEvaluador { get; set; }
        public List<EvContratistaCriterioDto> Plantilla { get; set; } = [];
        public List<EvContratistaAEvaluarDto> ContratistasAEvaluar { get; set; } = [];
        public bool PuedeVerTodos { get; set; }
        public bool YaMarcoNoAplica { get; set; }
    }

    public class EvContratistaCriterioDto
    {
        public int Id { get; set; }
        public string Criterio { get; set; } = string.Empty;
        public int Orden { get; set; }
    }

    public class EvContratistaAEvaluarDto
    {
        public int ContributorId { get; set; }
        public string ContributorNombre { get; set; } = string.Empty;
        public string ContributorRuc { get; set; } = string.Empty;
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public int DiasLaborados { get; set; }
        public bool YaEvalue { get; set; }
        public decimal? NotaPrevia { get; set; }
        public bool NoAplica { get; set; }
        public string? NoAplicaMotivo { get; set; }
    }

    // ─── VER EVALUACIONES (lista consolidada) ──────────────────────────────────
    public class EvContratistaVerInicioDto
    {
        public List<EvPeriodoDto> Periodos { get; set; } = [];
        public List<EvContratistaProyectoFiltroDto> Proyectos { get; set; } = [];
        public List<EvContratistaResumenDto> Evaluaciones { get; set; } = [];
    }

    public class EvContratistaProyectoFiltroDto
    {
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
    }

    public class EvContratistaResumenDto
    {
        public int ContributorId { get; set; }
        public string ContributorNombre { get; set; } = string.Empty;
        public string ContributorRuc { get; set; } = string.Empty;
        public int ProyectoId { get; set; }
        public string ProyectoNombre { get; set; } = string.Empty;
        public decimal? NotaOT { get; set; }
        public decimal? NotaSsoma { get; set; }
        public decimal? NotaResidencia { get; set; }
        public decimal? NotaCalidad { get; set; }
        public decimal? NotaProduccion { get; set; }
        public decimal? NotaAdministracion { get; set; }
        public decimal? NotaTotal { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    // ─── DASHBOARD EJECUTIVO ────────────────────────────────────────────────────
    public class EvContratistaDashboardDto
    {
        public int TotalContratistas { get; set; }
        public int Aprobados { get; set; }
        public int Regulares { get; set; }
        public int Desaprobados { get; set; }
        public decimal? PromedioGeneral { get; set; }
        public List<EvContratistaResumenDto> Contratistas { get; set; } = [];
        public List<EvContratistaAreaPromedioDto> PromediosPorArea { get; set; } = [];
        public List<EvContratistaTendenciaDto> Tendencia { get; set; } = [];
    }

    public class EvContratistaAreaPromedioDto
    {
        public string AreaNombre { get; set; } = string.Empty;
        public decimal? Promedio { get; set; }
        public int TotalEvaluaciones { get; set; }
    }

    public class EvContratistaTendenciaDto
    {
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string NombreMes { get; set; } = string.Empty;
        public int ContributorId { get; set; }
        public string ContributorNombre { get; set; } = string.Empty;
        public decimal? NotaTotal { get; set; }
    }

    // ─── FILTROS ────────────────────────────────────────────────────────────────
    public class EvContratistaFiltroDto
    {
        public int? PeriodoId { get; set; }
        public int? ProyectoId { get; set; }
    }

    // ─── ENVÍO DE RESULTADOS A GERENTES DE CONTRATISTA ─────────────────────────
    public class EmpresaResultadoEnvioDto
    {
        public int ContributorId { get; set; }
        public string ContributorNombre { get; set; } = string.Empty;
        public string? EmailAdministrador { get; set; }
        public List<EvContratistaResumenDto> Evaluaciones { get; set; } = [];
        public List<SupervisorResultadoDto> Supervisores { get; set; } = [];
    }

    public class SupervisorResultadoDto
    {
        public string SupervisorNombre { get; set; } = string.Empty;
        public string ProyectoNombre { get; set; } = string.Empty;
        public decimal? Nota { get; set; }
    }

    public class EvContratistaEnvioResultadoDto
    {
        public int Enviados { get; set; }
        public List<string> OmitidosSinEmail { get; set; } = [];
    }
}
