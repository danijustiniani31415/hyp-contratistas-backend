namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.CasoSocial
{
    public class CasoSocialListItemDto
    {
        public Guid Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public string? EmpresaNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public DateOnly FechaApertura { get; set; }
        public string TipoCaso { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaCierre { get; set; }
        public int TotalSeguimientos { get; set; }
    }

    public class CasoSocialDetalleDto
    {
        public Guid Id { get; set; }
        public int WorkerId { get; set; }
        public string? WorkerNombre { get; set; }
        public string? WorkerDni { get; set; }
        public int? ProyectoId { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? EmpresaId { get; set; }
        public string? EmpresaNombre { get; set; }
        public DateOnly FechaApertura { get; set; }
        public string TipoCaso { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string? Descripcion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateOnly? FechaCierre { get; set; }
        public string? Resultado { get; set; }
        public int? RegistradoPorId { get; set; }
        public int? CerradoPorId { get; set; }
        public List<SeguimientoDto> Seguimientos { get; set; } = [];
    }

    public class CasoSocialCreateDto
    {
        public int WorkerId { get; set; }
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public DateOnly FechaApertura { get; set; }
        public string TipoCaso { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string? Descripcion { get; set; }
    }

    public class CasoSocialUpdateDto
    {
        public int? ProyectoId { get; set; }
        public int? EmpresaId { get; set; }
        public DateOnly FechaApertura { get; set; }
        public string TipoCaso { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public string? Motivo { get; set; }
        public string? Descripcion { get; set; }
    }

    public class CasoSocialCerrarDto
    {
        public DateOnly FechaCierre { get; set; }
        public string? Resultado { get; set; }
    }

    public class CasoSocialFilterDto
    {
        public int? WorkerId { get; set; }
        public string? Estado { get; set; }
        public string? TipoCaso { get; set; }
        public string? Prioridad { get; set; }
        public int? EmpresaId { get; set; }
        public int Page { get; set; } = 1;
    }
}
