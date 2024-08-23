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
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public async Task Init()
        {
            if (_initFinished)
            {
                throw new InvalidOperationException();
            }

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
                await Logger.Log("Couldn't find tried codes. This is normal during first run.", Verbosity.Silent);
                TriedCodes = new();
                try
                {
                    await File.WriteAllTextAsync(Program.TriedCodesPath, "{}");
                }
                catch (Exception ex2)
                {
                    await Logger.Log(ex2.ToString(), Verbosity.Error);
                    await Logger.Log("Failed to write tried codes!", Verbosity.Silent);
                    await Utils.ExitFunction(true);
                }
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error);
                await Logger.Log("Failed to read tried codes. This may cause codes that have already been tried to be tried again.", Verbosity.Silent);
                TriedCodes = new();
                try
                {
                    await File.WriteAllTextAsync(Program.TriedCodesPath, "{}");
                }
                catch (Exception ex2)
                {
                    await Logger.Log(ex2.ToString(), Verbosity.Error);
                    await Logger.Log("Failed to write tried codes!", Verbosity.Silent);
                    await Utils.ExitFunction(true);
                }
            }

            await Logger.Log("Checking tokens...", Verbosity.Detail);

            bool exitAfter = false;

            var findInvalidUIDs = AllSettings
                .Where(x => x.Value.GetEnabledRegions().Any(x => x.CodeRedeemEnabled && !x.IsUIDValid))
                .Select(x => Games.First(y => y.ClassKey == x.Key))
                .ToArray();


            if (!DailyToken.IsValid && AllSettings.Values.Any(x => x.CheckinEnabled))
            {
                try
                {
                    var text = "Check-in is enabled, but the required tokens are missing.";
                    await Logger.Log(text, Verbosity.Silent);
                    text += "\nDo you want to log in?";
                    var title = "Missing daily token";
                    var msgBoxResult = Utils.ShowQuestion(text, title);
                    if (msgBoxResult != MessageBoxResult.Yes)
                    {
                        throw new OperationCanceledException();
                    }
                    if (findInvalidUIDs.Any())
                    {
                        DailyToken = await TokenLoader.GetDailyCookiesAndUids(findInvalidUIDs) ?? throw new NullReferenceException();
                    }
                    else
                    {
                        DailyToken = await TokenLoader.GetDailyCookies() ?? throw new NullReferenceException();
                    }
                }
                catch (OperationCanceledException)
                {
                    await Logger.Log("Operation was cancelled by the user.", Verbosity.Silent);
                    exitAfter = true;
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log("Encountered an error during log-in process.", Verbosity.Silent);
                    exitAfter = true;
                }
            }

            if (findInvalidUIDs.Any())
            {
                //TODO
                try
                {
                    var text = "Found missing UIDs on enabled modules.";
                    await Logger.Log(text, Verbosity.Silent);
                    text += "\nDo you want to log in?";
                    var title = "Missing UID";
                    var msgBoxResult = Utils.ShowQuestion(text, title);
                    if (msgBoxResult != MessageBoxResult.Yes)
                    {
                        throw new OperationCanceledException();
                    }
                    DailyToken = await TokenLoader.GetDailyCookiesAndUids(findInvalidUIDs) ?? throw new NullReferenceException();
                }
                catch (OperationCanceledException)
                {
                    await Logger.Log("Operation was cancelled by the user.", Verbosity.Silent);
                    exitAfter = true;
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log("Encountered an error during log-in process.", Verbosity.Silent);
                    exitAfter = true;
                }
            }

            if (!GiftToken.IsValid && AllSettings.Values.Any(x => x.GetEnabledRegions().Any()))
            {
                try
                {
                    var text = "Code redeems are enabled, but the required tokens are missing.";
                    await Logger.Log(text, Verbosity.Silent);
                    text += "\nDo you want to log in?";
                    var title = "Missing gift token";
                    var msgBoxResult = Utils.ShowQuestion(text, title);
                    if (msgBoxResult != MessageBoxResult.Yes)
                    {
                        throw new OperationCanceledException();
                    }
                    GiftToken = await TokenLoader.GetGiftCookies() ?? throw new NullReferenceException();
                }
                catch (OperationCanceledException)
                {
                    await Logger.Log("Operation was cancelled by the user.", Verbosity.Silent);
                    exitAfter = true;
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log("Encountered an error during log-in process.", Verbosity.Silent);
                    exitAfter = true;
                }
            }


            await Logger.Log("Rewriting settings...", Verbosity.Detail);

            try
            {
                var serialized = JsonSerializer.Serialize(this, Program.JsonOptions);
                await File.WriteAllTextAsync(Program.SettingsPath, serialized);
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error);
                await Logger.Log("Failed to re-write settings!", Verbosity.Silent);
                await Utils.ExitFunction(true);
            }

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
            var failedRedeems = new List<BaseGame>();
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
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log("Failed to init game scripts", Verbosity.Silent);
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
                        await Logger.Log($"Failed to check into {game.ClassName}", Verbosity.Silent);
                        failedCheckin.Add(game);
                    }
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log($"Encountered an error while checking into {game.ClassName}", Verbosity.Silent);
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
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log($"Encountered an error while redeeming codes for {game.ClassName}", Verbosity.Silent);
                    failedRedeems.Add(game);
                }
            }
            await Logger.Log($"-------------", Verbosity.Silent);

            if (!anyCheckin && !anyRedeem)
            {
                await Logger.Log("It seems like nothing happened...", Verbosity.Silent);
                await Logger.Log("Make sure that you've configured everything correctly.", Verbosity.Silent);
                await Utils.ExitFunction(true);
                return;
            }

            if (failedCheckin.Count != 0)
            {
                var msg = "Failed daily check-in for the following games:";
                var sb = new StringBuilder();
                sb.AppendLine(msg);
                await Logger.Log(msg, Verbosity.Silent);
                foreach (var game in failedCheckin)
                {
                    msg = $"  - {game.ClassName}";
                    sb.AppendLine(msg);
                    await Logger.Log(msg, Verbosity.Silent);
                }
                msg = "Do you want to check-in manually?";
                sb.AppendLine(msg);
                await Logger.Log(msg, Verbosity.Silent);
                
                var text = sb.ToString();
                var title = "Check-in Failed";
                var msgBoxResult = Utils.ShowQuestion(text, title);
                if (msgBoxResult == MessageBoxResult.Yes)
                {
                    foreach (var game in failedCheckin)
                    {
                        Utils.OpenUrl(game.DailyManualUrl);
                    }
                }
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
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log("Failed to write tried codes to file", Verbosity.Silent);
                }
            }
        }
    }
}
