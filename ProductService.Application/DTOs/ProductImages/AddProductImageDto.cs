namespace ProductService.Application.DTOs.ProductImages;

public class AddProductImageDto
{
    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}