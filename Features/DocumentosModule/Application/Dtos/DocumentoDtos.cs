namespace Abril_Backend.Features.DocumentosModule.Application.Dtos
{
    /// <summary>
    /// Resultado de buscar un DNI o un RUC. Siempre indica de dónde salió el dato, para que quien lo
    /// registra sepa si es alguien que ya tiene la empresa o algo consultado afuera.
    /// </summary>
    public class DocumentoDto
    {
        /// <summary>TRABAJADOR, TRANSPORTISTA o PROVEEDOR (ya registrados aquí), RENIEC o SUNAT (consulta externa).</summary>
        public string Origen { get; set; } = null!;
        public string Numero { get; set; } = null!;

        // Persona (DNI)
        public string? Nombres { get; set; }
        public string? Apellidos { get; set; }
        /// <summary>Si el DNI es de un trabajador registrado en Personas.</summary>
        public int? PersonaId { get; set; }

        // Empresa (RUC)
        public string? RazonSocial { get; set; }
        public string? Direccion { get; set; }

        /// <summary>Advertencia que no impide seguir (p. ej. la persona figura inactiva).</summary>
        public string? Aviso { get; set; }
    }
}
