namespace Abril_Backend.Features.CostsModule.Features.Configuration.AdjudicacionFolderFeature.Application.Dtos
{
    public class AdjudicacionFolderDto
    {
        public int ProjectAdjudicacionFolderId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
        public int FolderTypeId { get; set; }
        public string FolderTypeDescription { get; set; } = null!;
        public string LinkUrl { get; set; } = null!;
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
        public string? FolderName { get; set; }
        public string? WebUrl { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
    }

    public class AdjudicacionFolderCreateDto
    {
        public int ProjectId { get; set; }
        /// <summary>Tipo de raíz base ("07_OT/BACK UP PROYECTO" o "04_OBRAS").</summary>
        public int FolderTypeId { get; set; }
        public string LinkUrl { get; set; } = null!;
        /// <summary>Carpeta seleccionada en el navegador (debe estar dentro del drive del link).</summary>
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
    }

    public class AdjudicacionFolderUpdateDto
    {
        public int ProjectAdjudicacionFolderId { get; set; }
        public string LinkUrl { get; set; } = null!;
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
        public bool Active { get; set; }
    }

    // ── Navegador de carpetas ────────────────────────────────────────────────
    public class FolderItemDto
    {
        public string Id { get; set; } = null!;
        public string Name { get; set; } = null!;
    }

    public class ResolveLinkRequestDto
    {
        public string LinkUrl { get; set; } = null!;
    }

    public class FolderBrowseDto
    {
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
        public string? FolderName { get; set; }
        public List<FolderItemDto> Folders { get; set; } = new();
    }

    public class AdjudicacionFolderFilterDto
    {
        public int? ProjectId { get; set; }
        public int Page { get; set; } = 1;
    }

    public class ProjectSimpleDto
    {
        public int ProjectId { get; set; }
        public string ProjectDescription { get; set; } = null!;
    }

    public class FolderTypeSimpleDto
    {
        public int FolderTypeId { get; set; }
        public string FolderTypeDescription { get; set; } = null!;
    }

    public class AdjudicacionFolderFormDataDto
    {
        public List<ProjectSimpleDto> Projects { get; set; } = new();
        public List<FolderTypeSimpleDto> FolderTypes { get; set; } = new();
    }
}
