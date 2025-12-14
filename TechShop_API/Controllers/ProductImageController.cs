using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using TechShop_API.Models;
using TechShop_API.Models.Dto;
using TechShop_API.Services;
using TechShop_API.Services.Interfaces;
using TechShop_API.Utility;

[Route("api/[controller]")]
[ApiController]
public class ProductImageController : ControllerBase
{
    private readonly IProductImageService _imageService;

    public ProductImageController(IProductImageService imageService)
    {
        _imageService = imageService;
    }

    [HttpPost("upload-multiple")]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<ActionResult<ApiResponse>> UploadMultiple(
        [FromForm] ProductImagesUploadDTO dto)
    {
        var response = new ApiResponse();

        try
        {
            var images = await _imageService.UploadImagesAsync(dto.ProductId, dto.Files);
            response.Result = images;
            response.StatusCode = HttpStatusCode.Created;
        }
        catch (Exception ex)
        {
            response.IsSuccess = false;
            response.StatusCode = HttpStatusCode.BadRequest;
            response.ErrorMessages.Add(ex.Message);
        }

        return response;
    }

    [HttpDelete("{imageId:int}")]
    [Authorize(Roles = SD.Role_Admin)]
    public async Task<ActionResult<ApiResponse>> Delete(int imageId)
    {
        var response = new ApiResponse();

        bool deleted = await _imageService.DeleteImageAsync(imageId);

        if (!deleted)
        {
            response.IsSuccess = false;
            response.StatusCode = HttpStatusCode.NotFound;
            response.ErrorMessages.Add("Image not found.");
            return response;
        }

        response.StatusCode = HttpStatusCode.OK;
        return response;
    }
}
