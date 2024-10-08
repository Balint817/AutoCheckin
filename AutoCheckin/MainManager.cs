﻿using AutoCheckin.Enums;
using AutoCheckin.Exceptions;
using AutoCheckin.Games;
using AutoCheckin.Objects;
using Constants;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace AutoCheckin
{
    public class MainManager
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MainManager()
        {
            PartialInit().Wait();
        }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        [JsonPropertyName("dailyToken")]
        public DailyToken DailyToken { get; set; }

        [JsonPropertyName("giftToken")]
        public GiftToken GiftToken { get; set; }

        [JsonPropertyName("games")]
        public Dictionary<string, Settings> AllSettings { get; set; }

        [JsonPropertyName("closeOnSuccess")]
        public bool? AutoCloseOnSuccess { get; set; }

        [JsonPropertyName("verbosity")]
        public string VerbosityString { get; set; }

        [JsonIgnore]
        public Verbosity Verbosity => Enum.Parse<Verbosity>(VerbosityString);

        [JsonPropertyName("allVerbosityOptions")]
        public string[] AllVerbosityOptions => Enum.GetNames<Verbosity>();
        [JsonPropertyName(Values.LastVersionKey)]
        public string LastSkippedVersion { get; set; }
        public bool TryGetSettings(string key, [MaybeNullWhen(false)]out Settings settings)
        {
            return AllSettings.TryGetValue(key, out settings);
        }
        [JsonIgnore]
        public List<BaseGame> Games { get; set; }
        [JsonIgnore]
        private bool _initFinished;
        [JsonIgnore]
        public CodeRegionsByGame TriedCodes { get; set; }

        public async Task PartialInit()
        {
            DailyToken ??= new();
            DailyToken.EnsureNotNull();

            GiftToken ??= new();
            GiftToken.EnsureNotNull();

            VerbosityString ??= "Error";
            VerbosityString = VerbosityString.Trim();
            try
            {
                _ = Verbosity;
            }
            catch (Exception)
            {
                VerbosityString = "Error";
            }

            AutoCloseOnSuccess ??= true;

            await EnsureDefaults();
        }

        async Task GenericAskAction(string baseMsg, string msgExtend, string title, AsyncAction action)
        {
            var text = baseMsg;
            await Logger.Log(text, Verbosity.Silent);
            text += msgExtend;
            var msgBoxResult = Utils.ShowQuestion(text, title);
            if (msgBoxResult != MessageBoxResult.Yes)
            {
                throw new OperationCanceledException();
            }
            await action();
        }

        async Task<bool> GenericLogin(string baseMsg, string title, AsyncAction action)
        {
            var errMsgExtend = "\nDo you want to log in?";
            try
            {
                await GenericAskAction(baseMsg, errMsgExtend, title, action);
                return true;
            }
            catch (OperationCanceledException)
            {
                await Logger.Log("Operation was cancelled by the user.", Verbosity.Silent, color: ConsoleColor.DarkMagenta);
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                await Logger.Log("Encountered an error during log-in process.", Verbosity.Silent, color: ConsoleColor.Red);
            }
            return false;

        }
        async Task InitTriedCodes()
        {
            TriedCodes = new();
            try
            {
                await File.WriteAllTextAsync(Program.TriedCodesPath, "{}");
            }
            catch (Exception ex2)
            {
                await Logger.Log(ex2.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                await Logger.Log("Failed to write tried codes!", Verbosity.Silent, color: ConsoleColor.DarkRed);
                await Utils.ExitFunction(true);
            }
        }
        public async Task Init()
        {
            if (_initFinished)
            {
                throw new InvalidOperationException();
            }

            await PartialInit();

            await Logger.Log("Instantiating game scripts...", Verbosity.Detail);

            Games = new();
            var args = new object[] { this };
            foreach (var k in AllSettings.Keys)
            {
                var type = Type.GetType(k);
                if (type is null)
                {
                    continue;
                }
                var game = (BaseGame?)Activator.CreateInstance(type, args);
                if (game is null)
                {
                    continue;
                }
                Games.Add(game);
            }
            await Logger.Log("Loading tried codes...", Verbosity.Detail);

            try
            {
                var text = await File.ReadAllTextAsync(Program.TriedCodesPath);
                TriedCodes = JsonSerializer.Deserialize<CodeRegionsByGame>(text, Program.JsonOptions) ?? throw new NullReferenceException();
                foreach (var kv in TriedCodes)
                {
                    if (kv.Value is null)
                    {
                        TriedCodes[kv.Key] = new();
                    }
                }
            }
            catch (FileNotFoundException)
            {
                await Logger.Log("Couldn't find tried codes. This is normal during first run.", Verbosity.Silent, color: ConsoleColor.Red);
                await InitTriedCodes();
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                await Logger.Log("Failed to read tried codes. This may cause codes that have already been tried to be tried again.", Verbosity.Silent, color: ConsoleColor.Red);
                await InitTriedCodes();
            }

            await Logger.Log("Checking tokens...", Verbosity.Detail);

            bool exitAfter = false;

            var findInvalidUIDs = AllSettings
                .Where(x => x.Value.GetEnabledRegions().Any(x => x.CodeRedeemEnabled && !x.IsUIDValid))
                .Select(x => Games.First(y => y.ClassKey == x.Key))
                .ToList();


            if (!DailyToken.IsValid && AllSettings.Values.Any(x => x.CheckinEnabled))
            {
                var baseMsg = "Check-in is enabled, but the required tokens are missing.";
                var title = "Missing daily token";
                var invalidUidCopy = findInvalidUIDs;
                AsyncAction loginAction = async () =>
                {
                    if (invalidUidCopy.Any())
                    {
                        try
                        {
                            DailyToken = await TokenLoader.GetDailyCookiesAndUids(invalidUidCopy.ToArray()) ?? throw new NullReferenceException();
                        }
                        finally
                        {
                            invalidUidCopy.Clear();
                        }
                    }
                    else
                    {
                        DailyToken = await TokenLoader.GetDailyCookies() ?? throw new NullReferenceException();
                    }
                };
                var success = await GenericLogin(baseMsg, title, loginAction);
                if (!success)
                {
                    exitAfter = true;
                }
            }

            if (findInvalidUIDs.Any())
            {
                var baseMsg = "Found missing UIDs on enabled modules.";
                var title = "Missing UID";
                AsyncAction loginAction = async () =>
                {
                    DailyToken = await TokenLoader.GetDailyCookiesAndUids(findInvalidUIDs.ToArray()) ?? throw new NullReferenceException();
                };
                var success = await GenericLogin(baseMsg, title, loginAction);
                if (!success)
                {
                    exitAfter = true;
                }
            }

            if (!GiftToken.IsValid && AllSettings.Values.Any(x => x.GetEnabledRegions().Any()))
            {
                var baseMsg = "Code redeems are enabled, but the required tokens are missing.";
                var title = "Missing gift token";
                AsyncAction loginAction = async () =>
                {
                    GiftToken = await TokenLoader.GetGiftCookies() ?? throw new NullReferenceException();
                };
                var success = await GenericLogin(baseMsg, title, loginAction);
                if (!success)
                {
                    exitAfter = true;
                }
            }

            await RewriteSettings();

            if (exitAfter)
            {
                await Utils.ExitFunction(true);
            }

            _initFinished = true;
        }
        private async Task EnsureDefaults()
        {
            await Logger.Log("Validating settings...", Verbosity.Detail);

            AllSettings ??= new();
            foreach (var kv in AllSettings)
            {
                if (kv.Value is null)
                {
                    AllSettings.Remove(kv.Key);
                    continue;
                }
                kv.Value.EnsureNotNull();
            }
            await Logger.Log("Loading types...", Verbosity.Detail);

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(BaseGame).IsAssignableFrom(type))
                    {
                        if (type == typeof(BaseGame))
                        {
                            continue;
                        }
                        if (type.IsGenericType)
                        {
                            continue;
                        }
                        if (!AllSettings.TryGetValue(type.FullName!, out var settings) || settings is null)
                        {
                            AllSettings[type.FullName!] = settings = new();
                        }
                        settings.EnsureNotNull();
                    }
                }
            }
        }

        public async Task RunAll()
        {
            await Logger.Log("Running scripts...", Verbosity.Detail);

            var failedCheckin = new List<BaseGame>();
            var failedRedeems = new List<Tuple<BaseGame, Exception>>();
            bool anyCheckin = false;
            bool anyRedeem = false;
            foreach (var game in Games)
            {
                try
                {
                    await game.Init();
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                    await Logger.Log("Failed to init game scripts", Verbosity.Silent, color: ConsoleColor.DarkRed);
                    await Utils.ExitFunction(true);
                    return;
                }

                try
                {
                    if (TryGetSettings(game.ClassKey, out var settings) && settings.CheckinEnabled)
                    {
                        anyCheckin = true;
                    }
                    if (!await game.Checkin())
                    {
                        await Logger.Log($"Failed to check into {game.ClassName}", Verbosity.Silent, color: ConsoleColor.Red);
                        failedCheckin.Add(game);
                    }
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                    await Logger.Log($"Encountered an error while checking into {game.ClassName}", Verbosity.Silent, color: ConsoleColor.DarkRed);
                    failedCheckin.Add(game);
                }

                try
                {
                    if (TryGetSettings(game.ClassKey, out var settings) && settings.GetEnabledRegions().Any(x => x.IsValid))
                    {
                        anyRedeem = true;
                    }
                    await game.RedeemCodes();
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                    await Logger.Log($"Encountered an error while redeeming codes for {game.ClassName}", Verbosity.Silent, color: ConsoleColor.DarkRed);

                    failedRedeems.Add(new(game, ex));
                }
            }
            await Logger.Log($"-------------", Verbosity.Silent);

            if (!anyCheckin && !anyRedeem)
            {
                await Logger.Log("It seems like nothing happened...", Verbosity.Silent, color: ConsoleColor.DarkYellow);
                await Logger.Log("Make sure that you've configured everything correctly.", Verbosity.Silent, color: ConsoleColor.DarkYellow);
                await Utils.ExitFunction(true);
                return;
            }

            if (failedCheckin.Count != 0)
            {
                var msg = "Failed daily check-in for the following games:";
                var sb = new StringBuilder();
                sb.AppendLine(msg);
                await Logger.Log(msg, Verbosity.Silent, color: ConsoleColor.Red);
                foreach (var game in failedCheckin)
                {
                    msg = $"  - {game.ClassName}";
                    sb.AppendLine(msg);
                    await Logger.Log(msg, Verbosity.Silent, color: ConsoleColor.Red);
                }
                msg = "Do you want to check-in manually?";
                sb.AppendLine(msg);
                await Logger.Log(msg, Verbosity.Silent, color: ConsoleColor.Red);

                var text = sb.ToString();
                var title = "Check-in Failed";
                var failedCheckinCopy = failedCheckin;
                AsyncAction loginAction = async () =>
                {
                    foreach (var game in failedCheckinCopy)
                    {
                        Utils.OpenUrl(game.DailyManualUrl);
                    }
                };

                try
                {
                    await GenericAskAction("", text, title, loginAction);
                }
                catch (OperationCanceledException) {};
            }

            if (anyRedeem)
            {
                await Logger.Log($"Saving tried codes...", Verbosity.Detail);

                try
                {
                    var text = JsonSerializer.Serialize(TriedCodes, Program.JsonOptions);
                    await File.WriteAllTextAsync(Program.TriedCodesPath, text);
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                    await Logger.Log("Failed to write tried codes to file", Verbosity.Silent, color: ConsoleColor.DarkRed);
                }
            }

            bool fatalRedeemFailures = false;
            bool settingsChanged = false;
            foreach (var kv in failedRedeems)
            {
                var game = kv.Item1;
                var boxedEx = kv.Item2;
                switch (boxedEx)
                {
                    case CaptchaBlockException ex:
                        await Logger.Log($"Code redeem for {game.ClassName} was blocked by a captcha check", Verbosity.Silent, color: ConsoleColor.Red);
                        fatalRedeemFailures = true;
                        break;
                    case InvalidRegionException ex:
                        await Logger.Log($"Failed to redeem codes for {game.ClassName} because an account did not exist in the given region.", Verbosity.Silent, color: ConsoleColor.Red);
                        await Logger.Log($"This could be also be a sign that the wrong UID is set or the token doesn't match.", Verbosity.Silent, color: ConsoleColor.Red);

                        fatalRedeemFailures = true;
                        break;
                    case InvalidTokenException ex:
                        await Logger.Log($"Failed to redeem codes for {game.ClassName} because the user wasn't logged in.", Verbosity.Silent, color: ConsoleColor.Red);
                        await Logger.Log($"All tokens and UIDs were automatically invalidated.", Verbosity.Silent, color: ConsoleColor.Red);
                        await Logger.Log($"You will need to complete setup again on the next launch.", Verbosity.Silent, color: ConsoleColor.Red);
                        var settings = AllSettings[game.ClassKey];
                        settings.InvalidateRegions();
                        DailyToken.Reset();
                        GiftToken.Reset();

                        fatalRedeemFailures = true;
                        settingsChanged = true;
                        break;
                    case RequestMalformedException ex:
                        await Logger.Log($"Failed to redeem codes for {game.ClassName} because the server returned an error.", Verbosity.Silent, color: ConsoleColor.Red);
                        fatalRedeemFailures = true;
                        break;
                    case SystemBusyException ex:
                        await Logger.Log($"Failed to redeem codes for {game.ClassName} because the server is currently unavailable (or the request is malformed).", Verbosity.Silent, color: ConsoleColor.Red);
                        fatalRedeemFailures = true;
                        break;
                    default:
                        break;
                }
            }
            if (settingsChanged)
            {
                await RewriteSettings();
            }
            if (fatalRedeemFailures)
            {
                await Utils.ExitFunction(true);
            }
        }

        async Task RewriteSettings()
        {

            await Logger.Log("Rewriting settings...", Verbosity.Detail);

            try
            {
                var serialized = JsonSerializer.Serialize(this, Program.JsonOptions);
                await File.WriteAllTextAsync(Program.SettingsPath, serialized);
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error, color: ConsoleColor.Red);
                await Logger.Log("Failed to re-write settings!", Verbosity.Silent, color: ConsoleColor.DarkRed);
                await Utils.ExitFunction(true);
            }
        }
    }
}
