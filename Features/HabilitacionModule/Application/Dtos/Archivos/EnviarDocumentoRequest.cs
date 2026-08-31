namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Archivos
{
    public class EnviarDocumentoRequest
    {
        public int? HabTrabajadorId { get; set; }
        public int? HabEmpresaId { get; set; }
        public int? HabEquipoId { get; set; }
        public DateTime? Vigencia { get; set; }
        public string? ObsContratista { get; set; }
        public int? Mes { get; set; }
        public int? Anio { get; set; }
        public List<ArchivoSubidoDto> Archivos { get; set; } = [];
    }

    public class ArchivoSubidoDto
    {
        public string ArchivoUrl { get; set; } = string.Empty;
        public string? NombreArchivo { get; set; }
        public bool EsZip { get; set; }
        public string? ZipContenido { get; set; }
    }
}
