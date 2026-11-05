using System.Text.Json;

namespace Abril_Backend.Features.GuiasRemisionModule.Infrastructure.Models
{
    /// <summary>Conductor adicional de la guía (copia de sus datos al momento de crearla).</summary>
    public class ConductorSecundario
    {
        public string NumDoc { get; set; } = "";
        public string Nombres { get; set; } = "";
        public string Apellidos { get; set; } = "";
        public string Licencia { get; set; } = "";
    }

    /// <summary>Vehículo adicional (remolque/semirremolque) de la guía.</summary>
    public class VehiculoSecundario
    {
        public string Placa { get; set; } = "";
        public string? NroCirculacion { get; set; }
    }

    /// <summary>
    /// Los conductores y vehículos secundarios se guardan como JSON dentro de la guía: son una copia
    /// congelada de los datos (igual que el conductor principal), no una relación con los maestros.
    /// </summary>
    public static class GuiaSecundarios
    {
        private static readonly JsonSerializerOptions Opciones = new() { PropertyNameCaseInsensitive = true };

        public static List<T> Leer<T>(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<T>();
            try { return JsonSerializer.Deserialize<List<T>>(json, Opciones) ?? new List<T>(); }
            catch (JsonException) { return new List<T>(); }
        }

        public static string? Escribir<T>(IReadOnlyCollection<T> items) =>
            items.Count == 0 ? null : JsonSerializer.Serialize(items);
    }
}
