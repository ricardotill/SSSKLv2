using SSSKLv2.Data;

namespace SSSKLv2.Services.Interfaces;

public interface IAchievementService
{
    Task CheckOrderForAchievements(Order order);
}