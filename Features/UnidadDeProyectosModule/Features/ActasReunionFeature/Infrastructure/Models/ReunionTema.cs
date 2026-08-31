namespace Abril_Backend.Features.UnidadDeProyectosModule.Features.ActasReunionFeature.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de temas predefinidos para agendar reuniones (ej. Reunión de Jefatura de Proyectos).
    /// El tema personalizado escrito a mano no se registra aquí: solo queda en reunion.tema.
    /// </summary>
    public class ReunionTema
    {
        public int ReunionTemaId { get; set; }
        public string Descripcion { get; set; } = null!;

        /// <summary>
        /// Área/gerencia por defecto de la convocatoria recurrente de este tema (ej. "Reunión de
        /// Jefaturas de Proyectos" siempre convoca a Gerencia de Proyectos). Null = sin convocatoria
        /// asociada; se combina con los puestos de ReunionTemaPuesto igual que en la convocatoria
        /// masiva manual.
        /// </summary>
        public int? AreaScopeId { get; set; }

        /// <summary>Si este tema exige que los convocados carguen una agenda antes de la reunión.</summary>
        public bool RequiereAgenda { get; set; }
        /// <summary>
        /// True = agenda fija (siempre el mismo <see cref="AgendaTexto"/>, se edita una sola vez acá).
        /// False = agenda dinámica (cada participante propone sus temas en cada ocurrencia, ver
        /// ReunionAgendaItem). Solo aplica si RequiereAgenda es true.
        /// </summary>
        public bool AgendaFija { get; set; }
        public string? AgendaTexto { get; set; }
        /// <summary>
        /// Horas de anticipación (admite decimales) para recordar a los convocados que carguen su
        /// agenda. Solo aplica si RequiereAgenda es true y AgendaFija es false.
        /// </summary>
        public decimal? RecordatorioHorasAntes { get; set; }

        // ── Recurrencia (generación automática de la siguiente reunión) ─────────
        /// <summary>Si true, un job periódico genera automáticamente las siguientes ocurrencias
        /// según <see cref="IntervaloDias"/> desde <see cref="FechaAncla"/>. Requiere AreaScopeId
        /// configurado (el ámbito de las reuniones generadas).</summary>
        public bool EsRecurrente { get; set; }
        /// <summary>Distinto del soft-delete: permite pausar la generación sin perder la config.</summary>
        public bool RecurrenciaActiva { get; set; } = true;
        public int? IntervaloDias { get; set; }
        /// <summary>Fecha teórica de la primera ocurrencia de la serie. Fija: reprogramar o cancelar
        /// una ocurrencia generada NO la mueve.</summary>
        public DateOnly? FechaAncla { get; set; }
        public TimeOnly? HoraInicio { get; set; }
        public TimeOnly? HoraFin { get; set; }
        public string? Lugar { get; set; }
        /// <summary>Cuántos días antes de la fecha teórica se genera la reunión (para que la
        /// convocatoria/agenda le llegue a tiempo a los convocados).</summary>
        public int DiasAnticipacion { get; set; } = 5;
        /// <summary>Puntero de calendario: última fecha TEÓRICA (no la real/reprogramada) para la
        /// que ya se generó una reunión. Null = aún no se generó ninguna.</summary>
        public DateOnly? UltimaFechaGenerada { get; set; }
        /// <summary>Última reunión generada por esta serie, para encadenar ReunionAnteriorId.</summary>
        public int? UltimaReunionGeneradaId { get; set; }

        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
        public DateTime? UpdatedDateTime { get; set; }
        public int? UpdatedUserId { get; set; }
        public bool Active { get; set; }
        public bool State { get; set; }
    }
}
