using TechShop_API.Models;

namespace TechShop_API.Services.Interfaces
{
    public interface IProductImageService
    {
        Task<List<ProductImage>> UploadImagesAsync(int productId, IEnumerable<IFormFile> files);
        Task<bool> DeleteImageAsync(int imageId);
    }

}
