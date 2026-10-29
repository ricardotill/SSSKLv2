using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data.DAL.Interfaces;

namespace SSSKLv2.Data.DAL;

public class PushSubscriptionRepository(ApplicationDbContext dbContext) : IPushSubscriptionRepository
{
    public async Task Upsert(PushSubscription subscription)
    {
        var existing = await dbContext.PushSubscription
            .FirstOrDefaultAsync(s => s.UserId == subscription.UserId && s.Endpoint == subscription.Endpoint);

        if (existing == null)
        {
            dbContext.PushSubscription.Add(subscription);
        }
        else
        {
            existing.P256dh = subscription.P256dh;
            existing.Auth = subscription.Auth;
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task Delete(string userId, string endpoint)
    {
        var subscription = await dbContext.PushSubscription
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Endpoint == endpoint);

        if (subscription != null)
        {
            dbContext.PushSubscription.Remove(subscription);
            await dbContext.SaveChangesAsync();
        }
    }
}
