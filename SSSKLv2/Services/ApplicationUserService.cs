using SSSKLv2.Components;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ApplicationUserService(
    IApplicationUserRepository _applicationUserRepository,
    IProductRepository _productRepository,
    ILogger<ApplicationUserService> _logger) : IApplicationUserService
{
    public async Task<ApplicationUser> GetUserById(string id)
    {
        _logger.LogInformation("{GetType}: Get User with ID {Id}", GetType(), id);
        return await _applicationUserRepository.GetById(id);
    }
    public async Task<ApplicationUser> GetUserByUsername(string username)
    {
        _logger.LogInformation("{GetType}: Get User with username {Username}", GetType(), username);
        return await _applicationUserRepository.GetByUsername(username);
    }
    public async Task<IList<ApplicationUser>> GetAllUsers()
    {
        _logger.LogInformation("{GetType}: Get All Users", GetType());
        return await _applicationUserRepository.GetAll();
    }
    public async Task<IQueryable<ApplicationUser>> GetAllUsersObscured()
    {
        _logger.LogInformation("{GetType}: Get All Users Obscured", GetType());
        var result = new List<ApplicationUser>();

        var list = await _applicationUserRepository.GetAllForAdmin();

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

    public async Task<IEnumerable<LeaderboardEntryDto>> GetAllLeaderboard(Guid productId)
    {
        var ulist = await _applicationUserRepository.GetAllWithOrders();
        var product = await _productRepository.GetById(productId);

        var leaderboard = new List<LeaderboardEntryDto>();
        foreach (var u in ulist)
        {
            int count;
            try
            {
                count = u.Orders
                    .Where(o => o.Product != null && o.Product.Id == product.Id)
                    .Sum(o => o.Amount);
            }
            catch (OverflowException)
            {
                count = Int32.MaxValue;
            }
            if (count > 0) leaderboard.Add(new LeaderboardEntryDto() { Amount = count, FullName = $"{u.Name} {u.Surname.First()}", ProductName = product.Name});
        }
        return DeterminePositions(leaderboard);
    }
    
    public async Task<IEnumerable<LeaderboardEntryDto>> GetMonthlyLeaderboard(Guid productId)
    {
        var startDate = DateTime.Now.Date;
        startDate = new DateTime(startDate.Year, startDate.Month, 1);
        var endDate = startDate.AddMonths(1);
        
        var product = await _productRepository.GetById(productId);
        var ulist = await _applicationUserRepository.GetAllWithOrders();
        
        var leaderboard = new List<LeaderboardEntryDto>();
        foreach (var u in ulist)
        {
            int count;
            try
            {
                count = u.Orders
                    .Where(o => o.CreatedOn >= startDate && o.CreatedOn < endDate)
                    .Where(o => o.Product != null && o.Product.Id == product.Id)
                    .Sum(o => o.Amount);
            }
            catch (OverflowException)
            {
                count = Int32.MaxValue;
            }
            
            if (count > 0) leaderboard.Add(new LeaderboardEntryDto() { Amount = count, FullName = $"{u.Name} {u.Surname.First()}", ProductName = product.Name});
        }
        return DeterminePositions(leaderboard);
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLeaderboard(Guid productId)
    {
        var time = DateTime.Now.AddHours(-12);
        
        var product = await _productRepository.GetById(productId);
        var ulist = await _applicationUserRepository.GetAllWithOrders();
        
        var leaderboard = new List<LeaderboardEntryDto>();
        foreach (var u in ulist)
        {
            int count;
            try
            {
                count = u.Orders
                    .Where(o => o.CreatedOn >= time)
                    .Where(o => o.Product != null && o.Product.Id == product.Id)
                    .Sum(o => o.Amount);
            }
            catch (OverflowException)
            {
                count = Int32.MaxValue;
            }
            if (count > 0) leaderboard.Add(new LeaderboardEntryDto() { Amount = count, FullName = $"{u.Name} {u.Surname.First()}", ProductName = product.Name});
        }
        
        return DeterminePositions(leaderboard);
    }
    
    public async Task<IEnumerable<LeaderboardEntryDto>> Get12HourlyLiveLeaderboard(Guid productId)
    {
        var time = DateTime.Now.AddHours(-12);
        
        var product = await _productRepository.GetById(productId);
        var ulist = await _applicationUserRepository.GetFirst12WithOrders();
        
        var leaderboard = new List<LeaderboardEntryDto>();
        foreach (var u in ulist)
        {
            int count;
            try
            {
                count = u.Orders
                    .Where(o => o.CreatedOn >= time)
                    .Where(o => o.Product != null && o.Product.Id == product.Id)
                    .Sum(o => o.Amount);
            }
            catch (OverflowException)
            {
                count = Int32.MaxValue;
            }
            if (count > 0) leaderboard.Add(new LeaderboardEntryDto() { Amount = count, FullName = $"{u.Name} {u.Surname.First()}", ProductName = product.Name});
        }
        
        return DeterminePositions(leaderboard);
    }

    private static IEnumerable<LeaderboardEntryDto> DeterminePositions(IEnumerable<LeaderboardEntryDto> leaderboard)
    {
        var list = leaderboard
            .OrderByDescending(x => x.Amount)
            .Take(18)
            .ToList();
        int place = 0;
        int lastAmount = 0;
        foreach (var entry in list)
        {
            if (entry.Amount != 0)
            {
                if (entry.Amount != lastAmount) place++;
                entry.Position = place;
                lastAmount = entry.Amount;
            }
            else entry.Position = 0;
        }

        return list;
    }
}