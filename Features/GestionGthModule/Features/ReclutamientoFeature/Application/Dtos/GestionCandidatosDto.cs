namespace Abril_Backend.Features.GestionGthModule.Features.ReclutamientoFeature.Application.Dtos
{
    /// <summary>
    /// Panel de la vista del solicitante ("Solicitud de Personal"), en una sola petición:
    /// las tarjetas resumen, las tarjetas de "Gestión de candidatos" (long lists que GTH ya le
    /// envió para revisar) y la tabla "Mis solicitudes de vacante".
    /// </summary>
    public class SolicitantePanelDto
    {
        /// <summary>Contadores de las tarjetas resumen de la cabecera.</summary>
        public ResumenSolicitantePanelDto Resumen { get; set; } = new();

        /// <summary>Tarjetas "Long list enviada por GTH" pendientes de revisión del solicitante.</summary>
        public List<GestionCandidatoCardDto> GestionCandidatos { get; set; } = new();

        /// <summary>Filas de la tabla de solicitudes de vacante del área.</summary>
        public List<SolicitudVacanteListItemDto> MisSolicitudes { get; set; } = new();

        /// <summary>
        /// ¿El usuario puede mover estos requerimientos (registrar una solicitud nueva, decidir la
        /// long list, decidir al finalista, reenviar la aprobación)? Solo la jefatura del área:
        /// JEFE, GERENTE y GERENTE GENERAL. El resto entra a hacer seguimiento y nada más, así que
        /// el frontend le esconde los botones — el backend igual los rechaza.
        /// </summary>
        public bool PuedeGestionar { get; set; }
    }

    /// <summary>
    /// Tarjetas resumen del panel del solicitante: el embudo de sus requerimientos, desde el total
    /// registrado hasta los procesos ya cerrados.
    /// </summary>
    public class ResumenSolicitantePanelDto
    {
        /// <summary>"Mis solicitudes · Total registradas": todos sus requerimientos vigentes.</summary>
        public int TotalRegistradas { get; set; }

        /// <summary>"Pendientes · Sin respuesta": siguen en la fase inicial, GTH todavía no los tomó.</summary>
        public int Pendientes { get; set; }

        /// <summary>"En revisión · GTH evaluando": el siguiente paso le toca a GTH (sin contar el inicial).</summary>
        public int EnRevisionGth { get; set; }

        /// <summary>"Aprobadas · Este período": procesos cerrados (finalista aprobado) del año en curso.</summary>
        public int Aprobadas { get; set; }
    }

    /// <summary>
    /// Tarjeta de "Gestión de candidatos": un requerimiento del solicitante sobre el que GTH le
    /// dejó algo por revisar. Hay dos tipos, distinguidos por <see cref="Tipo"/>:
    /// la long list enviada (LONG_LIST) y el informe de finalistas (FINALISTAS).
    /// </summary>
    public class GestionCandidatoCardDto
    {
        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }

        /// <summary>Cantidad de candidatos de la tarjeta (long list cargada o finalistas evaluados).</summary>
        public int TotalCandidatos { get; set; }

        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>
        /// Qué le toca revisar al solicitante: <c>LONG_LIST</c> (CVs, con decisión de aprobar o
        /// rechazar) o <c>FINALISTAS</c> (informe de entrevistas de GTH, solo lectura).
        /// </summary>
        public string Tipo { get; set; } = TipoGestionCandidato.LongList;
    }

    /// <summary>Tipos de tarjeta de "Gestión de candidatos" (valores estables usados por el frontend).</summary>
    public static class TipoGestionCandidato
    {
        public const string LongList   = "LONG_LIST";
        public const string Finalistas = "FINALISTAS";
    }

    /// <summary>
    /// Revisión de la long list de un requerimiento (modal del solicitante): cabecera del
    /// requerimiento + candidatos con sus datos y CV, en una sola petición.
    /// </summary>
    public class RevisionLongListDto
    {
        public int RequerimientoId { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Puesto { get; set; } = string.Empty;
        public string? Area { get; set; }
        public string? ProyectoObra { get; set; }
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;

        /// <summary>Candidatos cargados por GTH en la long list, en orden.</summary>
        public List<CandidatoRevisionDto> Candidatos { get; set; } = new();
    }

    /// <summary>Un candidato de la long list como lo ve el solicitante en la revisión.</summary>
    public class CandidatoRevisionDto
    {
        public int CandidatoId { get; set; }
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Puesto del requerimiento (snapshot), no un dato capturado por candidato.</summary>
        public string? Puesto { get; set; }

        public string? Comentario { get; set; }

        /// <summary>Nombre y link del CV en SharePoint (para "Ver CV completo").</summary>
        public string? CvNombre { get; set; }
        public string? CvUrl { get; set; }

        /// <summary>
        /// Portafolio/anexos que GTH adjuntó al candidato además del CV, en orden de carga. Vacío
        /// si no cargó ninguno (son opcionales).
        /// </summary>
        public List<CandidatoAnexoDto> Anexos { get; set; } = new();

        /// <summary>Estado de revisión (PENDIENTE / APROBADO / RECHAZADO).</summary>
        public string EstadoCodigo { get; set; } = string.Empty;
        public string EstadoNombre { get; set; } = string.Empty;
    }

    /// <summary>Un archivo del "Portafolio/Anexos" de un candidato, como lo ve el solicitante.</summary>
    public class CandidatoAnexoDto
    {
        public int AnexoId { get; set; }

        /// <summary>Nombre con el que GTH lo subió (el de SharePoint si no se guardó el original).</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Link al archivo en SharePoint. Null si la subida no dejó url.</summary>
        public string? Url { get; set; }
    }
}
