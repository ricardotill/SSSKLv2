namespace SSSKLv2.Dto.Api.v1;

public class ApplicationUserDetailedDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberConfirmed { get; set; }

    // Personal name fields
    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string FullName { get; set; } = string.Empty;

    // Application-specific fields
    public decimal Saldo { get; set; }
    public DateTime LastOrdered { get; set; }

    // Profile picture URL (nullable)
    public string? ProfilePictureUrl { get; set; }

    // User Profile description
    public string? Description { get; set; }

    // Roles assigned to the user
    public IList<string> Roles { get; set; } = new List<string>();
}

