using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;
using TechShop_API.Data;
using TechShop_API.Models;
using TechShop_API.Models.Dto;

namespace TechShop_API.Controllers
{
    [Route("api/Laptop")]
    [ApiController]
    public class LaptopController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private ApiResponse _response;
        public LaptopController(ApplicationDbContext db)
        {
            _db = db;
            _response = new ApiResponse();
        }

        [HttpGet]
        public async Task<IActionResult> GetLaptops()
        {
            _response.Result = _db.Laptops;
            _response.StatusCode = System.Net.HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpGet("{id:int}",Name = "GetLaptop")]
        public async Task<IActionResult> GetLaptop(int id)
        {
            if (id == 0)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccsess = false;
                return BadRequest(_response);
            }
            Laptop laptop = _db.Laptops.FirstOrDefault(u=>u.Id == id);
            if (laptop == null)
            {
                _response.StatusCode=HttpStatusCode.NotFound;
                _response.IsSuccsess = false;
                return NotFound(_response);
            }
            _response.Result = laptop;
            _response.StatusCode = HttpStatusCode.OK;
            return Ok(_response);
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse>> CreateLaptop([FromForm]LaptopCreateDTO laptopCreateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    Laptop laptopToCreate = new()
                    {
                        Name = laptopCreateDTO.Name,
                        Description = laptopCreateDTO.Description,
                        Price = laptopCreateDTO.Price,
                        CPU = laptopCreateDTO.CPU,
                        GPU = laptopCreateDTO.GPU,
                        Storage = laptopCreateDTO.Storage,
                        ScreenSize = laptopCreateDTO.ScreenSize,
                        Resolution = laptopCreateDTO.Resolution,
                        Brand = laptopCreateDTO.Brand,
                        Stock = laptopCreateDTO.Stock,
                        Image = laptopCreateDTO.Image,
                    };
                    _db.Laptops.Add(laptopToCreate);
                    _db.SaveChanges();
                    _response.Result= laptopToCreate;
                    _response.StatusCode= HttpStatusCode.Created;
                    return CreatedAtRoute("GetLaptop", new {id=laptopToCreate.Id}, _response);
                }
                else
                {
                    _response.IsSuccsess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccsess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse>> UpdateLaptop(int id, [FromForm] LaptopUpdateDTO laptopUpdateDTO)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (laptopUpdateDTO == null || id != laptopUpdateDTO.Id)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccsess = false;
                        return BadRequest();
                    }

                    Laptop laptopFromDb = await _db.Laptops.FindAsync(id);
                    if (laptopFromDb == null)
                    {
                        _response.StatusCode = HttpStatusCode.BadRequest;
                        _response.IsSuccsess = false;
                        return BadRequest();
                    }

                    laptopFromDb.Name = laptopUpdateDTO.Name;
                    laptopFromDb.Description = laptopUpdateDTO.Description;
                    laptopFromDb.Price = laptopUpdateDTO.Price;
                    laptopFromDb.CPU = laptopUpdateDTO.CPU;
                    laptopFromDb.GPU = laptopUpdateDTO.GPU;
                    laptopFromDb.Storage = laptopUpdateDTO.Storage;
                    laptopFromDb.ScreenSize = laptopUpdateDTO.ScreenSize;
                    laptopFromDb.Resolution = laptopUpdateDTO.Resolution;
                    laptopFromDb.Brand = laptopUpdateDTO.Brand;
                    laptopFromDb.Stock = laptopUpdateDTO.Stock;
                    laptopFromDb.Image = laptopUpdateDTO.Image;

                    _db.Laptops.Update(laptopFromDb);
                    _db.SaveChanges();
                    _response.StatusCode = HttpStatusCode.NoContent;
                    return Ok(_response);
                }
                else
                {
                    _response.IsSuccsess = false;
                }
            }
            catch (Exception ex)
            {
                _response.IsSuccsess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteLaptop(int id)
        {
            try
            {
                if (id == 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccsess = false;
                    return BadRequest();
                }

                Laptop laptopFromDb = await _db.Laptops.FindAsync(id);
                if (laptopFromDb == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccsess = false;
                    return BadRequest();
                }

                //int miliseconds = 2000;
                //Thread.Sleep(miliseconds);

                _db.Laptops.Remove(laptopFromDb);
                _db.SaveChanges();
                _response.StatusCode = HttpStatusCode.NoContent;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccsess = false;
                _response.ErrorMessages
                    = new List<string>() { ex.ToString() };
            }

            return _response;
        }
    }
}
