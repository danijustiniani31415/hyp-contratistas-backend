using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Configuracion
{
    /// <summary>Códigos del catálogo <c>ss_emo_correo_tipo</c>.</summary>
    public static class EmoCorreoTipoCodigo
    {
        public const string Principal = "PRINCIPAL";
        public const string Copia     = "COPIA";
    }

    /// <summary>
    /// Códigos del catálogo <c>ss_emo_correo_evento</c>: los 4 correos de EMO cuyos
    /// destinatarios se configuran en /ssoma/salud-ocupacional/emos/configuracion.
    /// </summary>
    public static class EmoCorreoEventoCodigo
    {
        /// <summary>Resumen del cron diario de programación automática por vencimiento.</summary>
        public const string ProgramacionAutomatica = "PROGRAMACION_AUTOMATICA";
        /// <summary>Se programó un EMO a mano desde EMOs o Programaciones.</summary>
        public const string ProgramacionManual     = "PROGRAMACION_MANUAL";
        /// <summary>La clínica aceptó la cita.</summary>
        public const string Aceptada               = "ACEPTADA";
        /// <summary>La clínica rechazó la cita.</summary>
        public const string Rechazada              = "RECHAZADA";
    }

    /// <summary>
    /// Códigos del catálogo <c>ss_emo_correo_perfil</c>: la "columna" de la matriz de
    /// destinatarios. A un trabajador de Oficina Central no le escribe la misma gente
    /// que a uno de obra, así que cada correo se configura por perfil.
    ///
    /// Solo hay perfiles para el personal de casa: por negocio Abril controla únicamente
    /// el EMO de sus propios trabajadores — las contratistas manejan el de los suyos por
    /// su cuenta, y el cron de auto-programación ya las excluye filtrando por
    /// <c>contributor.es_abril</c>.
    /// </summary>
    public static class EmoCorreoPerfilCodigo
    {
        public const string OficinaCentral = "OFICINA_CENTRAL";
        public const string Staff          = "STAFF";
        public const string Obra           = "OBRA";

        /// <summary>
        /// Perfil de un trabajador a partir de sus dos campos de clasificación, o
        /// <c>null</c> si no es personal de casa: los trabajadores de contratistas no
        /// reciben ninguno de los correos de EMO de Abril.
        ///
        /// El personal de casa sin modalidad registrada cae en <see cref="Obra"/>, que es
        /// como lo trataba el código anterior a la matriz (y coincide con que casi ninguno
        /// de ellos tiene correo corporativo).
        /// </summary>
        public static string? Resolver(string? contrataCasa, int? obraOficinaStaffId)
        {
            if (!string.Equals(contrataCasa?.Trim(), "Casa", StringComparison.OrdinalIgnoreCase))
                return null;

            return obraOficinaStaffId switch
            {
                ObraOficinaStaffIds.OficinaCentral => OficinaCentral,
                ObraOficinaStaffIds.Staff          => Staff,
                _                                  => Obra,
            };
        }
    }

    /// <summary>
    /// Códigos de los destinatarios de catálogo (filas de <c>ss_emo_correo_destinatario</c>
    /// con <c>codigo</c> no nulo). Los correos adicionales que se agregan desde la pantalla
    /// no tienen código.
    /// </summary>
    public static class EmoCorreoDestinatarioCodigo
    {
        // ── Dinámicos: el correo no está guardado, se resuelve al enviar ──
        /// <summary>Correos de contacto de la clínica de la programación.</summary>
        public const string Clinica            = "CLINICA";
        /// <summary>Jefe personalizado del trabajador o, si no tiene, el revisor de su área.</summary>
        public const string Jefe               = "JEFE";
        /// <summary>Correo corporativo del propio trabajador.</summary>
        public const string Trabajador         = "TRABAJADOR";
        /// <summary>Residente del proyecto donde está vinculado el trabajador.</summary>
        public const string Residente          = "RESIDENTE";
        /// <summary>Coordinador administrativo del proyecto.</summary>
        public const string CoordAdmin         = "COORD_ADMIN";
        /// <summary>Coordinador SSOMA del proyecto.</summary>
        public const string CoordSsoma         = "COORD_SSOMA";
        /// <summary>Administrador de la razón social del trabajador.</summary>
        public const string AdminRazonSocial   = "ADMIN_RAZON_SOCIAL";
        /// <summary>Correo del área de Gestión del Talento Humano (<c>area_scope</c>).</summary>
        public const string Gth                = "GTH";

        // ── Buzones de área: el correo vive en la fila y se edita desde la pantalla ──
        public const string MedicinaOcupacional      = "MEDICINA_OCUPACIONAL";
        public const string ArqComJefe               = "ARQCOM_JEFE";
        public const string ArqComPrevencionista     = "ARQCOM_PREVENCIONISTA";
        public const string PostVentaJefe            = "POSTVENTA_JEFE";
        public const string PostVentaPrevencionista  = "POSTVENTA_PREVENCIONISTA";

        /// <summary>
        /// Destinatarios que solo reciben el correo si el proyecto del trabajador tiene
        /// arquitectura comercial (<c>project.tiene_arquitectura_comercial</c>). Es una
        /// condición del negocio, no un interruptor: por eso no se configura en la matriz.
        /// </summary>
        public static readonly string[] SoloConArquitecturaComercial =
        {
            ArqComJefe, ArqComPrevencionista, PostVentaJefe, PostVentaPrevencionista
        };

        public static bool RequiereArquitecturaComercial(string? codigo) =>
            codigo != null &&
            SoloConArquitecturaComercial.Contains(codigo, StringComparer.OrdinalIgnoreCase);
    }

    // ────────────────────────── Pantalla de configuración ──────────────────────────

    /// <summary>Una columna de la matriz.</summary>
    public class EmoCorreoPerfilDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    /// <summary>El interruptor de una celda (correo × perfil × destinatario).</summary>
    public class EmoCorreoCeldaDto
    {
        public int ReglaId { get; set; }
        public int PerfilId { get; set; }
        public string PerfilCodigo { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    /// <summary>Una fila de la matriz de un correo: el destinatario y sus celdas por perfil.</summary>
    public class EmoCorreoFilaDto
    {
        public int DestinatarioId { get; set; }
        /// <summary>Null en los correos adicionales agregados a mano.</summary>
        public string? Codigo { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        /// <summary>Null en los destinatarios dinámicos: su correo se resuelve al enviar.</summary>
        public string? Email { get; set; }
        /// <summary>PRINCIPAL (va en "Para") o COPIA (va en "CC").</summary>
        public string Tipo { get; set; } = EmoCorreoTipoCodigo.Principal;
        /// <summary>true = se puede cambiar el correo desde la pantalla.</summary>
        public bool Editable { get; set; }
        /// <summary>true = se puede eliminar la fila (solo los correos adicionales).</summary>
        public bool Eliminable { get; set; }
        /// <summary>true = además de estar activo, exige que el proyecto tenga arquitectura comercial.</summary>
        public bool RequiereArqCom { get; set; }
        /// <summary>true = está activo pero no tiene correo cargado, así que no le llega nada.</summary>
        public bool SinCorreo { get; set; }
        public int Orden { get; set; }
        public List<EmoCorreoCeldaDto> Celdas { get; set; } = new();
    }

    /// <summary>Una sección de la pantalla: un correo con toda su matriz.</summary>
    public class EmoCorreoEventoDto
    {
        public int Id { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public int Orden { get; set; }
        public List<EmoCorreoFilaDto> Destinatarios { get; set; } = new();
    }

    /// <summary>
    /// Respuesta única de la pantalla de Configuración de EMOs: los perfiles (columnas)
    /// y los 4 correos con su matriz completa, en una sola petición.
    /// </summary>
    public class EmoCorreosConfigDto
    {
        public List<EmoCorreoPerfilDto> Perfiles { get; set; } = new();
        public List<EmoCorreoEventoDto> Eventos { get; set; } = new();
    }

    // ────────────────────────────── Escritura ──────────────────────────────

    public class EmoCorreoAdicionalCreateDto
    {
        /// <summary>Correo desde cuya sección se está agregando: nace activo en sus 4 perfiles.</summary>
        public string EventoCodigo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        /// <summary>PRINCIPAL o COPIA. Por defecto PRINCIPAL.</summary>
        public string? Tipo { get; set; }
    }

    public class EmoCorreoDestinatarioUpdateDto
    {
        public string Email { get; set; } = string.Empty;
        public string? Nombre { get; set; }
        /// <summary>PRINCIPAL o COPIA. Solo aplica a los correos adicionales.</summary>
        public string? Tipo { get; set; }
    }

    public class EmoCorreoReglaPatchDto
    {
        public bool Active { get; set; }
    }
}
