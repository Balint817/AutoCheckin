using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace AutoCheckin
{

    internal class Program
    {
        internal static HttpClient Client { get; set; } = new();

        internal static JsonSerializerOptions JsonOptions = new()
        {
            AllowTrailingCommas = true,
            NumberHandling = JsonNumberHandling.Strict,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
        };
        internal static string SettingsPath = "settings.json";
        internal static string TriedCodesPath = "triedCodes.json";
        internal static MainManager MainManager = null!;
        public enum MessageBoxAction
        {
            None,
            Accept,
            Reject
        }
        public static MessageBoxAction DefaultMessageBoxAction = MessageBoxAction.None;
        static async Task Main(string[] args)
        {
            if (args.Contains("-help"))
            {
                await Console.Out.WriteLineAsync("-help       Displays this menu");
                await Console.Out.WriteLineAsync("-reset      Resets all settings");
                await Console.Out.WriteLineAsync("-nopopups   Rejects all message boxes");
                await Console.Out.WriteLineAsync("-yespopups  Accepts all message boxes");
                await Utils.ExitFunction(false);
                return;
            }
            if (args.Contains("-nopopups"))
            {
                if (args.Contains("-yespopups"))
                {
                    throw new ArgumentException("conflicting arguments", nameof(args));
                }
                DefaultMessageBoxAction = MessageBoxAction.Reject;
            }

            if (args.Contains("-yespopups"))
            {
                DefaultMessageBoxAction = MessageBoxAction.Accept;
            }
            //await Testing();
            await TrueMain(args);
        }

        static async Task Testing()
        {
            await Task.CompletedTask;
        }

        static async Task TrueMain(string[] args)
        {
            try
            {
                await Logger.Init();
            }
            catch (Exception)
            {
                Console.WriteLine("Failed to initialize Logger.");
                await Utils.ExitFunction(false);
                return;
            }
            bool? error = false;
            try
            {
                if (!args.Contains("-reset"))
                {
                    MainManager = JsonSerializer.Deserialize<MainManager>(await File.ReadAllTextAsync(SettingsPath), JsonOptions)!;
                }
                else
                {
                    MainManager = new();
                }
                if (MainManager is null)
                {
                    MainManager = new();
                    error = true;
                }
            }
            catch (FileNotFoundException)
            {
                error = null;
                MainManager = new();
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error);
                error = true;
                MainManager = new();
            }
            if (error != false)
            {
                if (error == true)
                {
                    await Logger.Log("Encountered an error while reading settings.", Verbosity.Silent);
                }
                try
                {
                    await File.WriteAllTextAsync(SettingsPath, JsonSerializer.Serialize(MainManager, JsonOptions));
                    await Logger.Log($"Re-written settings to '{SettingsPath}'.", Verbosity.Silent);
                }
                catch (Exception ex)
                {
                    await Logger.Log(ex.ToString(), Verbosity.Error);
                    await Logger.Log($"Failed to re-write settings to '{SettingsPath}'. You will have to fix the file manually.", Verbosity.Silent);
                }
                if (error == null)
                {
                    await Logger.Log("Couldn't find settings.json", Verbosity.Silent);
                    await Logger.Log("If this was your first launch, you will need to exit the program and edit the newly created file.", Verbosity.Silent);
                }
                await Utils.ExitFunction(false);
                return;
            }
            try
            {
                await MainManager.Init();
                await Logger.Log("Initalized MainManager", Verbosity.Detail);
            }
            catch (Exception ex)
            {
                await Logger.Log(ex.ToString(), Verbosity.Error);
                await Logger.Log("Failed to initialize program.", Verbosity.Silent);
                await Utils.ExitFunction(false);
                return;
            }
            await MainManager.RunAll();
            if (MainManager.AutoCloseOnSuccess != true)
            {
                await Utils.ExitFunction(false);
            }
        }
    }
}
