using Microsoft.AspNetCore.Mvc;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/v1/products/{productId:int}/images")]
public class ProductImagesController : ControllerBase
{
    // GET: api/v1/products/5/images
    [HttpGet]
    public IActionResult GetAll(int productId)
    {
        return Ok($"Get images for product {productId}");
    }

    // GET: api/v1/products/5/images/2
    [HttpGet("{imageId:int}")]
    public IActionResult GetById(
        int productId,
        int imageId)
    {
        return Ok(
            $"Get image {imageId} for product {productId}");
    }

    // POST: api/v1/products/5/images
    [HttpPost]
    public IActionResult Create(int productId)
    {
        return Ok(
            $"Add image to product {productId}");
    }

    // DELETE: api/v1/products/5/images/2
    [HttpDelete("{imageId:int}")]
    public IActionResult Delete(
        int productId,
        int imageId)
    {
        return Ok(
            $"Delete image {imageId} from product {productId}");
    }
}