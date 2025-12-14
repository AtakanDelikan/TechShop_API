namespace TechShop_API.Models.Dto
{
    public class ProductImagesUploadDTO
    {
        public int ProductId { get; set; }
        public List<IFormFile> Files { get; set; }
    }
}
