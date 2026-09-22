namespace Abril_Backend.Features.PersonasModule.Application.Dtos
{
    public class TareoCeldaDto
    {
        public int Dia { get; set; } // 1..31
        /// <summary>"" = sin registrar. NORMAL, DL, F, P, VC, DM.</summary>
        public string TipoDia { get; set; } = "";
        public decimal? HorasTrabajadas { get; set; }
        public decimal HorasExtra { get; set; }
    }

    public class TareoPersonaDto
    {
        public int PersonaId { get; set; }
        public string NombreCompleto { get; set; } = null!;
        public string? CargoNombre { get; set; }
        public List<TareoCeldaDto> Dias { get; set; } = new();
    }

    public class TareoMesResponseDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public int DiasEnMes { get; set; }
        public List<TareoPersonaDto> Personas { get; set; } = new();
    }

    public class TareoPersonaGuardarDto
    {
        public int PersonaId { get; set; }
        public List<TareoCeldaDto> Dias { get; set; } = new();
    }

    public class TareoGuardarDto
    {
        public int Anio { get; set; }
        public int Mes { get; set; }
        public List<TareoPersonaGuardarDto> Personas { get; set; } = new();
    }
}
