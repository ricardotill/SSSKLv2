namespace SSSKLv2.Dto.Api.v1;

public class ApplicationUserUpdateDto
{
    public string? Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    
    public bool EmailConfirmed { get; set; }
    public string? PhoneNumber { get; set; }
    
    public bool PhoneNumberConfirmed { get; set; }

    // Personal name fields
    public string? Name { get; set; }
    public string? Surname { get; set; }

    // Acceptable to include password on update, but it will be handled securely by Identity
    // If null or empty, password will not be changed.
    public string? Password { get; set; }
}

