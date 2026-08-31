namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Workers
{
    /// <summary>
    /// Edición de un trabajador desde el modal Configuración → Trabajadores.
    /// Modifica la tabla <c>person</c> (nombre completo, tipo y número de documento,
    /// cumpleaños) y campos de puesto/área en <c>workers</c> (categoría, ocupación,
    /// el puesto final autocompletado y el área asignada en el árbol de áreas).
    /// </summary>
    public class WorkerDatosBasicosDto
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public int? DocumentIdentityTypeId { get; set; }
        public string? NumeroDocumento { get; set; }
        public DateOnly? Cumpleanos { get; set; }
        /// <summary>Nombre de la categoría (campo de lógica), para mostrar.</summary>
        public string? Categoria { get; set; }
        /// <summary>Nombre del puesto (campo de presentación), para mostrar.</summary>
        public string? Puesto { get; set; }
        /// <summary>Nodo del árbol de áreas asignado al trabajador (workers.area_scope_id). Null = sin área.</summary>
        public int? AreaScopeId { get; set; }
        /// <summary>FK a <c>categoria</c>, derivada de <c>puesto.categoria_id</c>. Solo lectura.
        /// Null = sin puesto, o sea sin categoría.</summary>
        public int? CategoriaId { get; set; }
        /// <summary>FK a <c>puesto</c> (workers.puesto_id). Null = sin puesto.</summary>
        public int? PuestoId { get; set; }
        /// <summary>Correo corporativo del trabajador (workers.email_corporativo). Null/vacío = sin correo.</summary>
        public string? EmailCorporativo { get; set; }
        /// <summary>Correo personal / de contacto (person.email). Puede repetirse entre trabajadores.</summary>
        public string? EmailPersonal { get; set; }
    }
}
