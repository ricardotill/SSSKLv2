using System.ComponentModel.DataAnnotations;

namespace SSSKLv2.Dto.Api.v1
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
    }

    public class ProductCreateDto
    {
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int Stock { get; set; }
    }

    public class ProductUpdateDto
    {
        [Required]
        public Guid Id { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int Stock { get; set; }
    }
}

