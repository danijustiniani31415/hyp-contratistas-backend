namespace Abril_Backend.Features.GuiasRemisionModule.Application.Dtos
{
    public class TransportistaDto
    {
        public long Id { get; set; }
        public string Ruc { get; set; } = null!;
        public string RazonSocial { get; set; } = null!;
        public string? NroMtc { get; set; }
        public bool Activo { get; set; }
    }

    public class VehiculoDto
    {
        public long Id { get; set; }
        public string Placa { get; set; } = null!;
        public string? NroCirculacion { get; set; }
        public bool Activo { get; set; }
    }

    public class ConductorDto
    {
        public long Id { get; set; }
        public string NumDoc { get; set; } = null!;
        public string Nombres { get; set; } = null!;
        public string Apellidos { get; set; } = null!;
        public string Licencia { get; set; } = null!;
        public bool Activo { get; set; }
    }
}
