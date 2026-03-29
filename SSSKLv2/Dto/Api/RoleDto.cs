namespace SSSKLv2.Dto.Api;

public class RoleDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
}
