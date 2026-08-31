namespace Abril_Backend.Features.CostsModule.Shared.Models
{
    /// <summary>
    /// Ids de la tabla project_adjudicacion_folder_type. Un proyecto puede tener una carpeta viva por tipo.
    /// </summary>
    public static class AdjudicacionFolderTypes
    {
        /// <summary>Carpeta bajo "07_OT/BACK UP PROYECTO" (solo Oficina Central). Recibe únicamente el contrato firmado escaneado del paso 7.</summary>
        public const int BackupProyecto = 1;

        /// <summary>Carpeta bajo "04_OBRAS" (visible también para Oficina Técnica). Recibe todos los documentos de la adjudicación.</summary>
        public const int Obras = 2;
    }
}
