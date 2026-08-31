namespace Abril_Backend.Shared.Services.SharePoint.Dtos
{
    public class SharePointUploadResultDto
    {
        public string? WebUrl { get; init; }
        public string? ItemId { get; init; }
        /// <summary>Nombre de archivo realmente usado al subir (puede diferir del solicitado si hubo renombrado por conflicto).</summary>
        public string? FileName { get; init; }
    }
}
