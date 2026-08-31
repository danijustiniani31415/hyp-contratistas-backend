namespace Abril_Backend.Features.GestionAdministrativa.CarpetaAdjuntos.Application.Dtos
{
    /// <summary>Carpeta única (singleton) configurada para guardar los adjuntos de las solicitudes de salida.</summary>
    public class GaAdjuntoFolderDto
    {
        public int GaAdjuntoFolderId { get; set; }
        public string LinkUrl { get; set; } = null!;
        public string DriveId { get; set; } = null!;
        public string FolderId { get; set; } = null!;
        public string? FolderName { get; set; }
        public string? WebUrl { get; set; }
        public bool Active { get; set; }
        public DateTime CreatedDateTime { get; set; }
        public int CreatedUserId { get; set; }
    }

    /// <summary>Datos para configurar/actualizar la carpeta única: solo el link pegado por el usuario.</summary>
    public class GaAdjuntoFolderSaveDto
    {
        public string LinkUrl { get; set; } = null!;
    }
}
