using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Abril_Backend.Features.SsomaModule.PresupuestoMaterialesFeature.Application.Services;

public static class TextoNormalizador
{
    private static readonly Regex EspaciosMultiples = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex PrefijoNoUsar = new(@"^\(NO USAR\)\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PatronTalla = new(
        @"\bN[ºO°]?\s?(\d{2})\b|\bTALLA\s+([A-Z0-9]{1,3})\b|\bT[/\-]\s?(\d{2})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex PatronDimension = new(
        @"(\d+(?:[.,]\d+)?)\s?[xX]\s?(\d+(?:[.,]\d+)?)\s?(CM|M|MM)?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Mayúsculas, sin acentos, espacios colapsados, sin prefijo "(NO USAR)".</summary>
    public static string Normalizar(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

        var sinAcentos = QuitarAcentos(texto.Trim().ToUpperInvariant());
        sinAcentos = PrefijoNoUsar.Replace(sinAcentos, string.Empty);
        return EspaciosMultiples.Replace(sinAcentos, " ").Trim();
    }

    public static bool TieneNoUsar(string texto) =>
        !string.IsNullOrWhiteSpace(texto) && PrefijoNoUsar.IsMatch(texto.Trim());

    /// <summary>Extrae talla (ej. "42", "L") y devuelve el texto sin ese fragmento.</summary>
    public static (string TextoResidual, string? Talla) ExtraerTalla(string textoNormalizado)
    {
        var match = PatronTalla.Match(textoNormalizado);
        if (!match.Success) return (textoNormalizado, null);

        var talla = match.Groups[1].Success ? match.Groups[1].Value
                  : match.Groups[2].Success ? match.Groups[2].Value
                  : match.Groups[3].Value;

        var residual = EspaciosMultiples.Replace(PatronTalla.Replace(textoNormalizado, " ", 1), " ").Trim();
        return (residual, talla);
    }

    /// <summary>Extrae dimensión (ej. "60X40CM", "0.60X0.40") y la canoniza en metros con 2 decimales.</summary>
    public static (string TextoResidual, string? DimensionNorm) ExtraerDimension(string textoNormalizado)
    {
        var match = PatronDimension.Match(textoNormalizado);
        if (!match.Success) return (textoNormalizado, null);

        var a = match.Groups[1].Value.Replace(',', '.');
        var b = match.Groups[2].Value.Replace(',', '.');
        var unidad = match.Groups[3].Success ? match.Groups[3].Value.ToUpperInvariant() : "CM";

        var (metroA, metroB) = (ConvertirAMetros(a, unidad), ConvertirAMetros(b, unidad));
        var dimensionNorm = $"{metroA:0.00}X{metroB:0.00}";

        var residual = EspaciosMultiples.Replace(PatronDimension.Replace(textoNormalizado, " ", 1), " ").Trim();
        return (residual, dimensionNorm);
    }

    private static decimal ConvertirAMetros(string valor, string unidad)
    {
        var numero = decimal.Parse(valor, CultureInfo.InvariantCulture);
        return unidad switch
        {
            "MM" => numero / 1000m,
            "M" => numero,
            // CM por defecto; si el número ya viene como 0.60 (formato metros) se respeta tal cual
            _ => numero > 10 ? numero / 100m : numero
        };
    }

    private static string QuitarAcentos(string texto)
    {
        var normalizado = texto.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalizado)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }
        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
