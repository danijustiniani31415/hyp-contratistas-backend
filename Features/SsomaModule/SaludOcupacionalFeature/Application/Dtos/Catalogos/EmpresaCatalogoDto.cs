namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Catalogos
{
    /// <summary>
    /// DTO de razón social (contribuyente) usado por el catálogo de empresas en SSOMA
    /// y por la página Configuración → Razones Sociales.
    /// Los datos se leen desde la tabla <c>contributor</c>.
    /// </summary>
    public class EmpresaCatalogoDto
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Ruc { get; set; }
        public string? Direccion { get; set; }
        public string? PartidaRegistral { get; set; }
        public string? TipoActividad { get; set; }
        public bool? Activo { get; set; }
        public bool EsAbril { get; set; }
    }
}
