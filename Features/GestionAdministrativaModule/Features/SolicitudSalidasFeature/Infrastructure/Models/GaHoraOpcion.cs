namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Infrastructure.Models
{
    public class GaHoraOpcion
    {
        public int Id { get; set; }
        public TimeOnly Hora { get; set; }
        public string Etiqueta { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;
    }
}
