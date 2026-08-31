using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Infrastructure.Services
{
    public class LocalFileStorageService : IFileStorageService
    {
        private readonly IWebHostEnvironment _env;

        public LocalFileStorageService(IWebHostEnvironment env)
        {
            _env = env;
        }
        public async Task<List<string>> UploadFilesAsync(
            IEnumerable<(Stream Stream, string FileName)> files,
            string containerName)
        {
            var results = new List<string>();

            var baseFolder = Path.Combine(_env.WebRootPath, "images", containerName);

            Directory.CreateDirectory(baseFolder);

            foreach (var file in files)
            {
                var uniqueName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

                var physicalPath = Path.Combine(baseFolder, uniqueName);

                using (var outputStream = new FileStream(
                    physicalPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true))
                {
                    await file.Stream.CopyToAsync(outputStream);
                }

                var relativePath = $"/images/{containerName}/{uniqueName}";
                results.Add(relativePath);
            }

            return results;
        }
    }
}