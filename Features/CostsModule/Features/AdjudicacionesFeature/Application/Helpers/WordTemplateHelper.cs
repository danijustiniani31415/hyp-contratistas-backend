using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Helpers;

/// <summary>
/// Rellena un template Word (.docx) reemplazando placeholders del tipo {{CLAVE}}.
/// Trabaja directamente sobre el XML interno del .docx (ZIP) y fusiona únicamente
/// los nodos &lt;w:t&gt; que forman el placeholder, preservando el formato del resto.
/// Procesa: cuerpo del documento, encabezados y pies de página.
/// </summary>
public static class WordTemplateHelper
{
    /// <param name="multiParagraphReplacements">
    /// Reemplazos que expanden un único párrafo &lt;w:p&gt; que contiene la clave
    /// en N párrafos (uno por elemento de la lista). Si la lista está vacía el párrafo
    /// se elimina. Útil para marcadores como <c>{{CLÁUSULAS}}</c>.
    /// </param>
    /// <param name="boldAwareMultiParagraphReplacements">
    /// Igual que <paramref name="multiParagraphReplacements"/> pero cada entrada incluye
    /// un <c>baseRPr</c> (bloque <c>&lt;w:rPr&gt;…&lt;/w:rPr&gt;</c> con fuente y tamaño
    /// deseados) y una lista de ítems donde cada uno lleva un flag <c>bold</c> que fuerza
    /// o elimina la negrita independientemente del formato del placeholder en la plantilla.
    /// Útil para marcadores como <c>{{LINKS}}</c> donde se necesita fuente fija (p.ej.
    /// Arial Narrow 9 pt) y algunos párrafos van en negrita y otros no.
    /// </param>
    /// <param name="hyperlinkTargets">
    /// Placeholders que viven como <b>texto visible dentro de un</b> <c>&lt;w:hyperlink r:id="…"&gt;</c>
    /// (p. ej. <c>{{LINK1}}</c>). Además de reemplazar el texto (vía <paramref name="replacements"/>),
    /// se reescribe el <c>Target</c> de la relación del hipervínculo en
    /// <c>word/_rels/document.xml.rels</c> a la URL indicada. Necesario porque el destino del clic
    /// se guarda como relación aparte del texto: si solo se cambia el texto, el enlace sigue
    /// apuntando a donde lo dejó la plantilla. Solo se tocan las relaciones cuyo placeholder tenga
    /// URL no vacía.
    /// </param>
    public static byte[] FillTemplate(
        Stream templateStream,
        Dictionary<string, string> replacements,
        Dictionary<string, List<string>>? multiParagraphReplacements = null,
        Dictionary<string, (string baseRPr, List<(string text, bool bold)> items)>? boldAwareMultiParagraphReplacements = null,
        Dictionary<string, string>? hyperlinkTargets = null)
    {
        var ms = new MemoryStream();
        templateStream.CopyTo(ms);

        using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, leaveOpen: true))
        {
            // Reescribir el destino real de los hipervínculos ANTES del reemplazo de texto
            // (usa el documento original, donde el placeholder {{LINKn}} aún está intacto).
            UpdateHyperlinkRelationshipTargets(zip, hyperlinkTargets);

            // Recopilar todas las partes que pueden contener texto con placeholders:
            // cuerpo principal + todos los encabezados y pies de página numerados.
            var entryNames = zip.Entries
                .Select(e => e.FullName)
                .Where(n =>
                    n == "word/document.xml" ||
                    Regex.IsMatch(n, @"^word/header\d+\.xml$", RegexOptions.IgnoreCase) ||
                    Regex.IsMatch(n, @"^word/footer\d+\.xml$", RegexOptions.IgnoreCase))
                .ToList();

            foreach (var name in entryNames)
                ProcessZipEntry(zip, name, replacements, multiParagraphReplacements, boldAwareMultiParagraphReplacements);
        }

        ms.Position = 0;
        return ms.ToArray();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static void ProcessZipEntry(
        ZipArchive zip,
        string entryName,
        Dictionary<string, string> replacements,
        Dictionary<string, List<string>>? multiParagraphReplacements,
        Dictionary<string, (string baseRPr, List<(string text, bool bold)> items)>? boldAwareMultiParagraphReplacements)
    {
        var entry = zip.GetEntry(entryName);
        if (entry is null) return;

        string xml;
        using (var stream = entry.Open())
        using (var reader = new StreamReader(stream, Encoding.UTF8))
            xml = reader.ReadToEnd();

        // Eliminar elementos que fragmentan runs sin aportar texto visible
        xml = Regex.Replace(xml, @"<w:proofErr\b[^>]*/?>",           "");
        xml = Regex.Replace(xml, @"<w:bookmarkStart\b[^>]*/?>",      "");
        xml = Regex.Replace(xml, @"<w:bookmarkEnd\b[^>]*/?>",        "");
        xml = Regex.Replace(xml, @"<w:rPrChange\b.*?</w:rPrChange>", "", RegexOptions.Singleline);

        // Reemplazos simples: un valor por placeholder (dentro del mismo párrafo)
        xml = Regex.Replace(
            xml,
            @"<w:p[\s>].*?</w:p>",
            m => ReplaceInParagraphXml(m.Value, replacements),
            RegexOptions.Singleline);

        // Reemplazos multi-párrafo: un placeholder → N párrafos (uno por valor)
        if (multiParagraphReplacements is { Count: > 0 })
        {
            foreach (var (placeholder, values) in multiParagraphReplacements)
                xml = ReplaceWithMultipleParagraphs(xml, placeholder, values);
        }

        // Reemplazos multi-párrafo con control explícito de negrita por ítem
        if (boldAwareMultiParagraphReplacements is { Count: > 0 })
        {
            foreach (var (placeholder, (baseRPr, items)) in boldAwareMultiParagraphReplacements)
                xml = ReplaceWithBoldAwareMultipleParagraphs(xml, placeholder, baseRPr, items);
        }

        entry.Delete();
        var newEntry = zip.CreateEntry(entryName);
        using var writer = new StreamWriter(newEntry.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(xml);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Destino de hipervínculos (relaciones)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reescribe el atributo <c>Target</c> de las relaciones de hipervínculo en
    /// <c>word/_rels/document.xml.rels</c>. Para cada placeholder de
    /// <paramref name="hyperlinkTargets"/>, localiza en <c>document.xml</c> el
    /// <c>&lt;w:hyperlink r:id="…"&gt;</c> cuyo texto visible contiene ese placeholder y
    /// apunta esa relación a la URL indicada. Es imprescindible porque el texto del enlace
    /// y su destino son cosas distintas en OOXML: reemplazar solo el texto deja el clic
    /// apuntando a donde lo dejó la plantilla.
    /// </summary>
    private static void UpdateHyperlinkRelationshipTargets(
        ZipArchive zip, Dictionary<string, string>? hyperlinkTargets)
    {
        if (hyperlinkTargets is not { Count: > 0 }) return;

        var docEntry  = zip.GetEntry("word/document.xml");
        var relsEntry = zip.GetEntry("word/_rels/document.xml.rels");
        if (docEntry is null || relsEntry is null) return;

        string docXml;
        using (var s = docEntry.Open())
        using (var r = new StreamReader(s, Encoding.UTF8))
            docXml = r.ReadToEnd();

        // r:id del hipervínculo → URL destino. Se recorre cada bloque <w:hyperlink>…</w:hyperlink>
        // y se compara su texto plano (tolerante a placeholders partidos en varios runs).
        var idToUrl = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match hl in Regex.Matches(docXml, @"<w:hyperlink\b[^>]*>.*?</w:hyperlink>", RegexOptions.Singleline))
        {
            var ridMatch = Regex.Match(hl.Value, @"\br:id=""([^""]+)""");
            if (!ridMatch.Success) continue;

            var visibleText = ExtractPlainText(hl.Value);
            foreach (var (placeholder, url) in hyperlinkTargets)
            {
                if (string.IsNullOrEmpty(url)) continue;
                if (visibleText.Contains(placeholder, StringComparison.Ordinal))
                    idToUrl[ridMatch.Groups[1].Value] = url;
            }
        }

        if (idToUrl.Count == 0) return;

        string rels;
        using (var s = relsEntry.Open())
        using (var r = new StreamReader(s, Encoding.UTF8))
            rels = r.ReadToEnd();

        rels = Regex.Replace(rels, @"<Relationship\b[^>]*/>", relMatch =>
        {
            var element = relMatch.Value;
            var idMatch = Regex.Match(element, @"\bId=""([^""]+)""");
            if (!idMatch.Success || !idToUrl.TryGetValue(idMatch.Groups[1].Value, out var url))
                return element;

            var escaped = XmlAttrEscape(url);
            // MatchEvaluator para evitar que caracteres como '$' de la URL se interpreten
            // como referencias de grupo en la cadena de reemplazo.
            return Regex.Replace(element, @"(\bTarget="")[^""]*("")",
                mm => mm.Groups[1].Value + escaped + mm.Groups[2].Value);
        });

        relsEntry.Delete();
        var newRels = zip.CreateEntry("word/_rels/document.xml.rels");
        using var writer = new StreamWriter(newRels.Open(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(rels);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reemplazo multi-párrafo
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Localiza el párrafo &lt;w:p&gt; que contiene <paramref name="placeholder"/> y lo
    /// sustituye por tantos párrafos como elementos tenga <paramref name="values"/>.
    /// Si <paramref name="values"/> está vacío, el párrafo se elimina.
    /// Los saltos de línea (\n) dentro de cada valor se convierten en &lt;w:br/&gt;.
    /// </summary>
    private static string ReplaceWithMultipleParagraphs(string xml, string placeholder, List<string> values)
    {
        return Regex.Replace(
            xml,
            @"<w:p[\s>].*?</w:p>",
            m =>
            {
                var paraXml = m.Value;

                // Comprobar si este párrafo contiene el placeholder en su texto visible
                if (!ExtractPlainText(paraXml).Contains(placeholder, StringComparison.Ordinal))
                    return paraXml;

                // Sin valores → eliminar el párrafo del documento
                if (values.Count == 0) return "";

                // Extraer propiedades del párrafo (<w:pPr>) para clonarlas
                var pPrMatch = Regex.Match(paraXml, @"<w:pPr>.*?</w:pPr>", RegexOptions.Singleline);
                var pPr = pPrMatch.Success ? pPrMatch.Value : "";

                // Extraer <w:rPr> del primer <w:r> real del párrafo.
                // IMPORTANTE: <w:pPr> también puede contener un <w:rPr> (del marcador de párrafo ¶)
                // que aparece antes que los runs de texto, por lo que no se puede usar un
                // Regex.Match simple sobre todo el XML del párrafo.
                var rPr = "";
                var firstRunMatch = Regex.Match(paraXml, @"<w:r\b[^>]*>(.*?)</w:r>", RegexOptions.Singleline);
                if (firstRunMatch.Success)
                {
                    var rPrInRun = Regex.Match(firstRunMatch.Groups[1].Value, @"<w:rPr>.*?</w:rPr>", RegexOptions.Singleline);
                    rPr = rPrInRun.Success ? rPrInRun.Value : "";
                }

                // Párrafo separador: mismas propiedades que el original PERO sin <w:numPr>
                // para que el contador de la lista no avance y no aparezca un número vacío.
                var separatorPPr = Regex.Replace(pPr, @"<w:numPr>.*?</w:numPr>", "", RegexOptions.Singleline);

                var sb = new StringBuilder();
                for (int vi = 0; vi < values.Count; vi++)
                {
                    sb.Append(BuildParagraphWithText(pPr, rPr, values[vi]));
                    // Línea en blanco entre cláusulas (sin numeración)
                    if (vi < values.Count - 1)
                        sb.Append($"<w:p>{separatorPPr}</w:p>");
                }

                return sb.ToString();
            },
            RegexOptions.Singleline);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reemplazo multi-párrafo con control de negrita por ítem
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Igual que <see cref="ReplaceWithMultipleParagraphs"/> pero cada elemento lleva
    /// un flag <c>bold</c> que fuerza o elimina la negrita en ese párrafo.
    /// <paramref name="baseRPr"/> define la fuente y el tamaño fijos que se aplican
    /// a todos los runs (p.ej. Arial Narrow 9 pt); la negrita se añade encima cuando
    /// el flag lo requiere.  Si se pasa <c>""</c> los runs heredan el estilo del párrafo.
    /// </summary>
    private static string ReplaceWithBoldAwareMultipleParagraphs(
        string xml, string placeholder, string baseRPr, List<(string text, bool bold)> values)
    {
        return Regex.Replace(
            xml,
            @"<w:p[\s>].*?</w:p>",
            m =>
            {
                var paraXml = m.Value;

                if (!ExtractPlainText(paraXml).Contains(placeholder, StringComparison.Ordinal))
                    return paraXml;

                if (values.Count == 0) return "";

                var pPrMatch = Regex.Match(paraXml, @"<w:pPr>.*?</w:pPr>", RegexOptions.Singleline);
                var pPr = pPrMatch.Success ? pPrMatch.Value : "";

                // Limpiar la herencia de estilos del pPr para que nuestro rPr explícito
                // tenga precedencia absoluta sobre fuente y tamaño:
                //  · Eliminar <w:pStyle> → evita que el estilo de párrafo aporte fuente/color vía tema
                //  · Eliminar <w:rPr> interior → elimina el rPr del marcador de ¶ del placeholder
                var pPrClean = pPr;
                if (!string.IsNullOrEmpty(pPrClean))
                {
                    pPrClean = Regex.Replace(pPrClean, @"<w:pStyle\b[^>]*/?>",    "", RegexOptions.Singleline);
                    pPrClean = Regex.Replace(pPrClean, @"<w:rPr>.*?</w:rPr>", "", RegexOptions.Singleline);
                }

                var separatorPPr = Regex.Replace(pPrClean, @"<w:numPr>.*?</w:numPr>", "", RegexOptions.Singleline);

                var sb = new StringBuilder();
                for (int vi = 0; vi < values.Count; vi++)
                {
                    var (text, bold) = values[vi];
                    var itemRPr = bold ? ForceBold(baseRPr) : StripBold(baseRPr);
                    sb.Append(BuildParagraphWithText(pPrClean, itemRPr, text));
                    if (vi < values.Count - 1)
                        sb.Append($"<w:p>{separatorPPr}</w:p>");
                }

                return sb.ToString();
            },
            RegexOptions.Singleline);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers de negrita sobre <w:rPr>
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Elimina <c>&lt;w:b/&gt;</c> y <c>&lt;w:bCs/&gt;</c> del bloque rPr.</summary>
    private static string StripBold(string rPr)
    {
        if (string.IsNullOrEmpty(rPr)) return rPr;
        var r = Regex.Replace(rPr, @"<w:b\b[^>]*/?>",   "");
        r     = Regex.Replace(r,   @"<w:bCs\b[^>]*/?>", "");
        return r;
    }

    /// <summary>
    /// Garantiza que <c>&lt;w:b/&gt;</c> y <c>&lt;w:bCs/&gt;</c> estén presentes
    /// en el bloque rPr, independientemente de lo que haya en la plantilla.
    /// </summary>
    private static string ForceBold(string rPr)
    {
        // Primero limpiar cualquier vestigio de bold previo y añadir el explícito
        var stripped = StripBold(rPr);
        if (string.IsNullOrEmpty(stripped))
            return "<w:rPr><w:b/><w:bCs/></w:rPr>";

        // El esquema OOXML (CT_RPr) exige un orden específico de los hijos de <w:rPr>:
        // <w:b/> debe ir DESPUÉS de <w:rStyle>/<w:rFonts> pero ANTES de <w:sz>, <w:color>, etc.
        // Insertarlo al final (antes de </w:rPr>) produce XML fuera de orden que Word Online
        // ignora, perdiéndose la negrita. Por eso lo insertamos en la posición correcta.
        int insertAt = -1;
        var mFonts = Regex.Match(stripped, @"<w:rFonts\b[^>]*>");
        if (mFonts.Success)
        {
            insertAt = mFonts.Index + mFonts.Length;
        }
        else
        {
            var mStyle = Regex.Match(stripped, @"<w:rStyle\b[^>]*>");
            if (mStyle.Success)
                insertAt = mStyle.Index + mStyle.Length;
            else
            {
                var mOpen = Regex.Match(stripped, @"<w:rPr\b[^>]*>");
                if (mOpen.Success)
                    insertAt = mOpen.Index + mOpen.Length;
            }
        }

        return insertAt >= 0
            ? stripped.Insert(insertAt, "<w:b/><w:bCs/>")
            : stripped.Replace("</w:rPr>", "<w:b/><w:bCs/></w:rPr>"); // fallback defensivo
    }

    /// <summary>
    /// Garantiza que &lt;w:u w:val="single"/&gt; (subrayado) esté presente en el rPr.
    /// En el orden de hijos de &lt;w:rPr&gt; (CT_RPr), &lt;w:u&gt; va casi al final (tras sz/color),
    /// por lo que insertarlo antes de &lt;/w:rPr&gt; es la posición válida más simple.
    /// </summary>
    private static string ForceUnderline(string rPr)
    {
        if (string.IsNullOrEmpty(rPr))
            return "<w:rPr><w:u w:val=\"single\"/></w:rPr>";

        return rPr.Contains("</w:rPr>")
            ? rPr.Replace("</w:rPr>", "<w:u w:val=\"single\"/></w:rPr>")
            : rPr;
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Emite los runs de una sola línea de texto, dividiendo por tabulaciones (\t).
    /// Cada \t produce un run con &lt;w:tab/&gt; seguido del siguiente segmento de texto.
    /// Admite marcadores de negrita inline <c>**texto**</c>: cada fragmento entre
    /// asteriscos dobles se emite con <see cref="ForceBold"/> sobre el <paramref name="rPr"/> base.
    /// </summary>
    private static void AppendLineRuns(StringBuilder sb, string rPr, string line)
    {
        // Negrita inline: si el texto contiene ** tomamos un camino alternativo
        if (line.Contains("**"))
        {
            // Dividir por ** → partes alternadas: normal, bold, normal, bold, …
            var parts = Regex.Split(line, @"\*\*");
            bool inBold = false;
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    var partRPr = inBold ? ForceBold(rPr) : rPr;
                    var spaceAttr = (part.StartsWith(' ') || part.EndsWith(' '))
                        ? " xml:space=\"preserve\""
                        : "";
                    sb.Append("<w:r>");
                    if (!string.IsNullOrEmpty(partRPr)) sb.Append(partRPr);
                    sb.Append($"<w:t{spaceAttr}>{XmlEscape(part)}</w:t>");
                    sb.Append("</w:r>");
                }
                inBold = !inBold;
            }
            return;
        }

        // Subrayado inline: si el texto contiene __ tomamos un camino análogo al de negrita.
        // (Una línea usa negrita O subrayado, no ambos a la vez, en las cláusulas generadas.)
        if (line.Contains("__"))
        {
            var parts = Regex.Split(line, @"__");
            bool inUnderline = false;
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    var partRPr = inUnderline ? ForceUnderline(rPr) : rPr;
                    var spaceAttr = (part.StartsWith(' ') || part.EndsWith(' '))
                        ? " xml:space=\"preserve\""
                        : "";
                    sb.Append("<w:r>");
                    if (!string.IsNullOrEmpty(partRPr)) sb.Append(partRPr);
                    sb.Append($"<w:t{spaceAttr}>{XmlEscape(part)}</w:t>");
                    sb.Append("</w:r>");
                }
                inUnderline = !inUnderline;
            }
            return;
        }

        // Camino habitual: separar por tabulaciones
        var segments = line.Split('\t');
        bool firstSeg = true;
        foreach (var seg in segments)
        {
            if (!firstSeg)
            {
                // Run de tabulación
                sb.Append("<w:r>");
                if (!string.IsNullOrEmpty(rPr)) sb.Append(rPr);
                sb.Append("<w:tab/>");
                sb.Append("</w:r>");
            }
            firstSeg = false;

            // Emitir el run de texto (incluso si está vacío, para no perder la posición)
            var spaceAttr = (seg.StartsWith(' ') || seg.EndsWith(' '))
                ? " xml:space=\"preserve\""
                : "";
            sb.Append("<w:r>");
            if (!string.IsNullOrEmpty(rPr)) sb.Append(rPr);
            sb.Append($"<w:t{spaceAttr}>{XmlEscape(seg)}</w:t>");
            sb.Append("</w:r>");
        }
    }

    private static string BuildParagraphWithText(string pPr, string rPr, string text)
    {
        // Normalizar saltos de línea (CRLF / CR → LF)
        var normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
        var lines = normalized.Split('\n');

        var sb = new StringBuilder();
        sb.Append("<w:p>");
        if (!string.IsNullOrEmpty(pPr)) sb.Append(pPr);

        bool firstLine = true;
        foreach (var rawLine in lines)
        {
            if (!firstLine)
            {
                // Run de salto de línea suave (Shift+Enter en Word)
                sb.Append("<w:r>");
                if (!string.IsNullOrEmpty(rPr)) sb.Append(rPr);
                sb.Append("<w:br/>");
                sb.Append("</w:r>");
            }
            firstLine = false;

            AppendLineRuns(sb, rPr, rawLine);
        }

        sb.Append("</w:p>");
        return sb.ToString();
    }

    /// <summary>
    /// Extrae el texto plano visible de un párrafo Word (concatena el contenido de
    /// todos los nodos &lt;w:t&gt;), decodificando entidades XML.
    /// </summary>
    private static string ExtractPlainText(string paraXml)
    {
        var sb = new StringBuilder();
        foreach (Match m in Regex.Matches(paraXml, @"<w:t[^>]*>([^<]*)</w:t>"))
            sb.Append(XmlUnescape(m.Groups[1].Value));
        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static string ReplaceInParagraphXml(string paraXml, Dictionary<string, string> replacements)
    {
        // Ubicar todos los <w:t> con su posición exacta dentro del XML del párrafo
        var matches = Regex.Matches(paraXml, @"<w:t(\s[^>]*)?>([^<]*)</w:t>").ToList();
        if (matches.Count == 0) return paraXml;

        // Texto decodificado de cada nodo
        var nodeTexts = matches.Select(m => XmlUnescape(m.Groups[2].Value)).ToArray();
        var combined  = string.Concat(nodeTexts);

        if (!replacements.Keys.Any(combined.Contains)) return paraXml;

        // Calcular offset de inicio de cada nodo dentro de `combined`
        var offsets = new int[nodeTexts.Length];
        for (int i = 1; i < nodeTexts.Length; i++)
            offsets[i] = offsets[i - 1] + nodeTexts[i - 1].Length;

        // Copiar los textos para poder modificarlos
        var newTexts = nodeTexts.ToArray();

        foreach (var (placeholder, value) in replacements)
        {
            int searchFrom = 0;

            while (true)
            {
                // Recalcular combined y offsets en cada pasada (los nodos pueden haber cambiado)
                var cur    = string.Concat(newTexts);
                var curOff = new int[newTexts.Length];
                for (int i = 1; i < newTexts.Length; i++)
                    curOff[i] = curOff[i - 1] + newTexts[i - 1].Length;

                int phStart = cur.IndexOf(placeholder, searchFrom, StringComparison.Ordinal);
                if (phStart < 0) break;
                int phEnd = phStart + placeholder.Length;

                // Encontrar primer y último nodo que forman el placeholder
                int firstNode = -1, lastNode = -1;
                for (int i = 0; i < newTexts.Length; i++)
                {
                    int nStart = curOff[i];
                    int nEnd   = nStart + newTexts[i].Length;
                    if (nEnd > phStart && nStart < phEnd)
                    {
                        if (firstNode < 0) firstNode = i;
                        lastNode = i;
                    }
                }
                if (firstNode < 0) break;

                // Texto antes y después del placeholder dentro de los nodos límite
                var before = cur[curOff[firstNode]..phStart];
                var after  = cur[phEnd..(curOff[lastNode] + newTexts[lastNode].Length)];

                // Fusionar: primer nodo recibe before + value + after; el resto queda vacío
                newTexts[firstNode] = before + value + after;
                for (int i = firstNode + 1; i <= lastNode; i++)
                    newTexts[i] = "";

                // Continuar búsqueda tras el valor insertado (maneja múltiples ocurrencias)
                searchFrom = curOff[firstNode] + before.Length + value.Length;
            }
        }

        // Reconstruir el XML del párrafo con los nuevos textos
        int idx = 0;
        return Regex.Replace(paraXml, @"<w:t(\s[^>]*)?>([^<]*)</w:t>", m =>
        {
            if (idx >= newTexts.Length) return m.Value;
            var attrs   = m.Groups[1].Value;
            var newText = newTexts[idx++];

            // Sin cambio → devolver original intacto
            if (newText == XmlUnescape(m.Groups[2].Value)) return m.Value;

            // Asegurar xml:space="preserve" si el texto tiene espacios al inicio/fin
            if (!attrs.Contains("xml:space") && (newText.StartsWith(' ') || newText.EndsWith(' ')))
                attrs = " xml:space=\"preserve\"" + attrs;

            return $"<w:t{attrs}>{XmlEscape(newText)}</w:t>";
        });
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Escapa únicamente los caracteres que DEBEN escaparse en contenido de texto XML:
    /// &amp; → &amp;amp;  |  &lt; → &amp;lt;  |  &gt; → &amp;gt;
    /// Las comillas dobles (") NO se escapan en contenido de texto (solo son obligatorias
    /// dentro de valores de atributos). Escaparlas en texto produce &amp;quot; que Word
    /// puede re-fragmentar los nodos &lt;w:t&gt; al volver a leer el archivo.
    /// </summary>
    private static string XmlEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    /// <summary>
    /// Escapa una cadena para usarla como VALOR DE ATRIBUTO XML (p. ej. el <c>Target</c> de una
    /// relación). A diferencia de <see cref="XmlEscape"/>, aquí sí hay que escapar la comilla doble
    /// porque el valor va entre comillas. Las URLs de SharePoint traen <c>&amp;</c> sin escapar.
    /// </summary>
    private static string XmlAttrEscape(string s) =>
        s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    /// <summary>
    /// Convierte referencias de entidad XML y referencias numéricas de carácter
    /// a sus caracteres correspondientes.
    /// </summary>
    private static string XmlUnescape(string s)
    {
        // Entidades nombradas
        s = s.Replace("&quot;", "\"")
             .Replace("&apos;", "'")
             .Replace("&lt;",   "<")
             .Replace("&gt;",   ">")
             .Replace("&amp;",  "&");   // &amp; siempre al final para no doble-desescapar

        // Referencias numéricas decimales comunes (Word las usa a veces)
        s = Regex.Replace(s, @"&#(\d+);", m =>
            char.ConvertFromUtf32(int.Parse(m.Groups[1].Value)));

        // Referencias numéricas hexadecimales
        s = Regex.Replace(s, @"&#x([0-9A-Fa-f]+);", m =>
            char.ConvertFromUtf32(Convert.ToInt32(m.Groups[1].Value, 16)));

        return s;
    }
}
