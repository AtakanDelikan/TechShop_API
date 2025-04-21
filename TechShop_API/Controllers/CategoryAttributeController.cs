using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Utility;

namespace TechShop_API.Controllers
{
    [Route("api/CategoryAttribute")]
    [ApiController]
    public class CategoryAttributeController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public CategoryAttributeController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetCategoryAttributes()
        {
            _response.Result = _db.CategoryAttributes;
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{id:int}", Name = "GetCategoryAttribute")]
        public async Task<IActionResult> GetCategoryAttribute(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                return BadRequest(_response);
            }
            Category category = _db.Categories
                .Include(c => c.CategoryAttributes)
                .FirstOrDefault(u => u.Id == id);
            if (category == null)
            {
                _response.StatusCode = HttpStatusCode.NotFound;
                _response.IsSuccess = false;
                return NotFound(_response);
            }

            decimal maxPrice = _db.Products
                .Where(p => p.CategoryId == id)
                .Max(p => (decimal?)p.Price) ?? 0;

            _response.Result = new
            {
                Attributes = category.CategoryAttributes,
                MaxPrice = decimal.ToDouble(maxPrice)
            };
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateCategoryAttribute([FromBody] CategoryAttributeCreateDTO categoryAttributeCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    var categoryId = categoryAttributeCreateDTO.CategoryId;
                    if (_db.Categories.FirstOrDefault(u => u.Id == categoryId) == null)
                    {
                        throw new ArgumentException("Category doesn't exist");
                    }
                    if(!Enum.IsDefined(typeof(SD.DataTypeEnum), categoryAttributeCreateDTO.DataType))
                    {
                        throw new ArgumentException("Data type doesn't exist");
                    }
                    CategoryAttribute CategoryAttributeToCreate = new()
                    {
                        CategoryId = categoryAttributeCreateDTO.CategoryId,
                        AttributeName = categoryAttributeCreateDTO.AttributeName,
                        DataType = categoryAttributeCreateDTO.DataType,
                    };

                    _db.CategoryAttributes.Add(CategoryAttributeToCreate);
                    _db.SaveChanges();
                    _response.Result = CategoryAttributeToCreate;
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

        //TODO HttpPut API that updates categoryattribute
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UpdateCategoryAttribute(int id, [FromForm] CategoryAttributeUpdateDTO CategoryAttributeUpdateDTO)
        {
            try
            {
                if (!Enum.IsDefined(typeof(SD.DataTypeEnum), CategoryAttributeUpdateDTO.DataType))
                {
                    throw new ArgumentException("Data type doesn't exist");
                }
                if (ModelState.IsValid)
                {
                    if (CategoryAttributeUpdateDTO == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }

                    CategoryAttribute categoryAttributeFromDb = await _db.CategoryAttributes.FindAsync(id);
                    if (categoryAttributeFromDb == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccess = false;
                        return BadRequest();
                    }

                    categoryAttributeFromDb.AttributeName = CategoryAttributeUpdateDTO.AttributeName;

                    // if datatype is updated products will lose that attribute field
                    if (categoryAttributeFromDb.DataType != CategoryAttributeUpdateDTO.DataType)
                    {
                        var attributesToDelete = _db.ProductAttributes.Where(pa => pa.CategoryAttributeId == categoryAttributeFromDb.Id);
                        _db.ProductAttributes.RemoveRange(attributesToDelete);
                        categoryAttributeFromDb.DataType = CategoryAttributeUpdateDTO.DataType;
                    }

                    _db.CategoryAttributes.Update(categoryAttributeFromDb);
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
        public async Task<ActionResult<ApiResponse>> DeleteCategoryAttribute(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                CategoryAttribute categoryAttributeFromDb = await _db.CategoryAttributes.FindAsync(id);
                if (categoryAttributeFromDb == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    return BadRequest();
                }

                _db.CategoryAttributes.Remove(categoryAttributeFromDb);
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
