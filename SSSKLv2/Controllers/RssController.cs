using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using SSSKLv2.Services.Interfaces;

namespace SSSKLv2.Controllers;

[Route("feed")]
public class RssController(IProductService productService) : ControllerBase
{
    [ResponseCache(Duration = 1200)]
    [HttpGet]
    [Route("products.rss")]
    public async Task<IActionResult> GetProductsFeed()
    {
        var products = await productService.GetAllAvailable();
        var url = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
        
        var feed = new SyndicationFeed(
            "Producten",
            "Alle beschikbare producten, met prijs en inventaris",
            new Uri(url))
        {
            Items = products.Select((e) => new SyndicationItem(
                e.Name,
                $"{e.Price.ToString("C")} ({e.Stock} stuks)",
                new Uri(url)
            ))
        };

        var settings = new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            NewLineHandling = NewLineHandling.Entitize,
            NewLineOnAttributes = true,
            Indent = true,
            Async = true,
        };

        using var stream = new MemoryStream();
        await using var xmlWriter = XmlWriter.Create(stream, settings);
        var rssFormatter = new Rss20FeedFormatter(feed, false);
        rssFormatter.WriteTo(xmlWriter);
        await xmlWriter.FlushAsync();

        return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
    }
}