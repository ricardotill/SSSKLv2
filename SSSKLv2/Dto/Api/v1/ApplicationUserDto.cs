namespace SSSKLv2.Dto.Api.v1;

public class ApplicationUserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal Saldo { get; set; }
    public DateTime LastOrdered { get; set; }
}

