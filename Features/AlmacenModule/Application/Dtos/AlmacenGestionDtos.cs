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
        /// <summary>Si viene, renombra el almacén (el nombre es interno: SUNAT nunca lo ve).</summary>
        public string? Nombre { get; set; }
        public string? Direccion { get; set; }
        public string? Ubigeo { get; set; }
        public string? CodigoEstablecimientoSunat { get; set; }
        /// <summary>Si viene, activa o desactiva el almacén (no se borra: tiene historial de movimientos).</summary>
        public bool? Activo { get; set; }
    }

    public class AlmacenCrearDto
    {
        public string Nombre { get; set; } = "";
        /// <summary>CENTRAL o PROYECTO.</summary>
        public string Tipo { get; set; } = "PROYECTO";
        public string? Direccion { get; set; }
        public string? Ubigeo { get; set; }
        public string? CodigoEstablecimientoSunat { get; set; }
    }
}
