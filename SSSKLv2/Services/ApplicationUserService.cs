using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using SSSKLv2.Dto;
using SSSKLv2.Services.Interfaces;
using SSSKLv2.Agents;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace SSSKLv2.Services;

public class ApplicationUserService(
    IApplicationUserRepository applicationUserRepository,
    IProductRepository productRepository,
    UserManager<ApplicationUser> userManager,
    IBlobStorageAgent blobAgent,
    ApplicationDbContext context,
    ILogger<ApplicationUserService> logger) : IApplicationUserService
{
    public Task<int> GetCount() => applicationUserRepository.GetCount();
    public Task<int> GetCountAdmin() => applicationUserRepository.GetCountAll();
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
    public async Task<IList<ApplicationUser>> GetAllUsersAdmin(int skip, int take)
    {
        logger.LogInformation("{GetType}: Get All Users Admin paged Skip={Skip} Take={Take}", GetType(), skip, take);
        return await applicationUserRepository.GetAllForAdminPaged(skip, take);
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
            if (count > 0)
            {
                leaderboard.Add(new LeaderboardEntryDto()
                {
                    Amount = count,
                    FullName = $"{u.Name} {u.Surname.First()}",
                    ProductName = product.Name,
                    ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null
                });
            }
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
            
            if (count > 0)
            {
                leaderboard.Add(new LeaderboardEntryDto()
                {
                    Amount = count,
                    FullName = $"{u.Name} {u.Surname.First()}",
                    ProductName = product.Name,
                    ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null
                });
            }
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
            if (count > 0)
            {
                leaderboard.Add(new LeaderboardEntryDto()
                {
                    Amount = count,
                    FullName = $"{u.Name} {u.Surname.First()}",
                    ProductName = product.Name,
                    ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null
                });
            }
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
            if (count > 0)
            {
                leaderboard.Add(new LeaderboardEntryDto()
                {
                    Amount = count,
                    FullName = $"{u.Name} {u.Surname.First()}",
                    ProductName = product.Name,
                    ProfilePictureUrl = u.ProfileImageId != null ? $"/api/v1/blob/profilepicture/image/{u.ProfileImageId}" : null
                });
            }
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

        // Handle role changes
        if (dto.Roles != null && dto.Roles.Count > 0)
        {
            // Get current roles and remove all of them
            var currentRoles = await userManager.GetRolesAsync(user);
            if (currentRoles.Count > 0)
            {
                var removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    throw new InvalidOperationException(string.Join(";", removeResult.Errors.Select(e => e.Description)));
            }

            // Add new roles
            var addResult = await userManager.AddToRolesAsync(user, dto.Roles);
            if (!addResult.Succeeded)
                throw new InvalidOperationException(string.Join(";", addResult.Errors.Select(e => e.Description)));
        }

        // Return fresh user data
        var updated = await userManager.FindByIdAsync(id);
        return updated ?? user;
    }

    public async Task DeleteUser(string id)
    {
        logger.LogInformation("{GetType}: Delete User with ID {Id}", GetType(), id);

        var user = await userManager.FindByIdAsync(id);
        if (user == null) throw new Data.DAL.Exceptions.NotFoundException("ApplicationUser not found");

        var deleteResult = await userManager.DeleteAsync(user);
        if (!deleteResult.Succeeded)
            throw new InvalidOperationException(string.Join(";", deleteResult.Errors.Select(e => e.Description)));
    }

    public async Task<IList<string>> GetUserRoles(string userId)
    {
        logger.LogInformation("{GetType}: Get roles for user with ID {UserId}", GetType(), userId);
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) throw new Data.DAL.Exceptions.NotFoundException("ApplicationUser not found");
        return await userManager.GetRolesAsync(user);
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

    public async Task UpdateProfilePictureAsync(string userId, Stream imageStream, string contentType)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) throw new Data.DAL.Exceptions.NotFoundException("User not found");

        // Resize and crop to 400x400 using ImageSharp
        using var image = await Image.LoadAsync(imageStream);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(400, 400),
            Mode = ResizeMode.Crop
        }));

        using var outStream = new MemoryStream();
        // Save as PNG for consistency, or keep original if preferred. Let's stick to PNG for 400x400 avatars.
        await image.SaveAsPngAsync(outStream);
        outStream.Position = 0;

        var fileName = $"profile-{userId}-{Guid.NewGuid()}.png";
        var blobItem = await blobAgent.UploadFileToBlobAsync(fileName, "image/png", outStream);

        // Delete old image if exists
        if (user.ProfileImageId != null)
        {
            await DeleteProfilePictureAsync(userId);
        }

        var userImage = UserImage.ToUserImage(blobItem);
        userImage.User = user;
        
        context.UserImage.Add(userImage);
        user.ProfileImageId = userImage.Id;
        
        await context.SaveChangesAsync();
        await userManager.UpdateAsync(user);
    }

    public async Task DeleteProfilePictureAsync(string userId)
    {
        var user = await context.Users
            .Include(u => u.ProfileImage)
            .FirstOrDefaultAsync(u => u.Id == userId);
            
        if (user?.ProfileImage != null)
        {
            await blobAgent.DeleteFileToBlobAsync(user.ProfileImage.FileName);
            context.UserImage.Remove(user.ProfileImage);
            user.ProfileImageId = null;
            await context.SaveChangesAsync();
            await userManager.UpdateAsync(user);
        }
    }
}