public interface IBlobService
{
    Task<string> UploadAsync(IFormFile file);
    Task<bool> DeleteAsync(string blobUrl);
}
