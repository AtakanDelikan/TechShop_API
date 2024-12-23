using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Controllers
{
    [Route("api/ProductImage")]
    [ApiController]
    public class ProductImageController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public ProductImageController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateProduct([FromBody] ProductImagesCreateDTO productImageDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var productId = productImageDTO.ProductId;
                    if (_db.Products.FirstOrDefault(u => u.Id == productId) == null)
                    {
                        throw new ArgumentException("Product doesn't exist");
                    }

                    for (int i = 0; i < productImageDTO.Urls.Length; i++)
                    {
                        ProductImage productImageToCreate = new ProductImage
                        {
                            ProductId = productImageDTO.ProductId,
                            Url = productImageDTO.Urls[i],
                            DisplayOrder = i
                        };

                        _db.ProductImages.Add(productImageToCreate);
                    }
                    _db.SaveChanges();

                    _response.StatusCode = HttpStatusCode.Created;
                    return _response;
                }
                else
                {
                    _response.IsSuccess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }

    }
}
