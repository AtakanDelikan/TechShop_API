namespace TechShop_API.Models.Dto
{
    public class ProductImageUploadDTO
    {
        public int ProductId { get; set; }
        public IFormFile File { get; set; }
    }
}
