using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Mvc;
using Passerelle.Host.Interfaces;

namespace Passerelle.Host.Controllers;

[Route("feed.xml")]
public sealed class RssController(ILinkRepository linkRepository) : Controller
{
    public async Task<IActionResult> Index()
    {
        var (items, _)
            = await linkRepository.SearchAsync(null, null, "newest", 1, 50);

        var feed = new SyndicationFeed(
            "Passerelle",
            "Derniers liens partagés sur Passerelle",
            new Uri($"{Request.Scheme}://{Request.Host}/"))
        {
            Items = items.Select(link => new SyndicationItem(
                link.Title,
                link.Description,
                new Uri(link.Url),
                link.Id.ToString(),
                link.CreatedAt.ToDateTimeOffset())
            {
                PublishDate = link.CreatedAt.ToDateTimeOffset()
            })
        };

        using var stream = new MemoryStream();
        await using (var xmlWriter =
                     XmlWriter.Create(stream, new XmlWriterSettings { Async = true, Encoding = Encoding.UTF8 }))
        {
            new Rss20FeedFormatter(feed).WriteTo(xmlWriter);
        }

        return File(stream.ToArray(), "application/rss+xml; charset=utf-8");
    }
}