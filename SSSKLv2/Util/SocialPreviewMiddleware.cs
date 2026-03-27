using Microsoft.AspNetCore.Http;
using SSSKLv2.Data;
using SSSKLv2.Data.DAL.Interfaces;
using System.Text.RegularExpressions;

namespace SSSKLv2.Util;

public class SocialPreviewMiddleware(RequestDelegate next)
{
    private static readonly string[] SocialUserAgents = 
    {
        "WhatsApp",
        "WhatsAppBot",
        "facebookexternalhit",
        "Facebot",
        "Twitterbot",
        "LinkedInBot",
        "Slackbot",
        "Discordbot",
        "TelegramBot"
    };

    public async Task InvokeAsync(HttpContext context, IEventRepository eventRepository)
    {
        var userAgentHeader = context.Request.Headers.UserAgent.ToString();
        
        if (SocialUserAgents.Any(ua => userAgentHeader.Contains(ua, StringComparison.OrdinalIgnoreCase)))
        {
            var path = context.Request.Path.Value ?? "";
            
            // Look for event routes like /events/GUID
            var eventMatch = Regex.Match(path, @"^/events/([a-fA-F0-9-]{36})$", RegexOptions.IgnoreCase);
            
            if (eventMatch.Success && Guid.TryParse(eventMatch.Groups[1].Value, out var eventId))
            {
                var @event = await eventRepository.GetById(eventId);
                if (@event != null)
                {
                    await ServeEventPreview(context, @event);
                    return;
                }
            }
            
            // Generic site preview for other pages requested by crawlers
            await ServeGenericPreview(context);
            return;
        }

        await next(context);
    }

    private static async Task ServeEventPreview(HttpContext context, Event @event)
    {
        var title = $"{@event.Title} - SSSKL";
        var descriptionRaw = StripHtml(@event.Description);
        var description = descriptionRaw.Length > 160 ? descriptionRaw.Substring(0, 157) + "..." : descriptionRaw;
        var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
        var imageUrl = @event.Image?.Uri != null ? @event.Image.Uri : "";

        var html = ConstructHtml(title, description, url, imageUrl);
        
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(html);
    }

    private static async Task ServeGenericPreview(HttpContext context)
    {
        var title = "SSSKL";
        var description = "Stam stam stam...";
        var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}";
        
        var html = ConstructHtml(title, description, url, "");
        
        context.Response.ContentType = "text/html";
        await context.Response.WriteAsync(html);
    }

    private static string ConstructHtml(string title, string description, string url, string imageUrl)
    {
        // Minimal HTML with ONLY OGP tags to prevent manifest overrides or other conflicts
        var imageMeta = string.IsNullOrEmpty(imageUrl) 
            ? "" 
            : $@"<meta property=""og:image"" content=""{imageUrl}"">";

        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""utf-8"">
    <title>{title}</title>
    <meta property=""og:title"" content=""{title}"">
    <meta property=""og:description"" content=""{description}"">
    <meta property=""og:url"" content=""{url}"">
    {imageMeta}
    <meta property=""og:type"" content=""website"">
    <meta name=""twitter:card"" content=""summary_large_image"">
</head>
<body>
</body>
</html>";
    }

    private static string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return Regex.Replace(input, "<.*?>", string.Empty).Trim();
    }
}
