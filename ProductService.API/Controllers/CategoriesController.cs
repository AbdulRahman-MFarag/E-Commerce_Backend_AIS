using Microsoft.AspNetCore.Mvc;

namespace ProductService.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    // GET: api/v1/categories
    [HttpGet]
    public IActionResult GetAll()
    {
        return Ok("Get all categories");
    }

    // GET: api/v1/categories/5
    [HttpGet("{id:int}")]
    public IActionResult GetById(int id)
    {
        return Ok($"Get category with ID: {id}");
    }

    // POST: api/v1/categories
    [HttpPost]
    public IActionResult Create()
    {
        return Ok("Create category");
    }

    // PUT: api/v1/categories/5
    [HttpPut("{id:int}")]
    public IActionResult Update(int id)
    {
        return Ok($"Update category with ID: {id}");
    }

    // DELETE: api/v1/categories/5
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id)
    {
        return Ok($"Delete category with ID: {id}");
    }
}