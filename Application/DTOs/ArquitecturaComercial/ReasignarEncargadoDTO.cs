namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class ReasignarEncargadoDTO
    {
        public int ProyectoId { get; set; }
    }

    public class ReasignarEncargadoResultDTO
    {
        public int Actualizadas { get; set; }
        public bool WorkerNoEncontrado { get; set; }
    }
}
