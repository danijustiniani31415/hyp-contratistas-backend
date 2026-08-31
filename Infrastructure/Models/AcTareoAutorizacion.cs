using System.ComponentModel.DataAnnotations.Schema;

namespace Abril_Backend.Infrastructure.Models
{
    /// <summary>SSO-FO-150 firmado y escaneado — evidencia de que el trabajador autorizó el
    /// tratamiento de sus datos biométricos. Su existencia es la condición que habilita el
    /// enrolamiento facial (ver ArquitecturaComercialTareoService.EnrolarWorker).</summary>
    [Table("ac_tareo_autorizacion")]
    public class AcTareoAutorizacion
    {
        [Column("id")]
        public int Id { get; set; }

        [Column("worker_id")]
        public int WorkerId { get; set; }

        [Column("url_documento")]
        public string UrlDocumento { get; set; } = string.Empty;

        [Column("subido_por_user_id")]
        public int? SubidoPorUserId { get; set; }

        [Column("subido_en")]
        public DateTime SubidoEn { get; set; }
    }
}
