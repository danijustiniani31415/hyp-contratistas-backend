namespace Abril_Backend.Shared.Services.Revisores.Interfaces
{
    /// <summary>
    /// Lado de ESCRITURA del jefe personalizado de un trabajador (<c>workers_revisores</c>):
    /// el jefe elegido a mano que se sobrepone al revisor del área. Es el reemplazo de la
    /// pantalla "Revisores de Trabajadores" (/configuracion/revisor-salidas), retirada: ahora
    /// se asigna con el checkbox "Jefe personalizado" del formulario de trabajadores.
    ///
    /// El lado de LECTURA para saber a quién notificar es <see cref="IJefeRevisorResolver"/>,
    /// que aplica la cadena completa (jefe personalizado → revisor del área → GTH). Acá solo
    /// se lee el jefe personalizado en crudo, para precargar el formulario.
    ///
    /// Servicio compartido: lo usan Habilitación (detalle del trabajador y catálogo de jefes)
    /// y SSOMA · Salud Ocupacional (alta/edición del trabajador).
    /// </summary>
    public interface IJefePersonalizadoService
    {
        /// <summary>
        /// Jefe personalizado vigente de un trabajador (el de mayor prioridad entre las filas
        /// vivas y activas), o null si no tiene y por tanto le corresponde el revisor de su área.
        /// </summary>
        Task<JefePersonalizadoDto?> GetAsync(int workerId);

        /// <summary>
        /// Deja al trabajador exactamente con el jefe indicado: da de baja (soft delete) las
        /// asignaciones vivas que sobren y crea/reactiva la del revisor elegido. Con
        /// <paramref name="revisorWorkerId"/> en null se quitan todas y el trabajador vuelve a
        /// depender del revisor de su área.
        /// </summary>
        Task SetAsync(int workerId, int? revisorWorkerId);

        /// <summary>
        /// Trabajadores que pueden ser jefe: los que tienen correo corporativo @abril.pe en
        /// <c>workers.email_corporativo</c>, tengan o no usuario del sistema. Ordenados por nombre.
        /// </summary>
        Task<List<JefeCandidatoDto>> GetCandidatosAsync();
    }

    /// <summary>Jefe personalizado ya resuelto para mostrarlo en el formulario.</summary>
    public class JefePersonalizadoDto
    {
        public int WorkerId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>Opción del desplegable de jefe personalizado.</summary>
    public class JefeCandidatoDto
    {
        public int WorkerId { get; set; }
        /// <summary>
        /// Persona del candidato, para que el formulario descarte al propio trabajador aunque la
        /// ficha sea otra (una persona puede tener varias filas en <c>workers</c> por reingreso).
        /// </summary>
        public int? PersonId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
    }
}
