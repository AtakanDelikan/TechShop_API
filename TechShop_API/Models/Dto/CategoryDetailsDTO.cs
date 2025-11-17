namespace TechShop_API.Models.Dto
{
    public class CategoryDetailsDTO
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }

        public ParentCategoryDTO? Parent { get; set; }
    }
}
