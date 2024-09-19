using AutoCheckin.Enums;
using AutoCheckin.Exceptions;
using AutoCheckin.Objects;
using MiscUtil.IO;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

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
            var response = await responseMsg.Content.ReadFromJsonAsync<HoyoResponse>(Program.JsonOptions);
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

            try
            {
                var progress = new CodeRedeemProgress(codes.LongLength * enabledRegions.Length);
                await PrintProgress(progress, false);

                for (int i = 0; i < enabledRegions.Length; i++)
                {
                    var settingRegion = enabledRegions[i];
                    if (!oldCodesByRegion.TryGetValue(settingRegion.RegionKey, out var oldCodes))
                    {
                        oldCodesByRegion[settingRegion.RegionKey] = oldCodes = new();
                    }
                    await RedeemCodes(cookie, settingRegion, codes, oldCodes, progress);
                }
                await PrintProgress(progress, final: true);
            }
            finally
            {
                await Console.Out.WriteLineAsync();
            }
        }

        async Task PrintProgress(CodeRedeemProgress progress, bool clear=true, bool final=false)
        {
            if (MainManager.Verbosity >= Verbosity.PartialDebug)
            {
                return;
            }
            if (clear)
            {
                Utils.ClearCurrentConsoleLine();
            }

            TextWriter stream = final ? Logger.LogWriter : Console.Out;

            try
            {

                var valid = progress.valid;
                var skipped = progress.skipped + progress.used;
                var invalid = progress.expired + progress.invalid + progress.unknown;

                await stream.WriteAsync($"{Logger.DateNow} Progress: ");

                var originalColor = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    await stream.WriteAsync($"{valid}/");

                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    await stream.WriteAsync($"{skipped}/");

                    Console.ForegroundColor = ConsoleColor.Red;
                    await stream.WriteAsync($"{invalid}/");
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }

                await stream.WriteAsync($"{progress.Ratio * 100:0.##}%");
                if (final)
                {
                    await stream.WriteLineAsync();
                }
            }
            finally
            {
                if (final)
                {
                    await stream.DisposeAsync();
                }
            }
        }

        async Task RedeemCodes(string cookie, RegionUID settingRegion, string[] codes, List<string> oldCodes, CodeRedeemProgress progress)
        {
            var region = SettingRegionToUrlRegion(settingRegion.RegionKey);
            for (int i = 0; i < codes.Length; i++)
            {
                var code = codes[i];
                if (oldCodes.Contains(code))
                {
                    progress.skipped++;
                    continue;
                }
                Thread.Sleep(5000);
                await Logger.Log($"Redeeming code '{code}'...", Verbosity.PartialDebug);
                var origin = CodeRedeemOrigin;
                var referer = CodeRedeemReferer;
                var requestMsg = GetBaseRequest(HttpMethod.Get, cookie, origin, referer);
                var url = string.Format(CodeUrl, settingRegion.UID, region, code);
                requestMsg.RequestUri = new Uri(url);
                await TransformCodeRedeemMessage(requestMsg);
                var responseMsg = await Program.Client.SendAsync(requestMsg);
                var responseBody = await responseMsg.Content.ReadAsStringAsync();
                await Logger.Log(responseBody, Verbosity.FullDebug);
                HoyoResponse? hoyoResponse = null;
                try
                {
                    hoyoResponse = JsonSerializer.Deserialize<HoyoResponse>(responseBody, Program.JsonOptions)!;
                    if (hoyoResponse?.IsMalformed ?? true)
                    {
                        throw new RequestMalformedException();
                    }
                    else if (hoyoResponse.IsBusy)
                    {
                        throw new SystemBusyException();
                    }
                    else if (hoyoResponse.NotLoggedIn)
                    {
                        throw new InvalidTokenException();
                    }
                    else if (hoyoResponse.IsCaptchaBlock)
                    {
                        throw new CaptchaBlockException();
                    }
                    else if (hoyoResponse.MissingRegion)
                    {
                        throw new InvalidRegionException(settingRegion.RegionKey);
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync();
                    await Logger.Log(responseBody, Verbosity.Error);
                    throw ex;
                }
                var progressResp = progress.Add(hoyoResponse);
                progressResp ??= $"Unknown response.";
                await Logger.Log(progressResp, Verbosity.PartialDebug);
                oldCodes.Add(code);
                await PrintProgress(progress);
            }
        }

        internal protected BaseGame(MainManager mainManager)
        {
            ArgumentNullException.ThrowIfNull(mainManager, nameof(mainManager));
            MainManager = mainManager;
        }
    }
}
