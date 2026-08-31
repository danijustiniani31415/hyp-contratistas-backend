namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.ClinicaUsuarios
{
    public class ClinicaUsuarioListDto
    {
        public int ClinicaUsuarioId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public DateTime CreadoEn { get; set; }
        public bool TienePassword { get; set; }
    }

    public class ClinicaUsuarioDto
    {
        public int ClinicaUsuarioId { get; set; }
        public int ClinicaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public DateTime? UltimoAcceso { get; set; }
        public DateTime CreadoEn { get; set; }
        public int? CreadoPor { get; set; }
        public DateTime? ModificadoEn { get; set; }
        public int? ModificadoPor { get; set; }
        public bool TienePassword { get; set; }
    }

    public class ClinicaUsuarioCreateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class ClinicaUsuarioUpdateDto
    {
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }
}
