using Microsoft.AspNetCore.Mvc;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    // GET: api/v1/products
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("Get all products");
    }

    // GET: api/v1/products/5
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        return Ok($"Get product with ID: {id}");
    }

    // POST: api/v1/products
    [HttpPost]
    public IActionResult Create()
    {
        return Ok("Create product");
    }

    // PUT: api/v1/products/5
    [HttpPut("{id:int}")]
    public IActionResult Update(int id)
    {
        return Ok($"Update product with ID: {id}");
    }

    // DELETE: api/v1/products/5
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        return Ok($"Delete product with ID: {id}");
    }
}