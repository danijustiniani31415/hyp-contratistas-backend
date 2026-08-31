namespace Abril_Backend.Features.Costs.Adjudicaciones.Application.Dtos
{
    /// <summary>Datos mínimos para construir la ruta de carpetas en OneDrive del proyecto.</summary>
    public class AdjudicacionPathDataDto
    {
        public int    ProjectSubContractorId { get; set; }
        public int    ProjectId             { get; set; }
        public string ProjectDescription    { get; set; } = null!;
        public string? Abbreviation         { get; set; }
        public string ContributorRuc        { get; set; } = null!;
        public string ContributorName       { get; set; } = null!;
        public string WorkItemDescription   { get; set; } = null!;
        /// <summary>Especialidad asignada a la adjudicación (puede ser null si aún no se asignó).</summary>
        public string? WorkSpecialtyDescription { get; set; }

        // ── Carpetas de OneDrive del proyecto (Configuración → Carpeta de adjudicaciones) ──
        // Tipo "04_OBRAS": destino de TODOS los documentos de la adjudicación (visible para Of. Técnica).
        /// <summary>Drive de la carpeta 04_OBRAS. Null si no está configurada.</summary>
        public string? ObrasDriveId { get; set; }
        /// <summary>ItemId de la carpeta 04_OBRAS del proyecto. Null si no está configurada.</summary>
        public string? ObrasFolderId { get; set; }

        // Tipo "07_OT/BACK UP PROYECTO": solo recibe copia del contrato firmado escaneado (paso 7).
        /// <summary>Drive de la carpeta 07_OT/BACK UP PROYECTO. Null si no está configurada.</summary>
        public string? BackupDriveId { get; set; }
        /// <summary>ItemId de la carpeta 07_OT/BACK UP PROYECTO del proyecto. Null si no está configurada.</summary>
        public string? BackupFolderId { get; set; }

        /// <summary>Nombre ya asignado a la carpeta de esta adjudicación ("ADJUDICACIÓN N° X"). Null si aún no tiene.</summary>
        public string? AdjudicacionFolderName { get; set; }
    }
}
