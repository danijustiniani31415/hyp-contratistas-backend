namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class TareoEnrolamientoRequestDTO
    {
        public string FotoBase64 { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = [];
    }

    public class TareoEnrolamientoEstadoDTO
    {
        public bool Enrolado { get; set; }
        public DateTime? FechaEnrolamiento { get; set; }
    }

    /// <summary>Fila de la pantalla de "Gestión de permisos" del coordinador: un trabajador de
    /// Arquitectura Comercial con su estado de enrolamiento facial.</summary>
    public class TareoTrabajadorEnrolamientoDTO
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Enrolado { get; set; }
        public DateTime? FechaEnrolamiento { get; set; }
        /// <summary>SSO-FO-150 firmado y subido — sin esto el enrolamiento queda bloqueado.</summary>
        public bool AutorizacionSubida { get; set; }
        public DateTime? AutorizacionSubidaEn { get; set; }
        /// <summary>URL del escaneo firmado ya subido, para verlo/descargarlo a solicitud. Solo se
        /// popula para el listado del coordinador (GetTrabajadoresParaEnrolar) — el listado de
        /// autoservicio (GetTrabajadoresDisponiblesParaEnrolar) la deja null a propósito, para no
        /// exponer el documento firmado de terceros a la cuenta compartida de campo.</summary>
        public string? AutorizacionUrl { get; set; }
    }

    /// <summary>Datos para imprimir el SSO-FO-150 con nombre/DNI del trabajador ya precargados.</summary>
    public class TareoAutorizacionDetalleDTO
    {
        public int WorkerId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Dni { get; set; }
    }

    /// <summary>Geolocalización de un proyecto para el geofencing de Marcar Tareo — editable
    /// directo desde Gestión de Permisos, sin salir del módulo de Tareo.</summary>
    public class TareoProyectoGeoDTO
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = string.Empty;
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public decimal RadioGeofenceMetros { get; set; }
    }

    public class TareoProyectoGeoUpdateDTO
    {
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public decimal? RadioGeofenceMetros { get; set; }
    }

    /// <summary>Resultado de la identificación 1:N (buscar, entre los enrolados de Arquitectura
    /// Comercial, quién es la persona frente a la cámara según su embedding).</summary>
    public class TareoIdentificacionDTO
    {
        public bool Identificado { get; set; }
        public int? WorkerId { get; set; }
        public string? Nombre { get; set; }
    }

    public class TareoIdentificarRequestDTO
    {
        public float[]? Embedding { get; set; }
    }

    public class TareoMarcarRequestDTO
    {
        /// <summary>INICIO_JORNADA | INICIO_ALMUERZO | RETORNO | FIN_JORNADA</summary>
        public string Tipo { get; set; } = string.Empty;
        public string FotoBase64 { get; set; } = string.Empty;
        /// <summary>Embedding facial (128 floats) calculado en el cliente sobre la foto recién tomada.
        /// La similitud contra el embedding enrolado SIEMPRE se calcula en el servidor — nunca se
        /// confía en un score que mande el cliente, porque sería trivial de falsear llamando al
        /// endpoint directamente (sin pasar por la UI).</summary>
        public float[]? Embedding { get; set; }
        public DateTime? HoraDispositivo { get; set; }
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public decimal? PrecisionMetros { get; set; }
    }

    public class TareoRegistroDTO
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public DateTime HoraServidor { get; set; }
        public string FotoUrl { get; set; } = string.Empty;
        public decimal? Lat { get; set; }
        public decimal? Lng { get; set; }
        public int? ProjectId { get; set; }
        public string? ProjectNombre { get; set; }
        public decimal? DistanciaMetros { get; set; }
        public decimal? FaceMatchScore { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRevision { get; set; }
        public bool YaExistia { get; set; }
    }

    public class TareoMiTareoHoyDTO
    {
        public TareoRegistroDTO? InicioJornada { get; set; }
        public TareoRegistroDTO? InicioAlmuerzo { get; set; }
        public TareoRegistroDTO? Retorno { get; set; }
        public TareoRegistroDTO? FinJornada { get; set; }
    }

    public class TareoRegistroListaDTO
    {
        public int Id { get; set; }
        public int WorkerId { get; set; }
        public string WorkerNombre { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateOnly Fecha { get; set; }
        public DateTime HoraServidor { get; set; }
        public string FotoUrl { get; set; } = string.Empty;
        public string? ProjectNombre { get; set; }
        public decimal? DistanciaMetros { get; set; }
        public decimal? FaceMatchScore { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? MotivoRevision { get; set; }
    }

    public class TareoRegistroListResponseDTO
    {
        public List<TareoRegistroListaDTO> Items { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int PorPagina { get; set; }
    }

    public class TareoFiltroDTO
    {
        public int? WorkerId { get; set; }
        public int? ProyectoId { get; set; }
        public DateOnly? Desde { get; set; }
        public DateOnly? Hasta { get; set; }
        public string? Estado { get; set; }
        public int Pagina { get; set; } = 1;
        public int PorPagina { get; set; } = 50;
    }

    public class TareoRevisarRequestDTO
    {
        public bool Aprobar { get; set; }
        public string? Comentario { get; set; }
    }

    public class TareoReporteDiaDTO
    {
        public DateOnly Fecha { get; set; }
        public DateTime? InicioJornada { get; set; }
        public DateTime? InicioAlmuerzo { get; set; }
        public DateTime? Retorno { get; set; }
        public DateTime? FinJornada { get; set; }
        public decimal? TotalHoras { get; set; }
    }

    public class TareoReporteSemanalDTO
    {
        public int WorkerId { get; set; }
        public string WorkerNombre { get; set; } = string.Empty;
        public List<TareoReporteDiaDTO> Dias { get; set; } = new();
        public decimal TotalHorasSemana { get; set; }
    }
}
