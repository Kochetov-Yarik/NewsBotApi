using Microsoft.AspNetCore.Mvc;
using NewsBotApi.Models;
using Newtonsoft.Json;

namespace NewsBotApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NewsController : ControllerBase
    {
        [HttpGet("Search")]
        public async Task<IActionResult> SearchNews(
            string query = "Футбол",
            string country = "UA",
            string lang = "uk",
            int limit = 10)
        {
            
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://real-time-news-data.p.rapidapi.com/search?query={query}&limit={limit}&time_published=anytime&country={country}&lang={lang}"),
                Headers =
                {
                    { "x-rapidapi-key", Constants.RapidApiKey },
                    { "x-rapidapi-host", "real-time-news-data.p.rapidapi.com" },
                },
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<SerchRootobject>(body);

                return Ok(result);
            }
        }
        [HttpGet("Top-Headlines")]
        public async Task<IActionResult> GetTopHeadlines(
            string country = "UA",
            string lang = "uk",
            int limit = 10)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://real-time-news-data.p.rapidapi.com/top-headlines?country={country}&lang={lang}&limit={limit}"),
                Headers =
                {
                    { "x-rapidapi-key", Constants.RapidApiKey },
                    { "x-rapidapi-host", "real-time-news-data.p.rapidapi.com" },
                },
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<TopHRootobject>(body);

                return Ok(result);
            }
        }
        [HttpGet("Local-News")]
        public async Task<IActionResult> GetBySection(
            string query = "Київ",
            string country = "UA",
            string lang = "uk",
            int limit = 10)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://real-time-news-data.p.rapidapi.com/local-headlines?query={query}&country={country}&lang={lang}&limit={limit}"),
                Headers =
                {
                    { "x-rapidapi-key", Constants.RapidApiKey },
                    { "x-rapidapi-host", "real-time-news-data.p.rapidapi.com" },
                },
            };

            using (var response = await client.SendAsync(request))
            {
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync();

                var result = JsonConvert.DeserializeObject<LocalRootobject>(body);

                return Ok(result);
            }
        }
    }
}