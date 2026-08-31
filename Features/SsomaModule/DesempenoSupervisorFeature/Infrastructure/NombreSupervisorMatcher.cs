using System.Globalization;
using System.Text;

namespace Abril_Backend.Features.SsomaModule.DesempenoSupervisorFeature.Infrastructure;

/// <summary>
/// Resuelve a qué supervisor pertenece un RAC / OPT / inspección cuando el registro NO guardó
/// <c>created_by</c> y lo único que quedó es el nombre escrito como texto libre
/// (<c>reportante_nombre</c> / <c>observador_nombre</c> / <c>inspector_nombre</c>).
///
/// Antes se comparaba el texto guardado contra el nombre actual con un simple
/// <c>ToUpper().Trim()</c>, o sea igualdad exacta. Como el texto es un snapshot del momento en
/// que se creó el registro, cualquier corrección posterior del nombre en la ficha del
/// trabajador rompía el vínculo y los conteos de meses YA CERRADOS bajaban solos:
///
///   guardado "VELIZ ESTRADA MARTÍN WILFREDO"  vs  actual "VELIZ ESTRADA MARTIN WILFREDO"
///   guardado "Rhenzo barboza perales."        vs  actual "Barboza Perales Rhenzo Avelino"
///
/// (incidencia Corilla, ago-2026: al ing. Veliz se le cayó julio de 100% a 63% porque a su
/// nombre le quitaron la tilde, dejando 1 OPT y 2 inspecciones sin dueño.)
///
/// La comparación ahora es por CONJUNTO DE TOKENS normalizados: sin tildes, en mayúsculas, sin
/// puntuación y sin importar el orden de nombres y apellidos. Calza si los tokens de un lado
/// están contenidos en el otro (un nombre abreviado calza con el completo). Es determinista, no
/// difusa: si el texto calza con dos supervisores distintos no se atribuye a ninguno, porque
/// adivinar es peor que no contar.
/// </summary>
public static class NombreSupervisorMatcher
{
    /// <summary>
    /// Mínimo de tokens para el match PARCIAL (nombre abreviado contra nombre completo). Con uno
    /// o dos tokens ("PALMA JIMENEZ" contra "PALMA JIMENEZ MARIA MAGALY", "WILSON ULFE", o basura
    /// de pruebas tipo "qwerty") el riesgo de atribuirle actividad a la persona equivocada es
    /// mayor que el beneficio de contarla. No aplica al match por conjunto igual, que es exacto.
    /// </summary>
    private const int MinTokensParcial = 3;

    private static readonly char[] Separadores = [' ', ',', '.', ';', ':', '-', '_', '\t', '\n', '\r'];

    /// <summary>Normaliza a tokens comparables: sin diacríticos, mayúsculas, sin puntuación.</summary>
    public static string[] Tokens(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return [];

        var descompuesto = nombre.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(descompuesto.Length);
        foreach (var c in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        return sb.ToString()
            .ToUpperInvariant()
            .Split(Separadores, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// WorkerId del supervisor que calza con el nombre guardado, o 0 si no calza ninguno o si
    /// calza más de uno (ambiguo — ej. dos personas homónimas).
    /// </summary>
    public static int Resolver(string? nombreGuardado, IReadOnlyList<SupervisorNombre> supervisores)
    {
        var tokens = Tokens(nombreGuardado);
        if (tokens.Length == 0) return 0;

        // 1) Mismo conjunto de tokens = la misma persona, sin importar cuántos sean. Cubre todo
        //    lo que ya calzaba con la comparación exacta anterior —incluidos los nombres de dos
        //    palabras como "ESTEFANY CARO"— más las diferencias de tilde, coma y orden.
        var exactos = Candidatos(supervisores, s => MismoConjunto(tokens, s.Tokens));
        if (exactos.Count > 0) return exactos.Count == 1 ? exactos[0] : 0;

        // 2) Nombre abreviado contra nombre completo. Solo con 3+ tokens a ambos lados: es donde
        //    el match parcial deja de ser adivinanza.
        if (tokens.Length < MinTokensParcial) return 0;
        var parciales = Candidatos(supervisores,
            s => s.Tokens.Length >= MinTokensParcial && Contiene(tokens, s.Tokens));
        return parciales.Count == 1 ? parciales[0] : 0;
    }

    private static List<int> Candidatos(
        IReadOnlyList<SupervisorNombre> supervisores, Func<SupervisorNombre, bool> calza)
        => supervisores.Where(calza).Select(s => s.WorkerId).Distinct().ToList();

    private static bool MismoConjunto(string[] guardado, string[] actual)
        => guardado.Length == actual.Length && guardado.All(actual.Contains);

    // Calza si un conjunto contiene al otro: "RHENZO BARBOZA PERALES" (3) está contenido en
    // "BARBOZA PERALES RHENZO AVELINO" (4), y al revés si el nombre actual es el corto.
    private static bool Contiene(string[] guardado, string[] actual)
        => guardado.All(actual.Contains) || actual.All(guardado.Contains);

    public readonly record struct SupervisorNombre(int WorkerId, string[] Tokens);
}
