using Microsoft.AspNetCore.Mvc;
using Squash.Web.Areas.Public.Services.Sitemap;
using System.Xml.Linq;

namespace Squash.Web.Areas.Public.Controllers
{
    [Area("Public")]
    [Route("{culture:regex(^bg|en$)}/sitemap.xml")]
    public class SitemapController : Controller
    {
        private readonly ISitemapBuilder _sitemapBuilder;

        public SitemapController(ISitemapBuilder sitemapBuilder)
        {
            _sitemapBuilder = sitemapBuilder;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string culture, CancellationToken cancellationToken)
        {
            var entries = await _sitemapBuilder.BuildAsync(culture, cancellationToken);

            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset",
                entries.Select(entry =>
                {
                    var url = new XElement(ns + "url",
                        new XElement(ns + "loc", entry.Url));

                    if (entry.LastModifiedUtc.HasValue)
                    {
                        url.Add(new XElement(ns + "lastmod", entry.LastModifiedUtc.Value.ToString("yyyy-MM-dd")));
                    }

                    if (!string.IsNullOrWhiteSpace(entry.ChangeFrequency))
                    {
                        url.Add(new XElement(ns + "changefreq", entry.ChangeFrequency));
                    }

                    if (entry.Priority.HasValue)
                    {
                        url.Add(new XElement(ns + "priority", entry.Priority.Value.ToString("0.0")));
                    }

                    return url;
                }));

            var document = new XDocument(urlset);
            return Content(document.ToString(), "application/xml");
        }
    }
}
