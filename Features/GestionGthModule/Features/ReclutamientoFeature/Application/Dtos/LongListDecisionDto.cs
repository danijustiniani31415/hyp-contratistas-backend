namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Decisión del solicitante sobre la long list de un requerimiento (modal "Revisar long list y
    /// CVs"): por cada candidato, si lo aprueba o lo rechaza. El solicitante solo puede enviar la
    /// decisión cuando TODOS los candidatos vigentes de la long list tienen una decisión.
    /// </summary>
    public class LongListDecisionDto
    {
        public List<CandidatoDecisionDto> Decisiones { get; set; } = new();
    }

    /// <summary>Decisión de un candidato: aprobado (true) o rechazado (false).</summary>
    public class CandidatoDecisionDto
    {
        public int CandidatoId { get; set; }

        /// <summary>true = aprobado por el solicitante; false = rechazado.</summary>
        public bool Aprobado { get; set; }
    }

    /// <summary>
    /// Resultado de registrar la decisión de la long list: estado resultante del requerimiento
    /// + conteos. Si el solicitante rechazó a TODOS (<see cref="TodosRechazados"/> = true), el
    /// requerimiento vuelve a LONG_LIST para que GTH envíe una nueva long list; si aprobó al
    /// menos uno, avanza a LONG_LIST_APROBADA.
    /// </summary>
    public class LongListDecisionResultDto
    {
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
        public int Aprobados { get; set; }
        public int Rechazados { get; set; }

        /// <summary>true si el solicitante rechazó a todos los candidatos (0 aprobados).</summary>
        public bool TodosRechazados { get; set; }
    }

    /// <summary>
    /// Contexto que devuelve el repositorio tras registrar la decisión: el resultado (estado + conteos)
    /// más la cabecera del requerimiento y el detalle de la decisión por candidato, para que el servicio
    /// arme el correo que se envía a GTH sin volver a consultar la base de datos.
    /// </summary>
    public class LongListDecisionContextoDto
    {
        public LongListDecisionResultDto Resultado { get; set; } = new();

        // Cabecera del requerimiento (para el asunto/cuerpo del correo).
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>Nombre del solicitante que tomó la decisión (para el cuerpo del correo). Puede ser null.</summary>
        public string? SolicitanteNombre { get; set; }

        /// <summary>Candidatos con la decisión aplicada (nombre + aprobado), en orden de la long list.</summary>
        public List<CandidatoDecididoDto> Candidatos { get; set; } = new();
    }

    /// <summary>Un candidato con la decisión ya aplicada por el solicitante (para el correo a GTH).</summary>
    public class CandidatoDecididoDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Puesto { get; set; }
        public bool Aprobado { get; set; }
    }
}
