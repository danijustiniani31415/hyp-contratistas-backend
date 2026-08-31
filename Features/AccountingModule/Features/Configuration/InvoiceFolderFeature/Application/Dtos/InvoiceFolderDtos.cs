namespace Abril_Backend.Features.AccountingModule.Features.Configuration.InvoiceFolderFeature.Application.Dtos
{
    /// <summary>Carpeta única (singleton) configurada para guardar las facturas.</summary>
    public class InvoiceFolderDto
    {
        public int InvoiceFolderId { get; set; }
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
    public class InvoiceFolderSaveDto
    {
        public string LinkUrl { get; set; } = null!;
    }
}
