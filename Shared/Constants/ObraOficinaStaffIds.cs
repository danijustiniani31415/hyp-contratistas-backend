namespace Abril_Backend.Shared.Constants
{
    /// <summary>
    /// IDs del catálogo <c>workers_obra_oficina_staff</c>. Se insertan con id
    /// explícito en <c>Migrations_Manual/workers_obra_oficina_staff.sql</c>, así
    /// que son idénticos en dev y prod y se pueden usar como constantes.
    ///
    /// Sustituyen a las comparaciones por texto (<c>obra_oficina == "Staff"</c>)
    /// que existían antes de normalizar la columna.
    /// </summary>
    public static class ObraOficinaStaffIds
    {
        public const int Obra = 1;
        /// <summary>Staff = personal de Oficina Técnica destacado en proyecto.</summary>
        public const int Staff = 2;
        public const int OficinaCentral = 3;
        /// <summary>
        /// Personal Externo = vigilancia, mantenimiento, choferes. En la Data Maestra de GTH
        /// van con UBICACIÓN "PERSONAL EXTERNO", que no es ni la oficina principal ni un
        /// proyecto. No es personal "de escritorio", pero sí ocupa cupo de su razón social.
        /// </summary>
        public const int PersonalExterno = 4;

        /// <summary>
        /// Los dos valores que identifican personal "de escritorio" (con correo
        /// corporativo propio, Vida Ley, etc.), frente a <see cref="Obra"/>.
        /// <see cref="PersonalExterno"/> queda deliberadamente fuera: no tiene correo
        /// corporativo ni entra en los flujos de SCTR/Vida Ley, Habilitación, Control de
        /// Acceso ni Charlas, que son los que usan esta lista.
        /// </summary>
        public static readonly int[] StaffUOficinaCentral = { Staff, OficinaCentral };

        /// <summary>
        /// Quiénes ocupan cupo de la razón social en el cálculo de Reclutamiento (GTH):
        /// todos menos <see cref="Obra"/>. Es una lista aparte de
        /// <see cref="StaffUOficinaCentral"/> a propósito — significan cosas distintas y
        /// meter aquí un valor no debe cambiar el comportamiento de las otras features.
        /// </summary>
        public static readonly int[] ConsumenCupoRazonSocial =
            { Staff, OficinaCentral, PersonalExterno };

        /// <summary>Nombre del catálogo a partir del id (para DTOs y correos).</summary>
        public static string? Nombre(int? id) => id switch
        {
            Obra => "Obra",
            Staff => "Staff",
            OficinaCentral => "Oficina Central",
            PersonalExterno => "Personal Externo",
            _ => null
        };

        /// <summary>
        /// Id a partir del nombre (tolerante a mayúsculas/espacios). Se usa en los
        /// pocos puntos que todavía reciben el valor como texto (importaciones,
        /// filtros que llegan por query string).
        /// </summary>
        public static int? DesdeNombre(string? nombre)
        {
            var n = nombre?.Trim();
            if (string.IsNullOrEmpty(n)) return null;
            if (string.Equals(n, "Obra", StringComparison.OrdinalIgnoreCase)) return Obra;
            if (string.Equals(n, "Staff", StringComparison.OrdinalIgnoreCase)) return Staff;
            if (string.Equals(n, "Oficina Central", StringComparison.OrdinalIgnoreCase)) return OficinaCentral;
            if (string.Equals(n, "Personal Externo", StringComparison.OrdinalIgnoreCase)) return PersonalExterno;
            return null;
        }

        /// <summary>
        /// Nivel de riesgo del EMO aplicable según la clasificación del puesto, para
        /// decidir si un cambio de puesto es convalidable o exige un EMO nuevo:
        /// Oficina Central = riesgo bajo; Staff y Obra = riesgo alto (mismo protocolo
        /// de exámenes que un obrero, al estar destacados en proyecto).
        /// </summary>
        public static string? RiesgoEmo(int? id) => id switch
        {
            OficinaCentral => "Bajo",
            Staff or Obra => "Alto",
            _ => null
        };

        /// <summary>
        /// True cuando el cambio de puesto es de riesgo bajo a alto (Oficina Central →
        /// Staff/Obra): caso crítico que la R.M. 312-2011/MINSA no permite convalidar,
        /// porque el EMO de origen no evaluó los agentes de riesgo del puesto destino.
        /// </summary>
        public static bool EsCambioRiesgoCritico(int? origenId, int? destinoId) =>
            RiesgoEmo(origenId) == "Bajo" && RiesgoEmo(destinoId) == "Alto";
    }
}
