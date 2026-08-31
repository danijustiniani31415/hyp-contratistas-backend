namespace Abril_Backend.Features.Ssoma.SaludOcupacional.Application.Dtos.Emo
{
    public class EmoPorTrabajadorFilterDto
    {
        /// <summary>
        /// Una sola ficha. Lo usa el enlace desde Reclutamiento («Programar EMO de ingreso»):
        /// sin esto, la ficha del finalista podia caer en cualquier pagina de las 900+ filas y
        /// el modal se abriria sobre una lista que no lo muestra.
        /// </summary>
        public int? WorkerId { get; set; }

        /// <summary>
        /// Lista TODAS las fichas de <c>workers</c> (único filtro: <c>person.state</c>), sin
        /// exigir vinculación vigente con una empresa Abril ni un estado de ficha concreto.
        ///
        /// Lo activa SOLO Configuración → Trabajadores, que es el mantenedor del catálogo de
        /// fichas: ahí hay que poder llegar a un retirado o a una ficha sin vinculación para
        /// corregirle el puesto o el área. Sin esto, Configuración → Categorías y Puestos
        /// contaba trabajadores (cuenta todas las filas de <c>workers</c>, sin filtro) que
        /// después no se podían buscar para reasignarles el puesto.
        ///
        /// Las pantallas de EMOs (SSOMA y Clínica) NO lo mandan: para ellas un retirado o un
        /// tercero sin vinculación a Abril sigue siendo invisible.
        /// </summary>
        public bool TodasLasFichas { get; set; }

        public string? Search { get; set; }
        public string? Aptitud { get; set; }
        public string? Estado { get; set; }
        public int? EmpresaId { get; set; }
        public int? ProyectoId { get; set; }

        /// <summary>
        /// Nodo del árbol <c>area_scope</c> por el que se filtra: se incluyen los trabajadores
        /// del nodo y de todos sus descendientes. Reemplaza a los filtros de texto
        /// <see cref="Area"/>/<see cref="Subarea"/>, igual que <c>workers.area_scope_id</c>
        /// reemplazó a <c>workers.area</c>/<c>workers.subarea</c>.
        /// </summary>
        public int? AreaScopeId { get; set; }

        /// <summary>Obsoletos: los reemplaza <see cref="AreaScopeId"/> y ya no se aplican al filtrar.</summary>
        public string? Area { get; set; }
        public string? Subarea { get; set; }
        public DateOnly? FechaEmoDesde { get; set; }
        public DateOnly? FechaEmoHasta { get; set; }
        public bool SinLectura { get; set; }
        public bool SinCertificado { get; set; }
        public bool SinEmoCompleto { get; set; }

        /// <summary>Trabajadores con interconsulta derivada (no cancelada) y sin informe de levantamiento adjunto.</summary>
        public bool SinInterconsulta { get; set; }

        /// <summary>Subtab "Pendientes de lectura": EMOs marcados para que los lea el médico de
        /// Abril (RequiereLecturaAbril) y que todavía no tienen el archivo de lectura subido.</summary>
        public bool PendienteLecturaAbril { get; set; }

        /// <summary>"fechaEmo" | "fechaVencimiento". Cualquier otro valor (o null) ordena por nombre.</summary>
        public string? SortBy { get; set; }
        public bool SortDesc { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
