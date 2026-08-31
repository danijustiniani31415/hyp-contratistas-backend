namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs del catálogo <c>categoria_maestra</c>, que viene de la columna CATEGORÍA de la
    /// Data Maestra de GTH. Los ids son idénticos en dev y prod (verificado el 2026-08-10:
    /// 1/2/3 con los mismos códigos), así que se pueden usar como constantes.
    ///
    /// Sustituyen a las comparaciones contra <c>workers.categoria</c>, que es texto libre y
    /// guarda el NIVEL del puesto (Operario, Peón, Oficial, Supervisor, Arquitecto…), no la
    /// categoría laboral. Los dos campos se desincronizan: un practicante que pasa a
    /// planilla conserva el texto "Practicante", y un practicante puede tener ahí su
    /// profesión. Por eso cualquier regla de negocio sobre "es practicante" debe mirar
    /// <c>Worker.CategoriaMaestraId</c> y no el texto.
    /// </summary>
    public static class CategoriaMaestraIds
    {
        public const int Empleado = 1;
        /// <summary>Practicante pre-profesional o profesional (código <c>PRACTICANTE_PRE_PRO</c>).</summary>
        public const int PracticantePrePro = 2;
        /// <summary>RCC = personal de confianza. En la Data Maestra va sin razón social.</summary>
        public const int Rcc = 3;
    }
}
