namespace Abril_Backend.Features.Habilitacion.Application.Dtos.Catalogos
{
    /// <summary>
    /// Opcion del catalogo <c>workers_obra_oficina_staff</c>: Obra / Staff / Oficina Central.
    /// Alimenta el desplegable "Obra / Oficina" del formulario de trabajadores.
    /// </summary>
    public class ObraOficinaStaffDto
    {
        public int ObraOficinaStaffId { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
