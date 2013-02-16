using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ScrapySharp.Extensions;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            DoWork().Wait();
        }

        private static async Task DoWork()
        {
            var client = new HttpClient();

            var str = await client.GetStringAsync("http://dr.dk/tv");

            var doc = new HtmlDocument();
            doc.LoadHtml(str);

            var links = doc.DocumentNode.CssSelect("a");
            var urls = links.Where(s => s.Attributes["href"] != null && s.Attributes["href"].Value.Contains("tv/se")).Select(s => s.Attributes["href"].Value).Distinct().ToList();

            foreach (var url in urls)
            {
                var linkContent = await client.GetStringAsync("http://dr.dk" + url);

                var resourceLink = Regex.Match(linkContent, @"resource:\s""([\w:\/\.]*)""").Groups[1].Value;
                if (!string.IsNullOrEmpty(resourceLink))
                {
                    var resource = await client.GetStringAsync(resourceLink);
                    var resourceId = JsonConvert.DeserializeObject<dynamic>(resource).resourceId;

                    var android = await client.GetStringAsync("http://www.dr.dk/handlers/GetResource.ashx?id=" + resourceId + "&type=android");
                    var streamInfo = JsonConvert.DeserializeObject<dynamic>(android);
                    var streamLinks = streamInfo.links;

                    Console.WriteLine(streamInfo.postingTitle);
                    Console.WriteLine(streamInfo.postingTeaser);
                    Console.WriteLine(((streamLinks as JArray).OrderByDescending(x => (x as dynamic).KbpsBitrate).First() as dynamic).uri);
                    Console.WriteLine();
                }
            }
            Console.ReadKey();
        }
    }
}
