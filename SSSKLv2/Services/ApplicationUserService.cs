using Microsoft.AspNetCore.Identity;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Services;

public class ApplicationUserService(
    IApplicationUserRepository applicationUserRepository,
    IProductRepository productRepository,
    UserManager<ApplicationUser> userManager,
    ILogger<ApplicationUserService> logger) : IApplicationUserService
{
    public Task<int> GetCount() => applicationUserRepository.GetCount();
    public async Task<ApplicationUser> GetUserById(string id)
    {
        logger.LogInformation("{GetType}: Get User with ID {Id}", GetType(), id);
        return await applicationUserRepository.GetById(id);
    }
    public async Task<ApplicationUser> GetUserByUsername(string username)
    {
        logger.LogInformation("{GetType}: Get User with username {Username}", GetType(), username);
        return await applicationUserRepository.GetByUsername(username);
    }
    public async Task<IList<ApplicationUser>> GetAllUsers()
    {
        logger.LogInformation("{GetType}: Get All Users", GetType());
        return await applicationUserRepository.GetAll();
    }
    public async Task<IList<ApplicationUser>> GetAllUsers(int skip, int take)
    {
        logger.LogInformation("{GetType}: Get All Users paged Skip={Skip} Take={Take}", GetType(), skip, take);
        return await applicationUserRepository.GetAllPaged(skip, take);
    }
    public async Task<IQueryable<ApplicationUser>> GetAllUsersObscured()
    {
        logger.LogInformation("{GetType}: Get All Users Obscured", GetType());
        var result = new List<ApplicationUser>();

        var list = await applicationUserRepository.GetAllForAdmin();

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
        var ulist = await applicationUserRepository.GetAllWithOrders();
        var product = await productRepository.GetById(productId);

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
        // create month start with explicit DateTimeKind to avoid analyzer warning
        startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);
        
        var product = await productRepository.GetById(productId);
        var ulist = await applicationUserRepository.GetAllWithOrders();
        
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
        
        var product = await productRepository.GetById(productId);
        var ulist = await applicationUserRepository.GetAllWithOrders();
        
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
        
        var product = await productRepository.GetById(productId);
        var ulist = await applicationUserRepository.GetFirst12WithOrders();
        
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

    public async Task<ApplicationUser> UpdateUser(string id, SSSKLv2.Dto.Api.v1.ApplicationUserUpdateDto dto)
    {
        logger.LogInformation("{GetType}: Update User with ID {Id}", GetType(), id);

        if (dto == null) throw new ArgumentNullException(nameof(dto));

        var user = await userManager.FindByIdAsync(id);
        if (user == null) throw new Data.DAL.Exceptions.NotFoundException("ApplicationUser not found");

        // Update username
        if (!string.IsNullOrWhiteSpace(dto.UserName) && dto.UserName != user.UserName)
        {
            var setUserNameResult = await userManager.SetUserNameAsync(user, dto.UserName);
            if (!setUserNameResult.Succeeded)
                throw new InvalidOperationException(string.Join(";", setUserNameResult.Errors.Select(e => e.Description)));
            // ensure the in-memory object reflects the change
            user.UserName = dto.UserName;
        }

        // Update email
        if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
        {
            var setEmailResult = await userManager.SetEmailAsync(user, dto.Email);
            if (!setEmailResult.Succeeded)
                throw new InvalidOperationException(string.Join(";", setEmailResult.Errors.Select(e => e.Description)));
            // ensure the in-memory object reflects the change
            user.Email = dto.Email;
        }

        // Update phone number
        if (dto.PhoneNumber != null && dto.PhoneNumber != user.PhoneNumber)
        {
            var setPhoneResult = await userManager.SetPhoneNumberAsync(user, dto.PhoneNumber);
            if (!setPhoneResult.Succeeded)
                throw new InvalidOperationException(string.Join(";", setPhoneResult.Errors.Select(e => e.Description)));
            user.PhoneNumber = dto.PhoneNumber;
        }

        // Update profile fields
        var needsUpdate = false;
        if (dto.Name != null && dto.Name != user.Name)
        {
            user.Name = dto.Name;
            needsUpdate = true;
        }
        if (dto.Surname != null && dto.Surname != user.Surname)
        {
            user.Surname = dto.Surname;
            needsUpdate = true;
        }

        if (needsUpdate)
        {
            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(string.Join(";", updateResult.Errors.Select(e => e.Description)));
        }

        // Handle password change securely using a reset token (works for admin-driven password updates)
        if (!string.IsNullOrEmpty(dto.Password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, dto.Password);
            if (!resetResult.Succeeded)
                throw new InvalidOperationException(string.Join(";", resetResult.Errors.Select(e => e.Description)));
        }

        // Return fresh user data
        var updated = await userManager.FindByIdAsync(id);
        return updated ?? user;
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