using SSSKLv2.Components;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ApplicationUserService(
    IApplicationUserRepository _applicationUserRepository,
    ILogger<ApplicationUserService> _logger) : IApplicationUserService
{
    public async Task<ApplicationUser> GetUserById(string id)
    {
        _logger.LogInformation($"{GetType()}: Get User with ID {id}");
        return await _applicationUserRepository.GetById(id);
    }
    public async Task<ApplicationUser> GetUserByUsername(string username)
    {
        _logger.LogInformation($"{GetType()}: Get User with username {username}");
        return await _applicationUserRepository.GetByUsername(username);
    }
    public async Task<IList<ApplicationUser>> GetAllUsers()
    {
        _logger.LogInformation($"{GetType()}: Get All Users");
        return await _applicationUserRepository.GetAll();
    }
    public async Task<IQueryable<ApplicationUser>> GetAllUsersObscured()
    {
        _logger.LogInformation($"{GetType()}: Get All Users Obscured");
        var result = new List<ApplicationUser>();

        var list = await _applicationUserRepository.GetAll();

        foreach (var item in list)
        {
            ApplicationUser objApplicationUser = new ApplicationUser();

            objApplicationUser.Id = item.Id;
            objApplicationUser.UserName = item.UserName;
            objApplicationUser.Saldo = item.Saldo;
            objApplicationUser.Email = item.Email;
            objApplicationUser.Name = item.Name;
            objApplicationUser.Surname = item.Surname;
            objApplicationUser.EmailConfirmed = item.EmailConfirmed;
            objApplicationUser.PhoneNumber = item.PhoneNumber;
            objApplicationUser.PasswordHash = "*****";

            result.Add(objApplicationUser);
        }

        return result.AsQueryable();
    }

    public async Task<IEnumerable<LeaderboardEntry>> GetAllLeaderboard(Product product)
    {
        var ulist = await _applicationUserRepository.GetAllWithOrders();

        var leaderboard = new List<LeaderboardEntry>();
        foreach (var u in ulist)
        {
            int count;
            try
            {
                count = u.Orders
                    .Where(o => o.ProductNaam == product.Name)
                    .Sum(o => o.Amount);
            }
            catch (OverflowException)
            {
                count = Int32.MaxValue;
            }
            leaderboard.Add(new LeaderboardEntry() { Amount = count, FullName = $"{u.Name} {u.Surname.First()}"});
        }
        return leaderboard.OrderByDescending(x => x.Amount);
    }
    
    public async Task<IEnumerable<LeaderboardEntry>> GetMonthlyLeaderboard(Product product)
    {
        var startDate = DateTime.Now.Date;
        startDate = new DateTime(startDate.Year, startDate.Month, 1);
        var endDate = startDate.AddMonths(1);
        
        var ulist = await _applicationUserRepository.GetAllWithOrders();
        
        var leaderboard = new List<LeaderboardEntry>();
        foreach (var u in ulist)
        {
            int count;
            try
            {
                count = u.Orders
                    .Where(o => o.CreatedOn >= startDate && o.CreatedOn < endDate)
                    .Where(o => o.ProductNaam == product.Name)
                    .Sum(o => o.Amount);
            }
            catch (OverflowException)
            {
                count = Int32.MaxValue;
            }
            
            leaderboard.Add(new LeaderboardEntry() { Amount = count, FullName = $"{u.Name} {u.Surname.First()}"});
        }
        return leaderboard.OrderByDescending(x => x.Amount);
    }
}