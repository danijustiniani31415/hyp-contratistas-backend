namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Detalle de seguimiento de un requerimiento (modal "Estado del reclutamiento"): cabecera con
    /// datos clave + línea de tiempo vertical de las fases del pipeline. Se sirve en una sola petición.
    /// </summary>
    public class SeguimientoDto
    {
        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Puesto solicitado (para el subtítulo del modal).</summary>
        public string Puesto { get; set; } = string.Empty;

        /// <summary>Tipo de requerimiento (Nuevo / Reemplazo).</summary>
        public string TipoRequerimiento { get; set; } = string.Empty;

        /// <summary>
        /// true = ingreso directo <b>FFT</b>. La línea de tiempo de <see cref="Fases"/> ya viene sin
        /// las fases que este flujo no recorre; esto es para que la pantalla pueda decir por qué el
        /// proceso es más corto.
        /// </summary>
        public bool EsFft { get; set; }

        /// <summary>Nombre del candidato FFT que nombró el solicitante. Null cuando no es FFT.</summary>
        public string? FftCandidatoNombre { get; set; }

        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }
        public string? Justificacion { get; set; }

        /// <summary>
        /// Salario bruto mensual declarado para la vacante, en soles. Null en los requerimientos
        /// anteriores a que se pidiera el dato.
        /// </summary>
        public decimal? SalarioBrutoMensual { get; set; }

        /// <summary>Fecha de envío (created) en hora Perú (UTC-5).</summary>
        public DateTime Enviado { get; set; }

        // ── Estado actual ────────────────────────────────────────────────
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
        public int EstadoOrden { get; set; }

        /// <summary>
        /// Aprobación de Gerencia General de la solicitud (primer paso del flujo, obligatorio para
        /// toda vacante). Null en los requerimientos anteriores a esta funcionalidad, que no
        /// pasaron por ese paso.
        /// </summary>
        public AprobacionGgResumenDto? AprobacionGg { get; set; }

        // ── Sustento (adjunto opcional) ──────────────────────────────────
        public string? SustentoNombre { get; set; }
        public string? SustentoUrl { get; set; }

        /// <summary>Fases del pipeline en orden, con su estado (done/current/pending) ya calculado.</summary>
        public List<FaseSeguimientoDto> Fases { get; set; } = new();

        /// <summary>
        /// Candidatos rechazados a lo largo del proceso, con la etapa del rechazo (los que rechazó
        /// el propio solicitante y los que descartó GTH). Incluye los de long lists anteriores, que
        /// es lo que hay que poder consultar cuando se rechazó a todos y el proceso volvió a
        /// empezar por la long list.
        /// </summary>
        public List<CandidatoRechazadoDto> CandidatosRechazados { get; set; } = new();

        /// <summary>
        /// Quién obtuvo el puesto: el candidato que el solicitante aprobó en la decisión final,
        /// con quién y cuándo lo decidió. Null mientras el proceso no se haya cerrado.
        /// </summary>
        public SeleccionadoDto? Seleccionado { get; set; }

        /// <summary>Descripción de la siguiente fase pendiente (null si el requerimiento ya cerró).</summary>
        public string? SiguientePaso { get; set; }
    }

    /// <summary>Una fase del pipeline dentro del seguimiento vertical.</summary>
    public class FaseSeguimientoDto
    {
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }

        /// <summary>Estado visual de la fase respecto a la fase actual del requerimiento: "done" | "current" | "pending".</summary>
        public string Estado { get; set; } = "pending";
    }
}
