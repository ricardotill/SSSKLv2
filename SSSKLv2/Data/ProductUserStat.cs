using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SSSKLv2.Data;

// Per user per product cache of order stats, used to avoid recomputing leaderboards from raw orders.
public class ProductUserStat : BaseModel
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = null!;

    [Required]
    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; } = null!;

    public int TotalAmount { get; set; }
    public int TotalOrders { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSpent { get; set; }
    public DateTime? LastOrderDate { get; set; }
}
