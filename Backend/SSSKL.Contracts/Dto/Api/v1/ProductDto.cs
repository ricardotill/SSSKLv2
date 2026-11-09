using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SSSKL.Contracts.Dto.Api.v1
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool EnableLeaderboard { get; set; }

    }

    public class ProductCreateDto
    {
        [Required]
        public required string Name { get; set; }
        public string? Description { get; set; }
        [Required, JsonRequired]
        public decimal Price { get; set; }
        [Required, JsonRequired]
        public int Stock { get; set; }
        [JsonRequired]
        public bool EnableLeaderboard { get; set; }

    }

    public class ProductUpdateDto
    {
        [Required, JsonRequired]
        public Guid Id { get; set; }
        [Required]
        public required string Name { get; set; }
        public string? Description { get; set; }
        [Required, JsonRequired]
        public decimal Price { get; set; }
        [Required, JsonRequired]
        public int Stock { get; set; }
        [JsonRequired]
        public bool EnableLeaderboard { get; set; }

    }
}

