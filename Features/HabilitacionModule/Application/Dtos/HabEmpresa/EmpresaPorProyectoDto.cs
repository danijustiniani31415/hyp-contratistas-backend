namespace Abril_Backend.Features.Habilitacion.Application.Dtos.HabEmpresa
{
    /// <summary>
    /// Una fila de la vista "Empresas por proyecto" (filtro proyecto-primero en la pantalla
    /// Empresa): estado de habilitación + entregables agrupados por responsable (SSOMA vs
    /// Administración), para el proyecto consultado.
    /// </summary>
    public class EmpresaPorProyectoDto
    {
        public int EmpresaId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool Habilitada { get; set; }
        public int EntregablesSsomaSubidos { get; set; }
        public int EntregablesSsomaFaltantes { get; set; }
        public int EntregablesAdminSubidos { get; set; }
        public int EntregablesAdminFaltantes { get; set; }
    }
}
