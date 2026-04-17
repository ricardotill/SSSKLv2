using Lib.Net.Http.WebPush;
using System.Runtime.CompilerServices;
using Lib.Net.Http.WebPush.Authentication;
using Microsoft.EntityFrameworkCore;
using SSSKLv2.Data;
using SSSKLv2.Services.Interfaces;
using Newtonsoft.Json;

[assembly: InternalsVisibleTo("SSSKLv2.Test")]
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace SSSKLv2.Services;

public class WebPushService : IWebPushService
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly PushServiceClient _pushServiceClient;
    private readonly ILogger<WebPushService> _logger;

    public WebPushService(IConfiguration configuration, ApplicationDbContext context, HttpClient httpClient, ILogger<WebPushService> logger)
    {
        _configuration = configuration;
        _context = context;
        _logger = logger;
        
        _pushServiceClient = new PushServiceClient(httpClient);
        
        var publicKey = _configuration["VAPID_PUBLIC_KEY"] ?? _configuration["VapidDetails:PublicKey"];
        var privateKey = _configuration["VAPID_PRIVATE_KEY"] ?? _configuration["VapidDetails:PrivateKey"];
        var subject = _configuration["VAPID_SUBJECT"] ?? _configuration["VapidDetails:Subject"] ?? "mailto:webmaster@scoutingwilo.nl";

        if (!string.IsNullOrEmpty(publicKey) && !string.IsNullOrEmpty(privateKey))
        {
            _pushServiceClient.DefaultAuthentication = new VapidAuthentication(publicKey, privateKey)
            {
                Subject = subject
            };
        }
    }

    public async Task SendNotificationAsync(string userId, string title, string message, string? url = null)
    {
        var subscriptions = await _context.PushSubscription
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (!subscriptions.Any())
        {
            return;
        }

        var payload = JsonConvert.SerializeObject(new
        {
            notification = new
            {
                title = title,
                body = message,
                icon = "/assets/icons/icon-192x192.png",
                badge = "/assets/icons/icon-72x72.png",
                vibrate = new int[] { 100, 50, 100 },
                data = new
                {
                    url = url ?? "/",
                    onActionClick = new
                    {
                        @default = new { operation = "navigateLastFocusedOrOpen", url = url ?? "/" }
                    }
                }
            }
        });

        var pushMessage = new PushMessage(payload)
        {
            Topic = SanitizeTopic(title),
            Urgency = PushMessageUrgency.Normal,
            TimeToLive = 24 * 60 * 60 // 24 hours
        };

        foreach (var subscription in subscriptions)
        {
            try
            {
                var pushSubscription = new Lib.Net.Http.WebPush.PushSubscription
                {
                    Endpoint = subscription.Endpoint,
                    Keys = new Dictionary<string, string>
                    {
                        { "p256dh", subscription.P256dh },
                        { "auth", subscription.Auth }
                    }
                };

                await RequestPushMessageAsync(pushSubscription, pushMessage);
            }
            catch (PushServiceClientException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.StatusCode == System.Net.HttpStatusCode.Gone)
                {
                    // Subscription is no longer valid, remove it
                    _context.PushSubscription.Remove(subscription);
                    _logger.LogInformation("Removing invalid push subscription for user {UserId}", userId);
                }
                else
                {
                    _logger.LogError(ex, "Error sending push notification to user {UserId}. Status: {StatusCode}", userId, ex.StatusCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error sending push notification to user {UserId}", userId);
            }
        }

        await _context.SaveChangesAsync();
    }

    private string? SanitizeTopic(string title)
    {
        if (string.IsNullOrEmpty(title))
            return null;

        // Replace spaces with hyphens as per current logic
        var input = title.Replace(" ", "-");

        // RFC 8030: Topic must be max 32 chars and only use "unreserved" characters:
        // ALPHA / DIGIT / "-" / "." / "_" / "~"
        var sanitized = new string(input
            .Where(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '-' || c == '.' || c == '_' || c == '~')
            .ToArray())
            .ToLower();

        if (string.IsNullOrEmpty(sanitized))
            return null;

        return sanitized.Length > 32 ? sanitized.Substring(0, 32) : sanitized;
    }

    internal virtual async Task RequestPushMessageAsync(Lib.Net.Http.WebPush.PushSubscription subscription, PushMessage message)
    {
        await _pushServiceClient.RequestPushMessageDeliveryAsync(subscription, message);
    }
}
