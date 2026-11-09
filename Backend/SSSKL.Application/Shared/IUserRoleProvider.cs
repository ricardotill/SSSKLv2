namespace SSSKL.Application.Shared;

public interface IUserRoleProvider
{
    /// <summary>Returns the role names of the user; throws NotFoundException for an unknown user.</summary>
    Task<IList<string>> GetRoles(string userId);
}
