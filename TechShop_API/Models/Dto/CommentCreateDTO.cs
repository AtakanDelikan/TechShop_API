using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models.Dto
{
    public class CommentCreateDTO
    {
        public int ProductId { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }
    }
}
