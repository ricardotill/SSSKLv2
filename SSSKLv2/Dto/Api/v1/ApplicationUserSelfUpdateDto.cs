namespace SSSKLv2.Dto.Api.v1;

public class ApplicationUserSelfUpdateDto
{
    // Only allow changing these fields when a user updates their own profile
    public string? PhoneNumber { get; set; }
    public string? Name { get; set; }
    public string? Surname { get; set; }
}

