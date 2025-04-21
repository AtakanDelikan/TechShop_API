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

                    string value = productAttributeCreateDTO.Value;

                    UpsertProductAttribute(ProductAttributeToCreate, categoryAttribute, value);


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

        private void UpsertProductAttribute(ProductAttribute productAttribute, CategoryAttribute categoryAttribute, string value)
        {
            var dataType = categoryAttribute.DataType;

            switch (dataType)
            {
                case SD.DataTypeEnum.String:
                    {
                        productAttribute.String = value;
                        if (!categoryAttribute.UniqueValues.Contains(value))
                        {
                            categoryAttribute.UniqueValues.Add(value);
                            _db.CategoryAttributes.Update(categoryAttribute);
                        }
                        break;
                    }
                case SD.DataTypeEnum.Integer:
                    {
                        if (int.TryParse(value, out int result))
                        {
                            productAttribute.Integer = result;
                            if (!categoryAttribute.Min.HasValue)
                            {
                                categoryAttribute.Min = result;
                                categoryAttribute.Max = result;
                            }
                            if (categoryAttribute.Min > result)
                            {
                                categoryAttribute.Min = result;
                            }
                            if (categoryAttribute.Max < result)
                            {
                                categoryAttribute.Max = result;
                            }
                            _db.CategoryAttributes.Update(categoryAttribute);
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
                            productAttribute.Decimal = result;
                            if (!categoryAttribute.Min.HasValue)
                            {
                                categoryAttribute.Min = result;
                                categoryAttribute.Max = result;
                            }
                            if (categoryAttribute.Min > result)
                            {
                                categoryAttribute.Min = result;
                            }
                            if (categoryAttribute.Max < result)
                            {
                                categoryAttribute.Max = result;
                            }
                            _db.CategoryAttributes.Update(categoryAttribute);
                        }
                        else
                        {
                            throw new ArgumentException("Value cannot be converted to decimal");
                        }
                        break;
                    }
                case SD.DataTypeEnum.Boolean:
                    {
                        if (value == "true")
                        {
                            productAttribute.Boolean = true;
                        }
                        else if (value == "false")
                        {
                            productAttribute.Boolean = false;
                        }
                        else
                        {
                            productAttribute.Boolean = null;
                        }
                        break;
                    }
                default: break;
            }
            return;
        }

        //TODO HttpPut and delete should modify categoryAttribute min/max or unique strings
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UpdateProductAttribute(int id, [FromForm] ProductAttributeUpdateDTO ProductAttributeUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (ProductAttributeUpdateDTO == null)
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

                    var categoryAttribute = _db.CategoryAttributes.FirstOrDefault(u => u.Id == productAttributeFromDb.CategoryAttributeId);
                    string value = ProductAttributeUpdateDTO.Value;
                    UpsertProductAttribute(productAttributeFromDb, categoryAttribute, value);

                    _db.ProductAttributes.Update(productAttributeFromDb);
                    _db.SaveChanges();
                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
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
