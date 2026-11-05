namespace Abril_Backend.Features.AlmacenModule.Application.Dtos
{
    public class AlmacenGestionDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = null!;
        public string Nombre { get; set; } = null!;
        public string Tipo { get; set; } = null!;
        public bool Activo { get; set; }
        public string? Direccion { get; set; }
        public string? Ubigeo { get; set; }
        public string CodigoEstablecimientoSunat { get; set; } = "0000";
    }

    /// <summary>Datos que SUNAT ve en la guía de remisión: punto de partida/llegada y código de establecimiento.</summary>
    public class AlmacenActualizarDto
    {
        public string? Direccion { get; set; }
        public string? Ubigeo { get; set; }
        public string? CodigoEstablecimientoSunat { get; set; }
    }
}
