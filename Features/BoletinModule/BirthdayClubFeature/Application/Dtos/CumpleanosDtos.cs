namespace Abril_Backend.Features.BoletinModule.BirthdayClubFeature.Application.Dtos
{
    /// <summary>
    /// Una persona cumpleañera del trimestre. El cumpleaños sale de
    /// <c>person.fecha_nacimiento</c>. Solo se incluye si el trabajador tiene un
    /// <c>email_corporativo</c> con dominio @abril.pe y si su <c>person.mostrar_en_boletin</c>
    /// está en true (checkbox "Mostrar en el boletín" del formulario de trabajadores).
    /// </summary>
    public class CumpleaneroDto
    {
        public int WorkerId { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        /// <summary>Nombre del puesto del trabajador (catálogo <c>puesto</c>).</summary>
        public string? Puesto { get; set; }
        public string Email { get; set; } = string.Empty;

        /// <summary>Mes del cumpleaños (1-12).</summary>
        public int Mes { get; set; }

        /// <summary>Día del cumpleaños (1-31).</summary>
        public int Dia { get; set; }

        /// <summary>Foto en data URI base64, o null si Graph no devolvió foto.</summary>
        public string? FotoBase64 { get; set; }
    }

    /// <summary>Cumpleañeros de un trimestre (1-4) listos para pintar en el calendario.</summary>
    public class TrimestreCumpleanosDto
    {
        public int Trimestre { get; set; }
        public List<CumpleaneroDto> Cumpleaneros { get; set; } = new();
    }
}
