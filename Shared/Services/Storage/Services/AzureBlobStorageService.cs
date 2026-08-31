using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Abril_Backend.Infrastructure.Interfaces;

namespace Abril_Backend.Infrastructure.Services
{
    public class AzureBlobStorageService : IFileStorageService
    {
        private readonly BlobServiceClient _serviceClient;

        public AzureBlobStorageService(IConfiguration config)
        {
            var connectionString = config["Storage:AzureStorage:ConnectionString"];
            _serviceClient = new BlobServiceClient(connectionString);
        }

        public async Task<List<string>> UploadFilesAsync(IEnumerable<(Stream Stream, string FileName)> files, string containerName)
        {
            var container = _serviceClient.GetBlobContainerClient(containerName);

            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var tasks = files.Select(async file =>
            {
                var blobClient = container.GetBlobClient(file.FileName);

                var headers = new BlobHttpHeaders
                {
                    ContentType = GetContentType(file.FileName)
                };

                await blobClient.UploadAsync(file.Stream, new BlobUploadOptions
                {
                    HttpHeaders = headers
                });

                return blobClient.Uri.ToString();
            });

            var results = await Task.WhenAll(tasks);

            return results.ToList();
        }

        private string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }
    }
}