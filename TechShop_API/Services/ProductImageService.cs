using TechShop_API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using TechShop_API.Data;
using TechShop_API.Models;

namespace TechShop_API.Services
{
    public class ProductImageService : IProductImageService
    {
        private readonly IBlobService _blobService;
        private readonly ApplicationDbContext _db;

        public ProductImageService(IBlobService blobService, ApplicationDbContext db)
        {
            _blobService = blobService;
            _db = db;
        }

        public async Task<List<ProductImage>> UploadImagesAsync(int productId, IEnumerable<IFormFile> files)
        {
            var results = new List<ProductImage>();

            foreach (var file in files)
            {
                var url = await _blobService.UploadAsync(file);

                var img = new ProductImage
                {
                    ProductId = productId,
                    Url = url
                };

                _db.ProductImages.Add(img);
                results.Add(img);
            }

            await _db.SaveChangesAsync();
            return results;
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var img = await _db.ProductImages.FindAsync(imageId);
            if (img == null) return false;

            await _blobService.DeleteAsync(img.Url);
            _db.ProductImages.Remove(img);
            await _db.SaveChangesAsync();

            return true;
        }
    }

}
