using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

namespace Squash.Shared
{
    public static class Downloader
    {
        private const bool EnableFileCache = true;
        private static readonly string CacheFolder = Path.Combine(AppContext.BaseDirectory, "download-cache");

        public static string Download(string url)
        {
            if (EnableFileCache && TryReadFromCache(url, out var cached))
            {
                return cached;
            }

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip
                                         | System.Net.DecompressionMethods.Deflate
                                         | System.Net.DecompressionMethods.Brotli
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Squash/1.0 (+https://example.local)");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Cache-Control", "no-cache");

            var response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            using var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult();
            using var reader = new System.IO.StreamReader(stream, System.Text.Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            var content = reader.ReadToEnd();

            if (EnableFileCache)
            {
                SaveToCache(url, content);
            }

            return content;
        }

        private static bool TryReadFromCache(string url, out string content)
        {
            content = string.Empty;
            var path = GetCachePath(url);
            if (!File.Exists(path))
            {
                return false;
            }

            content = File.ReadAllText(path, Encoding.UTF8);
            return true;
        }

        private static void SaveToCache(string url, string content)
        {
            Directory.CreateDirectory(CacheFolder);
            var path = GetCachePath(url);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static string GetCachePath(string url)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(url));
            var name = Convert.ToHexString(hash).ToLowerInvariant();
            return Path.Combine(CacheFolder, $"{name}.html");
        }
    }
}
