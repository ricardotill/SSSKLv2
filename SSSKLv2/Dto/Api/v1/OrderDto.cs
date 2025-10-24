namespace SSSKLv2.Dto.Api.v1;

public class OrderDto
{
    public Guid Id { get; set; }
    public DateTime CreatedOn { get; set; }

    public Guid? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public Guid? UserId { get; set; }
    public string UserFullName { get; set; } = string.Empty;

    public int Amount { get; set; }
    public decimal Paid { get; set; }
}

