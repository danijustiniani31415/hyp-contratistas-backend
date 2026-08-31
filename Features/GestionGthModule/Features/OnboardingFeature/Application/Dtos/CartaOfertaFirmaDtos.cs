namespace Abril_Backend.Features.GestionGthModule.Features.OnboardingFeature.Application.Dtos
{
    /// <summary>
    /// Todo lo que la página PÚBLICA de firma de la carta oferta necesita al abrirse, en una sola
    /// petición: de qué puesto es la propuesta, si ya hay una firma registrada para reusarla y en qué
    /// estado está el documento. La carta en sí no viaja acá — es un binario y va por su propio
    /// endpoint (<c>/publico/documento</c>), que es el que alimenta el visor.
    /// </summary>
    public class CartaOfertaFirmaPublicoDto
    {
        /// <summary>Nombre del postulante, para saludarlo y para que confirme que la carta es suya.</summary>
        public string Nombre { get; set; } = string.Empty;

        public string? Puesto { get; set; }
        public string? Area { get; set; }
        public string? Empresa { get; set; }
        public string? ProyectoObra { get; set; }
        public string? JefeDirecto { get; set; }
        public DateOnly? FechaIngreso { get; set; }

        /// <summary>Nombre del archivo de la carta oferta que subió GTH (el que se está viendo).</summary>
        public string? CartaNombre { get; set; }

        /// <summary>
        /// Firma ya guardada en su ficha de la base maestra, como data URL para mostrarla directo en
        /// un <c>&lt;img&gt;</c>. Null = todavía no registró ninguna, y el botón «Firmar» sigue
        /// bloqueado hasta que lo haga.
        /// </summary>
        public string? FirmaDataUrl { get; set; }

        /// <summary>Cuándo registró (o reemplazó) su firma, ya en hora de Perú.</summary>
        public DateTime? FirmaActualizadaEn { get; set; }

        /// <summary>true si la carta ya quedó firmada: la página pasa a solo lectura.</summary>
        public bool YaFirmada { get; set; }

        /// <summary>Cuándo firmó la carta, ya en hora de Perú.</summary>
        public DateTime? FirmadaEn { get; set; }

        /// <summary>
        /// true si GTH ya revisó y aprobó la carta firmada. Desde ese momento el documento es
        /// definitivo y no se puede volver a firmar ni siquiera para corregir la firma.
        /// </summary>
        public bool Aprobada { get; set; }
    }

    /// <summary>Firma que el postulante dibujó en el canvas de la página pública.</summary>
    public class CartaOfertaFirmaGuardarDto
    {
        /// <summary>data:image/png;base64,… (o solo el base64) generado con canvas.toDataURL('image/png').</summary>
        public string ImageBase64 { get; set; } = null!;
    }

    /// <summary>Resultado de guardar la firma: la firma que quedó, para repintarla en la página.</summary>
    public class CartaOfertaFirmaGuardarResultDto
    {
        public string Message { get; set; } = string.Empty;
        public string? FirmaDataUrl { get; set; }
        public DateTime? FirmaActualizadaEn { get; set; }
    }

    /// <summary>Resultado de firmar la carta oferta desde la página pública.</summary>
    public class CartaOfertaFirmarResultDto
    {
        public string Message { get; set; } = string.Empty;

        /// <summary>Cuándo quedó firmada, ya en hora de Perú.</summary>
        public DateTime? FirmadaEn { get; set; }
    }

    /// <summary>
    /// Lo que el servicio necesita para resolver un token: a qué onboarding apunta, de qué ficha es
    /// la firma, dónde está la carta oferta que hay que mostrar o estampar y en qué estado quedó.
    /// Lo arma el repositorio en un solo roundtrip y no sale nunca al frontend.
    /// </summary>
    public class CartaOfertaFirmaContextoDto
    {
        public int OnboardingId { get; set; }

        /// <summary>Ficha de la base maestra donde vive la firma del postulante.</summary>
        public int PersonId { get; set; }

        /// <summary>Código del requerimiento (REQ-AAAA-NNNN): nombra el archivo firmado.</summary>
        public string Codigo { get; set; } = string.Empty;

        /// <summary>Nombre con el que se armó el file digital, para rearmarlo si no está persistido.</summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>Documento de identidad, solo para ese mismo caso de rearmado.</summary>
        public string Dni { get; set; } = string.Empty;

        // ── Carta oferta que subió GTH (la que se muestra y se firma) ──────────
        public string? CartaOfertaNombre { get; set; }
        public string? CartaOfertaUrl { get; set; }
        public string? CartaOfertaDriveId { get; set; }
        public string? CartaOfertaItemId { get; set; }

        // ── Carta ya firmada, si existe ────────────────────────────────────────
        // Es la que el visor muestra después de firmar: al postulante le interesa revisar y descargar
        // el documento con su firma, no el original.
        public string? CartaFirmadaNombre { get; set; }
        public string? CartaFirmadaUrl { get; set; }
        public string? CartaFirmadaDriveId { get; set; }
        public string? CartaFirmadaItemId { get; set; }

        /// <summary>File digital del colaborador, si ya está persistido en la fila.</summary>
        public FileDigitalCarpetaDto? Carpeta { get; set; }

        public bool YaFirmada { get; set; }
        public bool Aprobada { get; set; }
    }
}
