namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    /// <summary>
    /// Un nodo del árbol de áreas (<c>area_scope</c>) tal como lo necesita el formulario de
    /// trabajadores: lo del árbol para armar los desplegables en cascada, la equivalencia legacy
    /// que se guardará si se elige el nodo, y los revisores que le tocarían al trabajador.
    ///
    /// Todo viene resuelto por el backend para que el formulario no tenga que replicar ninguna
    /// regla ni pedir nada más al cambiar de área.
    /// </summary>
    public class AreaArbolNodoDto
    {
        public int AreaScopeId { get; set; }
        public int? AreaScopeParentId { get; set; }
        public string AreaItemName { get; set; } = string.Empty;
        /// <summary>"Área de Gerencia" / "Área Estándar".</summary>
        public string AreaTypeName { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }

        /// <summary>Equivalencia legacy que quedará en workers.area / .subarea / .jefatura.</summary>
        public string? Area { get; set; }
        public string? Subarea { get; set; }
        public string? Jefatura { get; set; }

        /// <summary>
        /// Revisores que le tocarían a un trabajador de este nodo sin considerar su proyecto, EN
        /// ORDEN de resolución: primero los de este nodo por prioridad, después los de sus áreas
        /// superiores y al final el área de GTH como último recurso.
        ///
        /// Va la lista y no solo el primero porque el formulario tiene que descartar al propio
        /// trabajador que está editando: los jefes de área son el revisor de su propia área, así
        /// que sin eso se verían como su propio jefe. El formulario muestra el primer candidato que
        /// no sea él (normalmente el revisor de la gerencia de la que cuelga su área).
        /// </summary>
        public List<AreaArbolRevisorDto> Revisores { get; set; } = new();

        /// <summary>
        /// Revisores por proyecto, solo para las áreas configuradas como "filtrar por proyecto".
        /// Si el proyecto elegido en el formulario está acá, esta lista manda sobre la de arriba.
        /// </summary>
        public List<AreaArbolRevisorProyectoDto> RevisoresPorProyecto { get; set; } = new();
    }

    /// <summary>
    /// Un candidato a revisor. <see cref="WorkerId"/> y <see cref="PersonId"/> son lo que el
    /// formulario compara para saber si el candidato es el trabajador que se está editando —
    /// por persona, porque un reingreso deja varias fichas en <c>workers</c> para la misma
    /// persona. Ambos van en null cuando el candidato es el área de GTH (el fallback).
    /// </summary>
    public class AreaArbolRevisorDto
    {
        public int? WorkerId { get; set; }
        public int? PersonId { get; set; }
        public string? Nombre { get; set; }
        public string? Email { get; set; }
    }

    public class AreaArbolRevisorProyectoDto
    {
        public int ProyectoId { get; set; }
        public List<AreaArbolRevisorDto> Revisores { get; set; } = new();
    }
}
