namespace Abril_Backend.Infrastructure.Interfaces
{
    public interface IFileStorageService
    {
        Task<List<string>> UploadFilesAsync(IEnumerable<(Stream Stream, string FileName)> files, string containerName);
    }
}