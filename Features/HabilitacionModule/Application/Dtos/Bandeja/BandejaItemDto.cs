using Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa;

namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Bandeja
{
    public class BandejaItemDto
    {
        public int Id { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string NombreEntregable { get; set; } = string.Empty;
        public string EntidadNombre { get; set; } = string.Empty;
        public string? EmpresaNombre { get; set; }
        public string? ProyectoNombre { get; set; }
        public int? ProyectoId { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
        public string? ArchivoUrl { get; set; }
        public string? ObsContratista { get; set; }
        public string Responsable { get; set; } = string.Empty;
        public DateTime? FechaEnvio { get; set; }
        public int? ItemId { get; set; }
        public bool EsMensual { get; set; }
        public int? Mes { get; set; }
        public int? Anio { get; set; }
        public int MesesPendientes { get; set; }
        public bool RequiereVigencia { get; set; }
        public List<EntregableMesArchivoDto> Archivos { get; set; } = [];
        public List<BandejaMesDto> Meses { get; set; } = [];
    }

    public class BandejaMesDto
    {
        public int Id { get; set; }
        public int Mes { get; set; }
        public int Anio { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? Vigencia { get; set; }
        public List<EntregableMesArchivoDto> Archivos { get; set; } = [];
    }
}
