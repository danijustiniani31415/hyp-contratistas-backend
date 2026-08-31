namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Convalidacion
{
    public class ConvalidacionCreateDto
    {
        public int EmoOrigenId { get; set; }
        public int? EmpresaDestinoId { get; set; }
        public DateOnly FechaConvalidacion { get; set; }
        public int? MedicoId { get; set; }
        public string Resultado { get; set; } = "Pendiente";
        public DateOnly? FechaVencimiento { get; set; }
        public string? UrlDocumento { get; set; }
        public string? Notas { get; set; }

        /// <summary>Puesto que tenía el trabajador al momento del EMO de origen. Si no se
        /// envía, se usa la ocupación actual del trabajador.</summary>
        public string? PuestoOrigen { get; set; }

        /// <summary>Puesto al que pasa el trabajador en la empresa destino.</summary>
        public string? PuestoDestino { get; set; }

        /// <summary>Clasificación de riesgo del puesto origen (catálogo
        /// workers_obra_oficina_staff). Si no se envía, se usa la clasificación actual
        /// del trabajador.</summary>
        public int? ObraOficinaStaffOrigenId { get; set; }

        /// <summary>Clasificación de riesgo del puesto destino.</summary>
        public int? ObraOficinaStaffDestinoId { get; set; }

        /// <summary>PIN de firma del médico (nunca se persiste, solo se valida contra el
        /// hash guardado). Obligatorio cuando Resultado no es "Pendiente".</summary>
        public string? PinFirma { get; set; }

        /// <summary>Access token de Microsoft obtenido con un desafío de login fresco
        /// (prompt=login) al momento de firmar — prueba que el médico reautenticó su
        /// sesión de Microsoft en este acto, no que solo tenía sesión abierta.</summary>
        public string? MicrosoftAccessToken { get; set; }
    }
}
