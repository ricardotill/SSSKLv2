namespace SSSKLv2.Data.DAL.Interfaces;

public interface IPushSubscriptionRepository
{
    // Creates the subscription, or refreshes the keys when the user already has one for the endpoint
    Task Upsert(PushSubscription subscription);
    Task Delete(string userId, string endpoint);
}
