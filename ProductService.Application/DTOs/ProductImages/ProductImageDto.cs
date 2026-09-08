namespace ProductService.Application.DTOs.ProductImages;

public class ProductImageDto
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}