namespace TechShop_API.Models.Dto
{
    public class PagedResultDTO<T>
    {
        public IEnumerable<T> Products { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public IEnumerable<CategoryFacetDTO>? AvailableCategories { get; set; }
    }
}
