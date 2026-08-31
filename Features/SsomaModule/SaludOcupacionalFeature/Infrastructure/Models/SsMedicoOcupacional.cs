using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Infrastructure.Models
{
    [Table("ss_medicos_ocupacionales")]
    public class SsMedicoOcupacional
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("apellido_nombre")]
        public string ApellidoNombre { get; set; } = string.Empty;

        [Column("cmp")]
        public string? Cmp { get; set; }

        [Column("especialidad")]
        public string? Especialidad { get; set; }

        [Column("clinica_id")]
        public int? ClinicaId { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("celular")]
        public string? Celular { get; set; }

        [Column("activo")]
        public bool Activo { get; set; }

        /// <summary>
        /// Médico que se asigna a una programación de EMO cuando no se eligió ninguno. El modal
        /// "Programar EMO con clínica" ya no ofrece elegirlo (lo pidió GTH), pero el listado y la
        /// agenda siguen mostrando la columna, así que la programación necesita uno igual. Va acá
        /// y no como constante en el código para que cambiar de médico por defecto sea un UPDATE
        /// y no un despliegue. Un índice único parcial garantiza que solo haya uno marcado.
        /// </summary>
        [Column("es_por_defecto")]
        public bool EsPorDefecto { get; set; }

        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Column("dni")]
        public string? Dni { get; set; }

        /// <summary>Fecha en que aceptó la autorización de firma electrónica (SSO-FO-149)
        /// dentro del sistema — no confundir con la fecha del documento escaneado.</summary>
        [Column("fecha_autorizacion_firma")]
        public DateTimeOffset? FechaAutorizacionFirma { get; set; }

        /// <summary>URL del PDF de autorización ya firmado a mano y escaneado (evidencia física).</summary>
        [Column("url_autorizacion_firmada")]
        public string? UrlAutorizacionFirmada { get; set; }

        /// <summary>Firma digital (imagen, idealmente PNG sin fondo) que el médico dibuja una
        /// vez, lo más parecida posible a la de su DNI. Se imprime en el SSO-FO-149 junto a un
        /// recuadro en blanco para que la firme también a mano y se puedan comparar. Requisito
        /// previo, junto con <see cref="UrlAutorizacionFirmada"/>, para poder configurar el PIN
        /// de firma (ver CatalogosService.SetPinFirmaAsync).</summary>
        [Column("firma_digital_url")]
        public string? FirmaDigitalUrl { get; set; }

        /// <summary>Hash del PIN de firma (nunca se guarda en texto plano). Se exige junto con
        /// la reautenticación de Microsoft para autorizar una convalidación.</summary>
        [Column("pin_firma_hash")]
        public string? PinFirmaHash { get; set; }

        [Column("pin_firma_intentos_fallidos")]
        public int PinFirmaIntentosFallidos { get; set; }

        [Column("pin_firma_bloqueado_hasta")]
        public DateTimeOffset? PinFirmaBloqueadoHasta { get; set; }

        [ForeignKey(nameof(ClinicaId))]
        public SsClinica? Clinica { get; set; }
    }
}
