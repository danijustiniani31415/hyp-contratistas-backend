using System.Text.Json.Serialization;

namespace Abril_Backend.Shared.Services.Sunat.Providers.Decolecta
{
    public class DecolectaRucResponse
    {
        [JsonPropertyName("razon_social")]
        public string RazonSocial { get; set; } = null!;

        [JsonPropertyName("numero_documento")]
        public string NumeroDocumento { get; set; } = null!;

        [JsonPropertyName("estado")]
        public string Estado { get; set; } = null!;

        [JsonPropertyName("condicion")]
        public string Condicion { get; set; } = null!;

        [JsonPropertyName("direccion")]
        public string Direccion { get; set; } = null!;

        [JsonPropertyName("ubigeo")]
        public string Ubigeo { get; set; } = null!;

        [JsonPropertyName("via_tipo")]
        public string ViaTipo { get; set; } = null!;

        [JsonPropertyName("via_nombre")]
        public string ViaNombre { get; set; } = null!;

        [JsonPropertyName("zona_codigo")]
        public string ZonaCodigo { get; set; } = null!;

        [JsonPropertyName("zona_tipo")]
        public string ZonaTipo { get; set; } = null!;

        [JsonPropertyName("numero")]
        public string Numero { get; set; } = null!;

        [JsonPropertyName("interior")]
        public string Interior { get; set; } = null!;

        [JsonPropertyName("lote")]
        public string Lote { get; set; } = null!;

        [JsonPropertyName("dpto")]
        public string Dpto { get; set; } = null!;

        [JsonPropertyName("manzana")]
        public string Manzana { get; set; } = null!;

        [JsonPropertyName("kilometro")]
        public string Kilometro { get; set; } = null!;

        [JsonPropertyName("distrito")]
        public string Distrito { get; set; } = null!;

        [JsonPropertyName("provincia")]
        public string Provincia { get; set; } = null!;

        [JsonPropertyName("departamento")]
        public string Departamento { get; set; } = null!;

        [JsonPropertyName("es_agente_retencion")]
        public bool EsAgenteRetencion { get; set; }

        [JsonPropertyName("es_buen_contribuyente")]
        public bool EsBuenContribuyente { get; set; }

        [JsonPropertyName("locales_anexos")]
        public object? LocalesAnexos { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; } = null!;

        [JsonPropertyName("actividad_economica")]
        public string ActividadEconomica { get; set; } = null!;

        [JsonPropertyName("numero_trabajadores")]
        public string NumeroTrabajadores { get; set; } = null!;

        [JsonPropertyName("tipo_facturacion")]
        public string TipoFacturacion { get; set; } = null!;

        [JsonPropertyName("tipo_contabilidad")]
        public string TipoContabilidad { get; set; } = null!;

        [JsonPropertyName("comercio_exterior")]
        public string ComercioExterior { get; set; } = null!;
    }
}
