using Abril_Backend.Application.DTOs;

namespace Abril_Backend.Features.VecinosModule.Features.GestionVecinosFeature.Application.Dtos
{
    /// <summary>Opción genérica de catálogo (id + descripción).</summary>
    public class CatalogOptionDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = null!;
    }

    public class ProjectOptionDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
    }

    /// <summary>Opciones para filtros y para el formulario de creación.</summary>
    public class VecinoFormOptionsDto
    {
        public List<ProjectOptionDto> Projects { get; set; } = new();
        public List<CatalogOptionDto> Colindancias { get; set; } = new();
        public List<CatalogOptionDto> TiposConstruccion { get; set; } = new();
        public List<CatalogOptionDto> Usos { get; set; } = new();
        public List<CatalogOptionDto> RelacionTipos { get; set; } = new();
    }

    /// <summary>Una imagen del estado de la propiedad.</summary>
    public class VecinoImagenDto
    {
        public int VecinoImagenId { get; set; }
        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
    }

    /// <summary>Una persona asociada a una casa/lote.</summary>
    public class VecinoPersonaDto
    {
        public int VecinoPersonaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Dni { get; set; }
        public string? Celular { get; set; }
        public int VecinoRelacionTipoId { get; set; }
        public string RelacionDescripcion { get; set; } = null!;
    }

    /// <summary>Fila de la lista/tarjeta de vecinos (contiene todo lo necesario para el detalle).</summary>
    public class VecinoListItemDto
    {
        public int VecinoId { get; set; }
        /// <summary>Lote/edificio al que pertenece este vecino/departamento.</summary>
        public int VecinoLoteId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public string? Predio { get; set; }
        public int? VecinoUsoId { get; set; }
        public string? UsoDescripcion { get; set; }
        /// <summary>Dirección del lote (puede ser nula si el lote aún no tiene dirección registrada).</summary>
        public string? Direccion { get; set; }
        public string? InteriorDepartamento { get; set; }
        /// <summary>Nombre de la persona principal (propietario) de la casa, para mostrar en la tabla/tarjeta.</summary>
        public string? NombrePropietario { get; set; }
        /// <summary>DNI de la persona principal (propietario), si tiene.</summary>
        public string? Dni { get; set; }
        /// <summary>Celular de la persona principal (propietario), si tiene.</summary>
        public string? Celular { get; set; }
        /// <summary>Todas las personas asociadas a la casa.</summary>
        public List<VecinoPersonaDto> Personas { get; set; } = new();
        public int VecinoColindanciaId { get; set; }
        public string ColindanciaDescripcion { get; set; } = null!;
        public int VecinoTipoConstruccionId { get; set; }
        public string TipoConstruccionDescripcion { get; set; } = null!;
        public string? Observaciones { get; set; }
        /// <summary>Imágenes del estado de la propiedad.</summary>
        public List<VecinoImagenDto> Imagenes { get; set; } = new();
        public DateTime CreatedDateTime { get; set; }
        public int SolicitudesCount { get; set; }
        public int CompromisosCount { get; set; }
        /// <summary>Solicitudes aprobadas (Aceptada) del vecino.</summary>
        public int SolicitudesAprobadas { get; set; }
        /// <summary>Solicitudes evaluables del vecino (Aceptada + Por responder, sin Denegada).</summary>
        public int SolicitudesEvaluables { get; set; }
        /// <summary>Entregables aprobados del vecino.</summary>
        public int EntregablesAprobados { get; set; }
        /// <summary>Entregables evaluables del vecino (Falta + Enviado + Aprobado, sin "No aplica").</summary>
        public int EntregablesEvaluables { get; set; }
        /// <summary>Requisitos subidos del vecino.</summary>
        public int RequisitosSubidos { get; set; }
        /// <summary>Requisitos evaluables del vecino (Subido + No subido, sin "No aplica").</summary>
        public int RequisitosEvaluables { get; set; }
    }

    /// <summary>Respuesta del load inicial: opciones de filtros/formulario + primera página.</summary>
    public class VecinosPageDto
    {
        public VecinoFormOptionsDto Options { get; set; } = new();
        public PagedResult<VecinoListItemDto> Vecinos { get; set; } = new();
    }

    public class VecinoFilterDto
    {
        public int Page { get; set; } = 1;
        public int? ProjectId { get; set; }
        public int? VecinoColindanciaId { get; set; }
        public string? Search { get; set; }
    }

    /// <summary>Una persona del formulario de alta de vecino (DNI opcional).</summary>
    public class VecinoPersonaCreateDto
    {
        public string Nombre { get; set; } = null!;
        public string? Dni { get; set; }
        public string? Celular { get; set; }
        public int VecinoRelacionTipoId { get; set; }
    }

    /// <summary>Persona dentro del formulario de edición (id presente = existente; null = nueva).</summary>
    public class VecinoPersonaUpsertDto
    {
        public int? VecinoPersonaId { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Dni { get; set; }
        public string? Celular { get; set; }
        public int VecinoRelacionTipoId { get; set; }
    }

    /// <summary>Edición de los datos de un vecino/departamento (sección Detalle) + sus personas.
    /// La dirección y observaciones se editan aparte (a nivel de lote, ver <see cref="VecinoLoteUpdateDto"/>).</summary>
    public class VecinoUpdateDto
    {
        public int VecinoUsoId { get; set; }
        public string? InteriorDepartamento { get; set; }
        public int VecinoColindanciaId { get; set; }
        public int VecinoTipoConstruccionId { get; set; }
        public List<VecinoPersonaUpsertDto> Personas { get; set; } = new();
    }

    /// <summary>Edición de los datos a nivel de lote (dirección + observaciones).</summary>
    public class VecinoLoteUpdateDto
    {
        public string Direccion { get; set; } = null!;
        public string? Observaciones { get; set; }
    }

    /// <summary>Un vecino/departamento del formulario de alta (interior + uso/colindancia/tipo + personas).</summary>
    public class VecinoDepartamentoCreateDto
    {
        public int VecinoUsoId { get; set; }
        public string? InteriorDepartamento { get; set; }
        public int VecinoColindanciaId { get; set; }
        public int VecinoTipoConstruccionId { get; set; }
        /// <summary>Personas del departamento (al menos una).</summary>
        public List<VecinoPersonaCreateDto> Personas { get; set; } = new();
    }

    /// <summary>
    /// Alta de vecinos sobre un lote existente (definido como polígono en el croquis).
    /// Fija/actualiza la dirección y observaciones del lote y crea N vecinos/departamentos.
    /// </summary>
    public class VecinoLoteRegisterDto
    {
        /// <summary>Polígono del croquis que representa el lote donde se registran los vecinos.</summary>
        public int ProjectCroquisLoteId { get; set; }
        public string Direccion { get; set; } = null!;
        public string? Observaciones { get; set; }
        /// <summary>Vecinos/departamentos a crear en el lote (al menos uno).</summary>
        public List<VecinoDepartamentoCreateDto> Vecinos { get; set; } = new();
    }

    /// <summary>Resultado del alta: el lote afectado y los vecinos creados (en orden).</summary>
    public class VecinoLoteRegisterResultDto
    {
        public int VecinoLoteId { get; set; }
        public List<int> VecinoIds { get; set; } = new();
    }

    // ── Solicitudes ─────────────────────────────────────────────────────────
    public class VecinoSolicitudItemDto
    {
        public int VecinoSolicitudId { get; set; }
        public int VecinoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool EsCritica { get; set; }
        public int VecinoSolicitudEstadoId { get; set; }
        public string EstadoDescripcion { get; set; } = null!;
        public DateTime CreatedDateTime { get; set; }
    }

    /// <summary>Respuesta al abrir el detalle: solicitudes + catálogos de solicitud, compromiso y entregables.</summary>
    public class VecinoSolicitudesResponseDto
    {
        public List<VecinoSolicitudItemDto> Solicitudes { get; set; } = new();
        public List<CatalogOptionDto> Estados { get; set; } = new();
        public List<CatalogOptionDto> CompromisoEstados { get; set; } = new();
        public List<CatalogOptionDto> EntregableEstados { get; set; } = new();
    }

    public class VecinoSolicitudCreateDto
    {
        public string Descripcion { get; set; } = null!;
        public bool EsCritica { get; set; }
    }

    public class VecinoSolicitudEstadoUpdateDto
    {
        public int VecinoSolicitudEstadoId { get; set; }
    }

    /// <summary>Edición del texto de una solicitud ya registrada.</summary>
    public class VecinoSolicitudDescripcionUpdateDto
    {
        public string Descripcion { get; set; } = null!;
    }

    // ── Compromisos ─────────────────────────────────────────────────────────
    public class VecinoEntregableItemDto
    {
        public int VecinoCompromisoEntregableId { get; set; }
        public int VecinoEntregableTipoId { get; set; }
        public string TipoDescripcion { get; set; } = null!;
        public int Orden { get; set; }
        public int VecinoEntregableEstadoId { get; set; }
        public string EstadoDescripcion { get; set; } = null!;
        public string? ArchivoUrl { get; set; }
        public string? OriginalFileName { get; set; }
    }

    /// <summary>Un archivo de "normativas" de un compromiso (sección multi-archivo).</summary>
    public class VecinoNormativaDto
    {
        public int VecinoCompromisoNormativaId { get; set; }
        public string ArchivoUrl { get; set; } = null!;
        public string? OriginalFileName { get; set; }
    }

    public class VecinoCompromisoItemDto
    {
        public int VecinoCompromisoId { get; set; }
        public int VecinoSolicitudId { get; set; }
        public string Descripcion { get; set; } = null!;
        public bool EsCritico { get; set; }
        public int VecinoCompromisoEstadoId { get; set; }
        public string EstadoDescripcion { get; set; } = null!;
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        /// <summary>Fecha límite por municipalidad/fiscalización (si la tiene, el compromiso está priorizado).</summary>
        public DateOnly? FechaFinMunicipalidad { get; set; }
        public string? Observaciones { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public List<VecinoEntregableItemDto> Entregables { get; set; } = new();
        public List<VecinoNormativaDto> Normativas { get; set; } = new();
    }

    public class VecinoCompromisoCreateDto
    {
        public string Descripcion { get; set; } = null!;
        public bool EsCritico { get; set; }
        public int? VecinoCompromisoEstadoId { get; set; }
        public DateOnly? FechaInicio { get; set; }
        public DateOnly? FechaFin { get; set; }
        public DateOnly? FechaFinMunicipalidad { get; set; }
        public string? Observaciones { get; set; }
    }

    public class VecinoCompromisoEstadoUpdateDto
    {
        public int VecinoCompromisoEstadoId { get; set; }
    }

    /// <summary>Cuerpo para fijar/limpiar la fecha límite por municipalidad/fiscalización de un compromiso.</summary>
    public class VecinoCompromisoFechaMunicipalidadUpdateDto
    {
        public DateOnly? FechaFinMunicipalidad { get; set; }
    }

    public class VecinoCompromisoObservacionesUpdateDto
    {
        public string? Observaciones { get; set; }
    }

    public class VecinoEntregableEstadoUpdateDto
    {
        public int VecinoEntregableEstadoId { get; set; }
    }

    // ── Requisitos (Gestión de requisitos) ────────────────────────────────────
    /// <summary>Un requisito del vecino: tipo, estado y archivo (si tiene).</summary>
    public class VecinoRequisitoItemDto
    {
        /// <summary>Id del registro (null si aún no existe fila para ese tipo).</summary>
        public int? VecinoRequisitoId { get; set; }
        public int VecinoRequisitoTipoId { get; set; }
        public string TipoDescripcion { get; set; } = null!;
        public int Orden { get; set; }
        public int VecinoRequisitoEstadoId { get; set; }
        public string EstadoDescripcion { get; set; } = null!;
        public string? ArchivoUrl { get; set; }
        public string? OriginalFileName { get; set; }
    }

    public class VecinoRequisitosResponseDto
    {
        public List<VecinoRequisitoItemDto> Requisitos { get; set; } = new();
        public List<CatalogOptionDto> Estados { get; set; } = new();
    }

    public class VecinoRequisitoEstadoUpdateDto
    {
        public int VecinoRequisitoEstadoId { get; set; }
    }

    public class VecinoRequisitoNoAplicaDto
    {
        public bool NoAplica { get; set; }
    }

    // ── Calendario de limpiezas ────────────────────────────────────────────────
    /// <summary>Una limpieza programada en una fecha (área común o depto de un vecino).</summary>
    public class VecinoLimpiezaDto
    {
        public int VecinoLimpiezaId { get; set; }
        public DateOnly Fecha { get; set; }
        public int VecinoLimpiezaTipoId { get; set; }
        public string TipoDescripcion { get; set; } = null!;
        public int? VecinoId { get; set; }
        public string? VecinoNombre { get; set; }
        /// <summary>Dirección del lote del vecino/departamento.</summary>
        public string? VecinoDireccion { get; set; }
        /// <summary>Interior/departamento del vecino (dentro del lote).</summary>
        public string? VecinoInterior { get; set; }
        public string? Descripcion { get; set; }
        public string? AtencionArchivoUrl { get; set; }
        public string? AtencionOriginalFileName { get; set; }
        public int? AtencionVecinoCompromisoId { get; set; }
        public string? AtencionCompromisoLabel { get; set; }
    }

    /// <summary>Opción de compromiso de un vecino, para relacionar la atención de limpieza.</summary>
    public class VecinoCompromisoSelectDto
    {
        public int VecinoCompromisoId { get; set; }
        public string Label { get; set; } = null!;
    }

    /// <summary>Limpiezas de un proyecto en un mes + catálogo de tipos.</summary>
    public class VecinoLimpiezasResponseDto
    {
        public List<VecinoLimpiezaDto> Limpiezas { get; set; } = new();
        public List<CatalogOptionDto> Tipos { get; set; } = new();
    }

    public class VecinoLimpiezaCreateDto
    {
        public DateOnly Fecha { get; set; }
        public int VecinoLimpiezaTipoId { get; set; }
        public int? VecinoId { get; set; }
        public string? Descripcion { get; set; }
    }

    /// <summary>
    /// Cumplimiento de atenciones de limpieza de un proyecto: programadas hasta hoy (sin contar
    /// futuras) vs. hechas (con archivo de atención subido), por departamento, área común y total.
    /// </summary>
    public class VecinoLimpiezaCumplimientoDto
    {
        public int DepartamentoProgramadas { get; set; }
        public int DepartamentoHechas { get; set; }
        public int ComunProgramadas { get; set; }
        public int ComunHechas { get; set; }
        public int TotalProgramadas { get; set; }
        public int TotalHechas { get; set; }
    }

    // ── Dashboard ─────────────────────────────────────────────────────────────
    /// <summary>Conteo de registros por estado de catálogo (para los donuts).</summary>
    public class VecinosDashboardEstadoDto
    {
        public int EstadoId { get; set; }
        public string Descripcion { get; set; } = null!;
        public int Count { get; set; }
    }

    /// <summary>Bloque del dashboard: usado tanto por proyecto como por el resumen global.</summary>
    public class VecinosDashboardProjectDto
    {
        /// <summary>0 cuando representa el resumen general (agregado global).</summary>
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        /// <summary>Cantidad de lotes/edificios del proyecto.</summary>
        public int LotesCount { get; set; }
        /// <summary>Cantidad de vecinos/departamentos del proyecto.</summary>
        public int VecinosCount { get; set; }
        public List<VecinosDashboardEstadoDto> Solicitudes { get; set; } = new();
        public List<VecinosDashboardEstadoDto> Compromisos { get; set; } = new();
        /// <summary>Cumplimiento de atenciones de limpieza (programadas hasta hoy vs. hechas).</summary>
        public VecinoLimpiezaCumplimientoDto Limpiezas { get; set; } = new();
    }

    /// <summary>Respuesta del dashboard: primero el resumen global, luego cada proyecto.</summary>
    public class VecinosDashboardDto
    {
        public VecinosDashboardProjectDto Resumen { get; set; } = new();
        public List<VecinosDashboardProjectDto> Proyectos { get; set; } = new();
    }
}
