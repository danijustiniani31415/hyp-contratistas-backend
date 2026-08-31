namespace Abril_Backend.Application.DTOs.ArquitecturaComercial
{
    public class AvanceSemanalSnapshotResultDTO
    {
        public int Total { get; set; }
        public DateOnly Semana { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
