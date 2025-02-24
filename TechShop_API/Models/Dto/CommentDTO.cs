using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TechShop_API.Models.Dto
{
    public class CommentDTO
    {
        public int CommentId { get; set; }
        public int ProductId { get; set; }
        public string Content { get; set; }
        public int Rating { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
