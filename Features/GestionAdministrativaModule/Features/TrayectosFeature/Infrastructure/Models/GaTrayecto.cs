namespace Abril_Backend.Features.GestionAdministrativa.Trayectos.Infrastructure.Models
{
    /// <summary>
    /// Catálogo de trayectos preconfigurados — asocia un par (lugar origen, lugar destino) con su
    /// monto referencial en soles. Sirve como fuente de datos para autocompletar montos en las
    /// solicitudes de salida. Distinto de <c>GaSolicitudTrayecto</c>, que representa el tramo
    /// real de una solicitud específica.
    /// </summary>
    public class GaTrayecto
    {
        public int Id { get; set; }
        public int LugarOrigenId { get; set; }
        public int LugarDestinoId { get; set; }
        public decimal Monto { get; set; }
        public bool Activo { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}
