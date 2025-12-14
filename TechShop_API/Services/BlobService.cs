using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace TechShop_API.Services
{
    public class BlobService : IBlobService
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public BlobService(IConfiguration config)
        {
            _connectionString = config["AzureBlob:ConnectionString"];
            _containerName = config["AzureBlob:ContainerName"];
        }

        public async Task<string> UploadAsync(IFormFile file)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            var blobClient = new BlobContainerClient(_connectionString, _containerName);
            var blob = blobClient.GetBlobClient(fileName);

            await using var stream = file.OpenReadStream();
            await blob.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });

            return blob.Uri.ToString(); // return full URL
        }

        public async Task<bool> DeleteAsync(string blobUrl)
        {
            var blobName = GetBlobNameFromUrl(blobUrl);

            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            var blobClient = containerClient.GetBlobClient(blobName);

            var result = await blobClient.DeleteIfExistsAsync();

            return result.Value;
        }

        private string GetBlobNameFromUrl(string blobUrl)
        {
            // Example blobUrl:
            // https://myaccount.blob.core.windows.net/mycontainer/folder/image.jpg

            var uri = new Uri(blobUrl);

            // uri.LocalPath -> /mycontainer/folder/image.jpg
            // Remove leading '/' and container name
            var segments = uri.LocalPath.TrimStart('/').Split('/', 2);

            if (segments.Length < 2)
                throw new ArgumentException("Invalid blob URL");

            // The rest is the blob name relative to the container
            return segments[1];
        }
    }
}
