namespace Abril_Backend.Features.SsomaModule.PetsFeature.Application.Dtos;

// "Indice" es la posición ORIGINAL del párrafo en el documento Word (no cambia si
// se filtra o recorta la lista) — se usa como identificador estable para que
// "ParentIndice" pueda referenciar a otro elemento de la misma respuesta, incluso
// después de que el usuario borre o edite filas en la vista previa.
public class ImportPasoPreviewDto
{
    public int Indice { get; set; }
    public int? ParentIndice { get; set; }
    public string Tipo { get; set; } = "paso"; // subtitulo | paso | letra | guion
    public string Texto { get; set; } = string.Empty;
    public string? ImagenBase64 { get; set; }
}

public class PetsImportPreviewDto
{
    public bool SeccionEncontrada { get; set; }

    // Procedimiento y Responsabilidades — las únicas dos secciones en árbol.
    public Dictionary<string, List<ImportPasoPreviewDto>> SeccionesArbol { get; set; } = [];

    // Introducción/Alcance/Objetivo/Definiciones/Restricciones — texto ya concatenado
    // por sección, listo para revisar/editar antes de guardar.
    public Dictionary<string, string> SeccionesTexto { get; set; } = [];

    // Todos los párrafos con contenido del documento, en orden, con el mismo tipo/
    // jerarquía ya detectados — respaldo cuando no se detecta NINGÚN título de
    // sección conocido, o el usuario prefiere elegir el rango a mano.
    public List<ImportPasoPreviewDto> TodosLosParrafos { get; set; } = [];
}

public class ImportPasoConfirmDto
{
    public int Indice { get; set; }
    public int? ParentIndice { get; set; }
    public string Tipo { get; set; } = "paso";
    public string Texto { get; set; } = string.Empty;
    public string? ImagenBase64 { get; set; }
}

public class ConfirmarImportacionRequest
{
    public Dictionary<string, List<ImportPasoConfirmDto>> SeccionesArbol { get; set; } = [];
    public Dictionary<string, string> SeccionesTexto { get; set; } = [];

    // true: antes de insertar, desactiva los pasos vigentes de cada sección en árbol
    // presente en SeccionesArbol — para cuando se vuelve a importar una versión
    // corregida del mismo documento. false (default): agrega al final, como antes.
    // No aplica a SeccionesTexto: esas siempre se sobrescriben (es un solo bloque).
    public bool Reemplazar { get; set; }
}
