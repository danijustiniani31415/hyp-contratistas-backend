namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs de nodos del árbol <c>area_scope</c> en PRODUCCIÓN, usados por el
    /// <see cref="Services.AreaScopeMatcher"/> para derivar el <c>area_scope_id</c> a partir
    /// de la subárea de texto plano del trabajador.
    ///
    /// ⚠️ Son IDs de producción y el árbol es administrable por UI. Si se soft-borra/mueve un
    /// nodo, o se prueba en la BD de desarrollo (desfasada), verificar que sigan vivos con la
    /// query de <see cref="Services.AreaScopeMatcher"/>.
    /// </summary>
    public static class AreaScopeIds
    {
        // ── Bajo Gerencia de Administración (54) ──
        public const int Administracion             = 55;
        public const int Contabilidad               = 56;
        public const int Finanzas                   = 57;
        public const int GestionDelTalentoHumano    = 58;
        public const int Legal                      = 59;
        public const int Logistica                  = 60;
        public const int TecnologiaDeLaInformacion  = 61;
        public const int TramitesDocumentarios      = 62;
        public const int Ventas                     = 63;

        // ── Bajo Gerencia de Marketing (16) ──
        public const int Marketing                  = 75;

        // ── Bajo Gerencia de Proyectos (17) ──
        public const int UnidadDeProyectos          = 41;
        public const int Calidad                    = 42;
        public const int Ssoma                      = 44;
        public const int CostosYPresupuestos        = 46;
        public const int PostVenta                  = 52;
        public const int Produccion                 = 76;
        public const int Residencia                 = 78;
        public const int ArquitecturaComercial      = 80;
        public const int Arquitectura               = 81;

        // ── Bajo Unidad de Proyectos (41) ──
        public const int IngenieriaBim              = 53;
        public const int PlaneamientoBim            = 73;

        // ── Destino de la subárea de catálogo "Administración Obra". ──
        //    Antes apuntaba a su hijo Obra_Oficina "Oficina Técnica" (84); ese tipo
        //    de área se eliminó y la distinción pasó a workers.obra_oficina_staff_id.
        public const int AdministracionDeObra = 83;
    }
}
