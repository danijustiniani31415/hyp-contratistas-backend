namespace Abril_Backend.Features.GestionGthModule.Features.ReclutadoresFeature.Application.Dtos
{
    /// <summary>
    /// Una fila de la pantalla "Reclutadores" (Gestión GTH → Configuración): un trabajador del
    /// área de Gestión del Talento Humano con el interruptor que decide si sale o no en el
    /// desplegable "Responsable del proceso" del detalle de Reclutamiento.
    ///
    /// La lista se arma sola con la gente del área: no hay alta ni baja manual, así que la fila
    /// se identifica por <see cref="WorkerId"/> y no por el id de la tabla filtro (que puede no
    /// existir todavía si a ese trabajador nunca lo tocaron).
    /// </summary>
    public class ReclutadorDto
    {
        /// <summary>Ficha del trabajador en la base maestra (<c>workers.id</c>).</summary>
        public int WorkerId { get; set; }

        public string Nombre { get; set; } = "";

        /// <summary>Puesto de la ficha. Null si la ficha no tiene puesto asignado.</summary>
        public string? Puesto { get; set; }

        /// <summary>true = sale en el desplegable "Responsable del proceso".</summary>
        public bool Activo { get; set; }

        /// <summary>
        /// El trabajador ya no es del equipo de GTH (cambió de área o dejó de ser trabajador
        /// vigente) pero sigue listado porque tiene fila en la tabla filtro. Se muestra para que
        /// se pueda desactivar: si no, quedaría prendido y sin manera de apagarlo desde la UI.
        /// </summary>
        public bool FueraDelEquipo { get; set; }

        /// <summary>Área actual de la ficha. Solo se usa para explicar <see cref="FueraDelEquipo"/>.</summary>
        public string? Area { get; set; }
    }

    /// <summary>Body del interruptor de una fila.</summary>
    public class ReclutadorTogglePatchDto
    {
        public bool Activo { get; set; }
    }

    /// <summary>Respuesta del interruptor: el estado que quedó guardado, más el aviso a mostrar.</summary>
    public class ReclutadorToggleResultDto
    {
        public int WorkerId { get; set; }
        public bool Activo { get; set; }
        public string Message { get; set; } = "";
    }
}
