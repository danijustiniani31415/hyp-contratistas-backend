namespace Abril_Backend.Features.GestionAdministrativa.SolicitudSalidas.Application.Dtos
{
    public class SolicitudSalidaFormDataDto
    {
        public List<MotivoSalidaDto> Motivos { get; set; } = new();
        public List<LugarSalidaDto> Lugares { get; set; } = new();

        /// <summary>Email de la jefatura que recibirá el email de aprobación. Null si no se pudo resolver.</summary>
        public string? AprobadorEmail { get; set; }

        /// <summary>True si el trabajador pertenece a "Tecnología de la Información". Habilita autocompleta de monto desde el catálogo.</summary>
        public bool EsTI { get; set; }

        /// <summary>Catálogo de trayectos activos — solo poblado cuando <see cref="EsTI"/> es true.</summary>
        public List<TrayectoCatalogoOptionDto> TrayectosCatalogo { get; set; } = new();
    }

    public class TrayectoCatalogoOptionDto
    {
        public int LugarOrigenId { get; set; }
        public int LugarDestinoId { get; set; }
        public decimal Monto { get; set; }
    }

    public class MotivoSalidaDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        /// <summary>Si true, el frontend exige subir un documento adjunto al elegir este motivo.</summary>
        public bool RequiereAdjunto { get; set; }
        /// <summary>Si true, las horas declaradas con este motivo son estimadas (cambia la etiqueta
        /// de la hora de retorno en el formulario y recepción no registra hora real).</summary>
        public bool EsHoraEstimada { get; set; }
        /// <summary>Si true, el frontend exige escribir un motivo adicional (detalle) al elegir este motivo.</summary>
        public bool RequiereMotivoAdicional { get; set; }
    }

    public class LugarSalidaDto
    {
        public int Id { get; set; }
        public string NombreDisplay { get; set; } = string.Empty;
        public bool EsLibre { get; set; }
    }
}
