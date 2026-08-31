namespace Abril_Backend.Features.GestionAdministrativa.MotivosSalida.Application.Dtos
{
    public class GaMotivoSalidaConfigItemDto
    {
        public int Id { get; set; }
        public string Descripcion { get; set; } = string.Empty;
        public bool Activo { get; set; }
        /// <summary>Si true, al solicitar una salida con este motivo se exige un documento adjunto.</summary>
        public bool RequiereAdjunto { get; set; }
        /// <summary>Si true, las horas declaradas son estimadas: recepción no registra hora real.</summary>
        public bool EsHoraEstimada { get; set; }
        /// <summary>Si true, al elegir este motivo en una solicitud se exige escribir un motivo adicional (detalle).</summary>
        public bool RequiereMotivoAdicional { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }

    public class GaMotivoSalidaCreateDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool RequiereAdjunto { get; set; }
        public bool EsHoraEstimada { get; set; }
        public bool RequiereMotivoAdicional { get; set; }
    }

    public class GaMotivoSalidaEditDto
    {
        public string Descripcion { get; set; } = string.Empty;
        public bool RequiereAdjunto { get; set; }
        public bool EsHoraEstimada { get; set; }
        public bool RequiereMotivoAdicional { get; set; }
    }
}
