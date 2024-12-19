using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/ProductAttribute")]
    [ApiController]
    public class ProductAttributeController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public ProductAttributeController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetProductAttributes()
        {
            _response.Result = _db.ProductAttributes;
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(_response);
        }


        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateProductAttribute([FromBody] ProductAttributeCreateDTO productAttributeCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var productId = productAttributeCreateDTO.ProductId;
                    if (_db.Products.FirstOrDefault(u => u.Id == productId) == null)
                    {
                        throw new ArgumentException("Product doesn't exist");
                    }

                    var categoryAttributeId = productAttributeCreateDTO.CategoryAttributeId;
                    var categoryAttribute = _db.CategoryAttributes.FirstOrDefault(u => u.Id == categoryAttributeId);
                    if (categoryAttribute == null)
                    {
                        throw new ArgumentException("Category Attribute doesn't exist");
                    }

                    ProductAttribute ProductAttributeToCreate = new()
                    {
                        ProductId = productId,
                        CategoryAttributeId = categoryAttributeId,
                    };

                    // check data type
                    var dataType = categoryAttribute.DataType;

                    string value = productAttributeCreateDTO.Value;
                    switch (dataType)
                    {
                        case SD.DataTypeEnum.String:
                            {
                                ProductAttributeToCreate.String = value;
                                break;
                            }
                        case SD.DataTypeEnum.Integer:
                            {
                                if (int.TryParse(value, out int result))
                                {
                                    ProductAttributeToCreate.Integer = result;
                                }
                                else
                                {
                                    throw new ArgumentException("Value cannot be converted to integer");
                                }
                                break;
                            }
                        case SD.DataTypeEnum.Decimal:
                            {
                                if (double.TryParse(value, out double result))
                                {
                                    ProductAttributeToCreate.Decimal = result;
                                }
                                else
                                {
                                    throw new ArgumentException("Value cannot be converted to decimal");
                                }
                                break;
                            }
                        case SD.DataTypeEnum.Boolean:
                            {
                                if (productAttributeCreateDTO.Value == "true") {
                                    ProductAttributeToCreate.Boolean = true;
                                }
                                else if (productAttributeCreateDTO.Value == "false") {
                                    ProductAttributeToCreate.Boolean = false;
                                }
                                else {
                                    ProductAttributeToCreate.Boolean = null;
                                }
                                break;
                            }
                        default: break;
                    }

                    _db.ProductAttributes.Add(ProductAttributeToCreate);
                    _db.SaveChanges();
                    _response.Result = ProductAttributeToCreate;
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

        //TODO HttpPut API that updates productattribute

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteProductAttribute(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                ProductAttribute productAttributeFromDb = await _db.ProductAttributes.FindAsync(id);
                if (productAttributeFromDb == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                _db.ProductAttributes.Remove(productAttributeFromDb);
                _db.SaveChanges();
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
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
