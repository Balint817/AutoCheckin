using AutoCheckin.Games;
using AutoCheckin.Objects;
using Microsoft.Playwright;
using System.IO;
using System.Text.Json;

namespace AutoCheckin
{
    public class SelectorAction
    {
        public SelectorAction(string selector, long timeoutMilliseconds = 0, Func<IElementHandle, Task>? action = null)
        {
            Selector = selector;
            TimeoutMilliseconds = timeoutMilliseconds;
            Action = action;
        }
        public string Selector { get; set; }
        public long TimeoutMilliseconds { get; set; }
        public Func<IElementHandle, Task>? Action { get; set; }
        public string? Result { get; set; }
    }
    class TokenLoader
    {

        static Dictionary<string, string?> readDailyToken = new()
        {
            ["ltoken_v2"] = null,
            ["ltmid_v2"] = null,
            ["ltuid_v2"] = null,
        };
        static Dictionary<string, string?> readGiftToken = new()
        {
            ["cookie_token_v2"] = null,
            ["account_mid_v2"] = null,
            ["account_id_v2"] = null,
        };

        const string DailyUrl = "https://act.hoyolab.com/ys/event/signin-sea-v3/index.html?act_id=e202102251931481&hyl_auth_required=true&hyl_presentation_style=fullscreen&lang=en-us&bbs_theme=dark&bbs_theme_device=1";
        const string GiftUrl = "https://genshin.hoyoverse.com/en/gift";

        internal static async Task<DailyToken> GetDailyCookies()
        {
            return await GetCookies<DailyToken>(DailyUrl, readDailyToken);
        }
        internal static async Task<GiftToken> GetGiftCookies()
        {
            return await GetCookies<GiftToken>(GiftUrl, readGiftToken);
        }
        internal static async Task<DailyToken> GetDailyCookiesAndUids(BaseGame[] uidGames)
        {
            var dailyUrl = DailyUrl;
            var requiredCookies = readDailyToken;
            var actions = new List<PlaywrightDelegate>
            {
                async (c, p) => await GetRequiredCookies(c, p, dailyUrl, requiredCookies)
            };
            var completeActions = new List<SelectorAction[]?>();
            foreach (var game in uidGames)
            {
                try
                {
                    var gameCopy = game;
                    var url = game.HoyolabGameProfileUrl;
                    var getActions = game.GetUidActions().ToArray();
                    completeActions.Add(getActions);
                    actions.Add(async (c, p) => await GetRequiredText(p, gameCopy.HoyolabGameProfileUrl, getActions));
                }
                catch (NotImplementedException)
                {
                    completeActions.Add(null);
                }
            }

            await PlaywrightMethod(actions.ToArray());

            bool missingUid = false;

            for (int i = 0; i < uidGames.Length; i++)
            {
                var game = uidGames[i];
                var complete = completeActions[i];
                if (complete is null)
                {
                    missingUid = true;
                    continue;
                }
                if (!game.TryGetSettings(out var settings))
                {
                    throw new ArgumentException("received game without settings", nameof(uidGames));
                }
                foreach (var region in settings.GetEnabledRegions())
                {
                    var uid = game.GetUidFromCompleteActions(complete, region.RegionKey);

                    var oldValue = region.UID;
                    var oldValid = region.IsUIDValid;
                    if (uid is null)
                    {
                        if (!oldValid)
                        {
                            missingUid = true;
                        }
                        continue;
                    }
                    region.UID = string.Join("", uid.Where(c => '0' <= c && c <= '9'));
                    if (!region.IsUIDValid)
                    {
                        if (!oldValid)
                        {
                            missingUid = true;
                            continue;
                        }
                        region.UID = oldValue;
                    }
                }
            }
            if (missingUid)
            {
                throw new ApplicationException("Failed to obtain UIDs for all provided games");
            }

            var serialized = JsonSerializer.Serialize(requiredCookies, Program.JsonOptions);
            return JsonSerializer.Deserialize<DailyToken>(serialized)!;
        }
        static async Task<T> GetCookies<T>(string url, Dictionary<string, string?> requiredCookies)
        {
            await PlaywrightMethod(async (c, p) => await GetRequiredCookies(c, p, url, requiredCookies));
            var serialized = JsonSerializer.Serialize(requiredCookies, Program.JsonOptions);
            return JsonSerializer.Deserialize<T>(serialized)!;
        }
        static async Task GetRequiredText(IPage page, string url, IList<SelectorAction> selectors)
        {
            await page.GotoAsync(url);
            foreach (var selector in selectors)
            {
                bool searching = true;
                int timePassed = 0;
                while (searching)
                {
                    var queryAll = await page.QuerySelectorAllAsync(selector.Selector);

                    if (queryAll.Count != 0)
                    {
                        selector.Result = await queryAll[0].InnerTextAsync();
                        var task = selector.Action?.Invoke(queryAll[0]);
                        if (task != null)
                        {
                            await task;
                        }
                        searching = false;
                    }
                    else
                    {
                        // throws an exception if page is closed (to cancel the process)
                        if (page.IsClosed)
                        {
                            throw new OperationCanceledException();
                        }
                    }
                    await Task.Delay(100);
                    timePassed += 100;
                    if (selector.TimeoutMilliseconds > 0 && timePassed > selector.TimeoutMilliseconds)
                    {
                        searching = false;
                    }
                }
            }
        }
        static async Task GetRequiredCookies(IBrowserContext context, IPage page, string url, Dictionary<string, string?> requiredCookies)
        {
            await page.GotoAsync(url);
            bool searching = true;
            while (searching)
            {
                var cookies = await context.CookiesAsync();
                foreach (var cookie in cookies)
                {
                    if (requiredCookies.ContainsKey(cookie.Name))
                    {
                        requiredCookies[cookie.Name] = cookie.Value;
                    }
                }

                bool anyMissing = requiredCookies.Values.Any(string.IsNullOrEmpty);
                if (!anyMissing)
                {
                    searching = false;
                }
                else
                {
                    // throws an exception if page is closed (to cancel the process)
                    if (page.IsClosed)
                    {
                        throw new OperationCanceledException();
                    }
                }
                await Task.Delay(100);
            }
        }

        public delegate Task PlaywrightDelegate(IBrowserContext context, IPage page);
        static async Task PlaywrightMethod(params PlaywrightDelegate[] actions)
        {
            using var playwright = await Playwright.CreateAsync();

            if (!File.Exists(playwright.Chromium.ExecutablePath))
            {
                await Logger.Log("Preparing for first launch...", Verbosity.Silent);
                await Utils.ExecuteScript(Path.GetFullPath("playwright.ps1"), "install");
            }

            var launchOptions = new BrowserTypeLaunchOptions
            {
                ChromiumSandbox = true,
                Headless = false,
            };

            await using var browser = await playwright.Chromium.LaunchAsync(launchOptions);
            var context = await browser.NewContextAsync();
            /// <see cref="IBrowserContext.DisposeAsync"/>
            var page = await context.NewPageAsync();
            foreach (var action in actions)
            {
                var task = action?.Invoke(context, page);
                if (task != null)
                {
                    await task;
                }
                if (page.IsClosed)
                {
                    page = await context.NewPageAsync();
                }

            }

            await context.CloseAsync();
            await browser.CloseAsync();

            await context.DisposeAsync();
        }
    }
}
