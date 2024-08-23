using AutoCheckin.Objects;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Net.Http.Json;

namespace AutoCheckin.Games
{
    public abstract class BaseGame
    {

        public const string DailyOrigin = "https://act.hoyolab.com";
        public const string DailyReferer = "https://act.hoyolab.com/";
        public abstract string DailyApiUrl { get; }
        public abstract string DailyManualUrl { get; }
        public abstract IEnumerable<SelectorAction> GetUidActions();
        public abstract string HoyolabGameProfileUrl { get; }
        public abstract string? GetUidFromCompleteActions(SelectorAction[] actions, string regionKey);
        public abstract string CodeUrl { get; }
        public string ClassKey => this.GetType().FullName!;
        public string ClassName => this.GetType().Name;
        public virtual async Task<string[]> GetRedeemCodes() => await Utils.NotImplementedAsync<string[]>();
        public abstract string CodeRedeemOrigin { get; }
        public abstract string CodeRedeemReferer { get; }
        public virtual async Task Init() => await Task.CompletedTask;
        public virtual async Task TransformCodeRedeemMessage(HttpRequestMessage message) => await Task.CompletedTask;
        public abstract string SettingRegionToUrlRegion(string region);
        public async Task<bool> Checkin()
        {
            if (!TryGetSettings(out var settings) || !settings.CheckinEnabled)
            {
                return true;
            }
            await Logger.Log($"Checking into {ClassName}...", Verbosity.Silent);
            if (!MainManager.DailyToken.IsValid)
            {
                await Logger.Log($"Token for {ClassName} is not valid!", Verbosity.Error);
                return false;
            }
            var cookie = MainManager.DailyToken.MakeCookie();
            var requestMsg = GetBaseRequest(HttpMethod.Post, cookie, DailyOrigin, DailyReferer);
            requestMsg.RequestUri = new Uri(DailyApiUrl);
            var responseMsg = await Program.Client.SendAsync(requestMsg);
            var response = await responseMsg.Content.ReadFromJsonAsync<DailyResponse>(Program.JsonOptions);
            return response?.IsSuccess ?? false;
        }

        internal protected HttpRequestMessage GetBaseRequest(HttpMethod method, string cookie, string origin = "", string referer = "")
        {
            return new HttpRequestMessage()
            {
                Method = method,
                Headers =
                {
                    { "Cookie", cookie },
                    { "Accept", "application/json, text/plain, */*" },
                    { "Accept-Encoding", "gzip, deflate, br, zstd" },
                    { "Accept-Language", "en-US,en;q=0.9" },
                    { "Connection", "keep-alive" },
                    { "x-rpc-app_version", "2.34.1" },
                    { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36" },
                    { "x-rpc-client_type", "4" },
                    { "Origin", origin },
                    { "Referer", referer },
                }
            };

        }

        protected MainManager MainManager { get; private set; }
        public bool TryGetSettings([MaybeNullWhen(false)]out Settings settings)
        {
            return MainManager.TryGetSettings(ClassKey, out settings);
        }

        public async Task RedeemCodes()
        {
            if (!TryGetSettings(out Settings? settings) || !settings.GetEnabledRegions().Any())
            {
                return;
            }

            var codes = await GetRedeemCodes();
            await Logger.Log($"Checking {codes.Length} codes for {ClassName}...", Verbosity.Silent);
            var enabledRegions = settings.GetEnabledRegions().ToArray();

            if (!MainManager.TriedCodes.TryGetValue(ClassKey, out var oldCodesByRegion))
            {
                MainManager.TriedCodes[ClassKey] = oldCodesByRegion = new();
            }
            var cookie = MainManager.GiftToken.MakeCookie();

            var total = codes.LongLength * enabledRegions.Length;
            long triedCodesCount = 0;

            try
            {
                await PrintProgress(0, 1, false);

                for (int i = 0; i < enabledRegions.Length; i++)
                {
                    var settingRegion = enabledRegions[i];
                    if (!oldCodesByRegion.TryGetValue(settingRegion.RegionKey, out var oldCodes))
                    {
                        oldCodesByRegion[settingRegion.RegionKey] = oldCodes = new();
                    }
                    triedCodesCount = await RedeemCodes(cookie, settingRegion, codes, oldCodes, triedCodesCount, total);
                }
            }
            finally
            {
                await Console.Out.WriteLineAsync();
            }
        }

        async Task PrintProgress(long triedCodesCount, long total, bool clear=true)
        {
            if (MainManager.Verbosity >= Verbosity.PartialDebug)
            {
                return;
            }
            if (clear)
            {
                Utils.ClearCurrentConsoleLine();
            }
            await Console.Out.WriteAsync($"{Logger.DateNow} Progress: {triedCodesCount / (double)total * 100:0.##}%");
        }

        async Task<long> RedeemCodes(string cookie, RegionUID settingRegion, string[] codes, List<string> oldCodes, long triedCodesCount, long total)
        {
            var region = SettingRegionToUrlRegion(settingRegion.RegionKey);
            for (int i = 0; i < codes.Length; i++)
            {
                triedCodesCount++;
                var code = codes[i];
                if (oldCodes.Contains(code))
                {
                    continue;
                }
                await Logger.Log($"Redeeming code '{code}'...", Verbosity.PartialDebug);
                var origin = CodeRedeemOrigin;
                var referer = CodeRedeemReferer;
                var requestMsg = GetBaseRequest(HttpMethod.Get, cookie, origin, referer);
                var url = string.Format(CodeUrl, settingRegion.UID, region, code);
                requestMsg.RequestUri = new Uri(url);
                await TransformCodeRedeemMessage(requestMsg);
                var responseMsg = await Program.Client.SendAsync(requestMsg);
                var response = await responseMsg.Content.ReadAsStringAsync();
                await Logger.Log(response, Verbosity.FullDebug);
                oldCodes.Add(code);
                Thread.Sleep(5000);
                await PrintProgress(triedCodesCount, total);
            }
            return triedCodesCount;
        }

        internal protected BaseGame(MainManager mainManager)
        {
            ArgumentNullException.ThrowIfNull(mainManager, nameof(mainManager));
            MainManager = mainManager;
        }
    }
}
