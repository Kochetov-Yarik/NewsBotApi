using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using NewsBotApi.Models;
using System.Text;

namespace DiscordBot
{
    public class FavoriteSite
    {
        public int Id { get; set; }
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }
    }

    public class SavedNews
    {
        public string Title { get; set; }
        public string Snippet { get; set; }
        public string Link { get; set; }
        public string PhotoUrl { get; set; }
    }

    class Program
    {
        private DiscordSocketClient _client;
        private static Dictionary<ulong, List<SavedNews>> _userMemory = new();

        private static Dictionary<ulong, List<string>> _searchHistory = new();

        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiBaseUrl = "https://localhost:7025/api";

        static async Task Main(string[] args) => await new Program().RunBotAsync();

        public async Task RunBotAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig { GatewayIntents = GatewayIntents.AllUnprivileged });
            _client.Log += Log;

            _client.Ready += ReadyAsync;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.SelectMenuExecuted += SelectMenuHandler;
            _client.ButtonExecuted += ButtonHandler;

            await _client.LoginAsync(TokenType.Bot, Constants.DiskordToken);
            await _client.StartAsync();
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private async Task ReadyAsync()
        {
            try
            {
                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("search").WithDescription("Пошук новин за ключовим словом")
                    .AddOption("query", ApplicationCommandOptionType.String, "Що шукаємо? (наприклад: Спорт)", isRequired: true).Build());

                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("top-headlines").WithDescription("Отримати головні світові або національні новини")
                    .AddOption("country", ApplicationCommandOptionType.String, "Код країни (UA, US)", isRequired: false)
                    .AddOption("lang", ApplicationCommandOptionType.String, "Мова новин (uk, en)", isRequired: false).Build());

                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("local-news").WithDescription("Отримати регіональні новини міст")
                    .AddOption("query", ApplicationCommandOptionType.String, "Назва міста (наприклад: Київ)", isRequired: false).Build());

                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("list-sites").WithDescription("Переглянути список збережених сайтів").Build());

                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("remove-site").WithDescription("Видалити сайт зі списку")
                    .AddOption("id", ApplicationCommandOptionType.Integer, "ID сайту, який треба видалити", isRequired: true).Build());

                await _client.CreateGlobalApplicationCommandAsync(new SlashCommandBuilder()
                    .WithName("history").WithDescription("Переглянути історію ваших останніх пошуків новин").Build());

                Console.WriteLine("[УСПІХ] Усі слейш-команди зареєстровані!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ПОМИЛКА РЕЄСТРАЦІЇ] {ex.Message}");
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            await command.DeferAsync();

            _ = Task.Run(async () =>
            {
                try
                {
                    if (command.Data.Name == "history")
                    {
                        if (_searchHistory.TryGetValue(command.User.Id, out var history) && history.Count > 0)
                        {
                            await command.FollowupAsync("🕒 **Ваші останні пошуки:**\n\n" + string.Join("\n", history));
                        }
                        else
                        {
                            await command.FollowupAsync("📭 Ваша історія пошуку наразі порожня.");
                        }
                        return;
                    }

                    List<SavedNews> fetchedNews = new List<SavedNews>();
                    string apiUrl = null;
                    string historyText = "";

                    switch (command.Data.Name)
                    {
                        case "search":
                            var qSearch = command.Data.Options.First().Value.ToString();
                            apiUrl = $"{ApiBaseUrl}/News/Search?query={Uri.EscapeDataString(qSearch)}&limit=10";
                            historyText = $"🔍 `/search` ➡️ {qSearch}";
                            break;
                        case "top-headlines":
                            var country = command.Data.Options.FirstOrDefault(o => o.Name == "country")?.Value?.ToString() ?? "UA";
                            var lang = command.Data.Options.FirstOrDefault(o => o.Name == "lang")?.Value?.ToString() ?? "uk";
                            apiUrl = $"{ApiBaseUrl}/News/Top-Headlines?country={country}&lang={lang}&limit=10";
                            historyText = $"🌍 `/top-headlines` ➡️ Країна: {country}, Мова: {lang}";
                            break;
                        case "local-news":
                            var city = command.Data.Options.FirstOrDefault(o => o.Name == "query")?.Value?.ToString() ?? "Київ";
                            apiUrl = $"{ApiBaseUrl}/News/Local-News?query={Uri.EscapeDataString(city)}&limit=10";
                            historyText = $"🏙️ `/local-news` ➡️ Місто: {city}";
                            break;
                    }

                    if (apiUrl != null)
                    {
                        if (!_searchHistory.ContainsKey(command.User.Id))
                            _searchHistory[command.User.Id] = new List<string>();

                        _searchHistory[command.User.Id].Insert(0, $"`[{DateTime.Now:HH:mm}]` {historyText}");

                        var response = await _httpClient.GetAsync(apiUrl);
                        response.EnsureSuccessStatusCode();

                        dynamic result = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
                        if (result?.data != null)
                        {
                            foreach (var item in result.data)
                                fetchedNews.Add(new SavedNews { Title = item.title, Snippet = item.snippet, Link = item.link, PhotoUrl = item.photo_url });
                        }

                        if (fetchedNews.Count > 0)
                        {
                            _userMemory[command.User.Id] = fetchedNews;
                            string listText = "Ось що я знайшов. Виберіть потрібну новину в меню нижче:\n\n";
                            var menuBuilder = new SelectMenuBuilder().WithPlaceholder("Оберіть новину для читання...").WithCustomId("news_dropdown");

                            for (int i = 0; i < fetchedNews.Count; i++)
                            {
                                listText += $"**{i + 1}.** {fetchedNews[i].Title}\n";
                                string shortTitle = fetchedNews[i].Title.Length > 95 ? fetchedNews[i].Title.Substring(0, 95) + "..." : fetchedNews[i].Title;
                                menuBuilder.AddOption(shortTitle, i.ToString());
                            }

                            await command.FollowupAsync(text: listText, components: new ComponentBuilder().WithSelectMenu(menuBuilder).Build());
                        }
                        else
                        {
                            await command.FollowupAsync("На жаль, за цим запитом нічого не знайдено.");
                        }
                        return;
                    }

                    string favoritesApiUrl = $"{ApiBaseUrl}/Favorites";

                    if (command.Data.Name == "list-sites")
                    {
                        var response = await _httpClient.GetAsync(favoritesApiUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var sites = JsonConvert.DeserializeObject<List<FavoriteSite>>(await response.Content.ReadAsStringAsync());
                            if (sites == null || sites.Count == 0)
                                await command.FollowupAsync("📭 Ваш список збережених новин наразі порожній.");
                            else
                            {
                                string listText = "📌 **Ваші збережені новини:**\n\n";
                                foreach (var site in sites)
                                    listText += $"**ID {site.Id}:** [{site.SiteName}](<{site.SiteUrl}>)\n";
                                await command.FollowupAsync(listText);
                            }
                        }
                        else
                            await command.FollowupAsync("❌ Не вдалося отримати список новин.");
                    }
                    else if (command.Data.Name == "remove-site")
                    {
                        var idString = command.Data.Options.First(o => o.Name == "id").Value.ToString();
                        var response = await _httpClient.DeleteAsync($"{favoritesApiUrl}/{idString}");

                        if (response.IsSuccessStatusCode)
                            await command.FollowupAsync($"🗑️ Новину з ID **{idString}** успішно видалено!");
                        else
                            await command.FollowupAsync($"❌ Не вдалося видалити новину з ID {idString}. Можливо, її не існує.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ПОМИЛКА] {ex.Message}");
                    await command.FollowupAsync("Сталася помилка під час обробки команди.");
                }
            });
        }

        private async Task SelectMenuHandler(SocketMessageComponent component)
        {
            await component.DeferAsync();

            _ = Task.Run(async () =>
            {
                if (component.Data.CustomId != "news_dropdown") return;

                if (_userMemory.TryGetValue(component.User.Id, out var userNews))
                {
                    int selectedIndex = int.Parse(component.Data.Values.First());
                    var selectedNews = userNews[selectedIndex];

                    var embed = BuildNewsEmbed(selectedNews.Title, selectedNews.Snippet, selectedNews.Link, selectedNews.PhotoUrl);
                    var button = new ButtonBuilder().WithLabel("➕ Зберегти в обране").WithCustomId($"save_{selectedIndex}").WithStyle(ButtonStyle.Success);

                    await component.FollowupAsync(embed: embed, components: new ComponentBuilder().WithButton(button).Build());
                }
                else
                {
                    await component.FollowupAsync("Сесія застаріла. Будь ласка, виконайте пошук новин ще раз.", ephemeral: true);
                }
            });
        }

        private async Task ButtonHandler(SocketMessageComponent component)
        {
            await component.DeferAsync();

            _ = Task.Run(async () =>
            {
                if (!component.Data.CustomId.StartsWith("save_")) return;

                if (_userMemory.TryGetValue(component.User.Id, out var userNews))
                {
                    int index = int.Parse(component.Data.CustomId.Replace("save_", ""));
                    var newsToSave = userNews[index];

                    var newSite = new FavoriteSite { SiteName = newsToSave.Title, SiteUrl = newsToSave.Link };
                    var jsonContent = new StringContent(JsonConvert.SerializeObject(newSite), Encoding.UTF8, "application/json");

                    try
                    {
                        var response = await _httpClient.PostAsync($"{ApiBaseUrl}/Favorites", jsonContent);
                        if (response.IsSuccessStatusCode)
                            await component.FollowupAsync($"✅ Новину **{newsToSave.Title}** додано до ваших збережених!", ephemeral: true);
                        else
                            await component.FollowupAsync("❌ Помилка при збереженні до бази даних.", ephemeral: true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ПОМИЛКА БД] {ex.Message}");
                        await component.FollowupAsync("❌ Сервер бази даних не відповідає.", ephemeral: true);
                    }
                }
                else
                {
                    await component.FollowupAsync("Час очікування минув. Зробіть пошук новин ще раз.", ephemeral: true);
                }
            });
        }

        private Embed BuildNewsEmbed(string title, string snippet, string link, string photoUrl)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle($"📰 {title ?? "Новина"}")
                .WithDescription($"📝 {snippet ?? "Немає короткого опису."}\n\n🔗 **Посилання:** {link}")
                .WithColor(Color.Blue);

            if (!string.IsNullOrEmpty(photoUrl) && Uri.IsWellFormedUriString(photoUrl, UriKind.Absolute))
                embedBuilder.WithImageUrl(photoUrl);

            return embedBuilder.Build();
        }
    }
}