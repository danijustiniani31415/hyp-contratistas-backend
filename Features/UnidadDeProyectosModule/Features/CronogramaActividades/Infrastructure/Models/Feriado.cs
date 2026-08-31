namespace Abril_Backend.Shared.Models
{
    public class Feriado
    {
        public int Id { get; set; }
        public DateOnly Fecha { get; set; }
        public string? Descripcion { get; set; }
    }
}
