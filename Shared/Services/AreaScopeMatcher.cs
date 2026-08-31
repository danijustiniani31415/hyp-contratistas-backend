using System.Globalization;
using System.Text;
using Abril_Backend.Shared.Constants;

namespace Abril_Backend.Shared.Services
{
    /// <summary>
    /// Resuelve el <c>area_scope_id</c> (árbol normalizado <c>area_scope</c>) a partir de
    /// los campos de texto plano <c>area</c>/<c>subarea</c> que sigue capturando el formulario
    /// de registro/edición de trabajadores.
    ///
    /// NO reemplaza el formulario: es un match interno. Cuando el usuario elige área+subárea
    /// (valores del catálogo <c>cat_subarea</c>), se calcula además el nodo del árbol que mejor
    /// le corresponde y se guarda en <see cref="Infrastructure.Models.Worker.AreaScopeId"/>.
    ///
    /// El mapeo se derivó de los 222 trabajadores de PRODUCCIÓN que ya tenían ambos campos
    /// poblados, cruzados contra el árbol vivo (state=true) colgado de las 3 raíces de Gerencia.
    /// La subárea es el discriminador: cada subárea del catálogo apunta a exactamente un nodo,
    /// por eso la clave es la subárea normalizada (el área queda implícita en el catálogo).
    ///
    /// ⚠️ Los IDs son de PRODUCCIÓN. Si se prueba en la BD de desarrollo hay que verificar que
    /// el árbol area_scope tenga los mismos IDs (dev suele estar desfasada). Ver query de
    /// verificación al pie de este archivo.
    /// </summary>
    public static class AreaScopeMatcher
    {
        // subárea normalizada (minúsculas, sin tildes, trim) -> area_scope_id (nodo vivo en prod).
        private static readonly Dictionary<string, int> SubareaToScope = new()
        {
            // ── Gerencia de Administración (54) ──
            ["administracion"]                 = AreaScopeIds.Administracion,
            ["contabilidad"]                   = AreaScopeIds.Contabilidad,
            ["finanzas"]                       = AreaScopeIds.Finanzas,
            ["gestion del talento humano"]     = AreaScopeIds.GestionDelTalentoHumano,
            ["legal"]                          = AreaScopeIds.Legal,
            ["logistica"]                      = AreaScopeIds.Logistica,
            ["tecnologia de la informacion"]   = AreaScopeIds.TecnologiaDeLaInformacion,
            ["tramites documentarios"]         = AreaScopeIds.TramitesDocumentarios,
            ["ventas"]                         = AreaScopeIds.Ventas,

            // ── Gerencia de Marketing (16) ──
            ["marketing"]                      = AreaScopeIds.Marketing,

            // ── Gerencia de Proyectos (17) ──
            ["arquitectura"]                   = AreaScopeIds.Arquitectura,
            ["arquitectura comercial"]         = AreaScopeIds.ArquitecturaComercial,
            ["calidad"]                        = AreaScopeIds.Calidad,
            ["costos y presupuestos"]          = AreaScopeIds.CostosYPresupuestos,
            ["post venta"]                     = AreaScopeIds.PostVenta,
            ["produccion"]                     = AreaScopeIds.Produccion,
            ["residencia"]                     = AreaScopeIds.Residencia,
            ["ssoma"]                          = AreaScopeIds.Ssoma,
            ["unidad de proyectos"]            = AreaScopeIds.UnidadDeProyectos,
            // subáreas anidadas bajo "Unidad de Proyectos" (41)
            ["ingenieria bim"]                 = AreaScopeIds.IngenieriaBim,
            ["planeamiento bim"]               = AreaScopeIds.PlaneamientoBim,
            // "Administración Obra" -> "Administración de Obra" (83). Antes apuntaba al
            // hijo Obra_Oficina "Oficina Técnica" (84); ese tipo de área ya no existe
            // (la distinción Obra/Staff/Oficina Central vive en workers.obra_oficina_staff_id).
            ["administracion obra"]            = AreaScopeIds.AdministracionDeObra,

            // "Comité" (Proyectos) existe en cat_subarea pero NO tiene nodo en area_scope -> null.
        };

        /// <summary>
        /// Mapa inverso del anterior: area_scope_id -> subárea normalizada. Es la dirección que usa
        /// hoy el formulario de trabajadores (se elige el nodo del árbol y de ahí se derivan los
        /// campos legacy area/subarea/jefatura). Se deriva del mismo diccionario para que ambas
        /// direcciones no puedan divergir; los 22 nodos son distintos, así que la inversión es 1:1.
        /// Los nodos del árbol que no están acá (gerencias, hijos Obra_Oficina, nodos nuevos) los
        /// resuelve <c>IAreaScopeLegacyResolver</c> subiendo por el árbol y cruzando por nombre
        /// contra <c>cat_subarea</c>.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, string> ScopeToSubarea =
            SubareaToScope.ToDictionary(kv => kv.Value, kv => kv.Key);

        /// <summary>
        /// Devuelve el area_scope_id que corresponde a la pareja área/subárea capturada, o
        /// <c>null</c> si no hay match (subárea vacía, obrero sin área, o subárea sin nodo como "Comité").
        ///
        /// Solo se usa para los llamadores que siguen capturando texto plano. El formulario de
        /// habilitación ya manda el nodo elegido, y en ese caso se hace el camino inverso
        /// (ver <c>IAreaScopeLegacyResolver</c>).
        /// </summary>
        public static int? Resolve(string? area, string? subarea)
        {
            var key = Normalize(subarea);
            if (key.Length == 0) return null;

            return SubareaToScope.TryGetValue(key, out var scopeId) ? scopeId : (int?)null;
        }

        /// <summary>minúsculas + sin diacríticos + trim, para tolerar "Logística"/"Logistica", espacios, etc.</summary>
        public static string Normalize(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var formD = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            foreach (var c in formD)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}

/*
-- Verificación (correr en la BD destino antes de confiar en los IDs hardcodeados):
-- confirma que cada area_scope_id del mapa siga apuntando al nodo esperado y vivo.
select s.area_scope_id, ai.area_item_name, pai.area_item_name as padre, s.state
from area_scope s
join area_item ai on ai.area_item_id = s.area_item_id
left join area_scope ps on ps.area_scope_id = s.area_scope_parent_id
left join area_item pai on pai.area_item_id = ps.area_item_id
where s.area_scope_id in (41,42,44,46,52,53,55,56,57,58,59,60,61,62,63,73,75,76,78,80,81,84)
order by s.area_scope_id;
*/
