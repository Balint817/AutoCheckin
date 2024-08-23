using Constants;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using Updater;

namespace AutoCheckin
{
    public enum MessageBoxAction
    {
        None,
        Accept,
        Reject
    }
    public class Program
    {
        internal static HttpClient Client { get; set; } = new();
        internal static JsonSerializerOptions JsonOptions => Values.JsonOptions;
        internal const string SettingsPath = Values.SettingsPath;
        internal const string TriedCodesPath = Values.TriedCodesPath;
        internal static MainManager MainManager = null!;
        public static MessageBoxAction DefaultMessageBoxAction = MessageBoxAction.None;

        static async Task Update(bool exitAfter, string[] programArgs)
        {
            if (!UpdatesEnabled)
            {
                return;
            }
            await Logger.Log("Checking for updates...", Verbosity.Silent);
            var startInfo = new ProcessStartInfo("Updater.exe")
            {
                UseShellExecute = true,
            };
            if (!exitAfter)
            {
                startInfo.ArgumentList.Add("-restart");
            }
            if (DefaultMessageBoxAction != MessageBoxAction.None)
            {
                startInfo.ArgumentList.Add("-popupless");
            }
            var process = Process.Start(startInfo)!;
            await process.WaitForExitAsync();
            switch ((UpdaterExitCode)process.ExitCode)
            {
                case UpdaterExitCode.UpToDate:
                    await Logger.Log("Up to date.", Verbosity.Silent);
                    if (exitAfter)
                    {
                        await Utils.ExitFunction(true);
                    }
                    break;
                case UpdaterExitCode.UpdateSkipped:
                    await Logger.Log("Update skipped.", Verbosity.Silent);
                    if (exitAfter)
                    {
                        await Utils.ExitFunction(true);
                    }
                    break;
                case UpdaterExitCode.Update:
                    await Logger.Log("Update requested. Closing...", Verbosity.Silent);
                    startInfo.ArgumentList.Add("-forceupdate");
                    startInfo.ArgumentList.Add("--original");
                    foreach (var arg in programArgs)
                    {
                        startInfo.ArgumentList.Add(arg);
                    }
                    Process.Start(startInfo);
                    Environment.Exit(0);
                    break;
                default:
                    await Logger.Log("Encountered an error during the update check", Verbosity.Silent);
                    if (exitAfter)
                    {
                        await Utils.ExitFunction(true);
                    }
                    break;
            }
        }
        static async Task Main(string[] args)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName!)!;
            if (args.Contains("-help"))
            {
                await Console.Out.WriteLineAsync("-help       Displays this menu");
                await Console.Out.WriteLineAsync("-reset      Resets all settings");
                await Console.Out.WriteLineAsync("-nopopups   Rejects all message boxes");
                await Console.Out.WriteLineAsync("-yespopups  Accepts all message boxes");
                await Console.Out.WriteLineAsync("-update     Exits after updating");
                await Console.Out.WriteLineAsync("-noupdate   Disables update check");
                await Console.Out.WriteLineAsync();
                await Console.Out.WriteLineAsync("If popups are skipped, updates are automatically accepted.");
                await Console.Out.WriteLineAsync("If popups are rejected, 'Press enter to exit' is also skipped");
                await Console.Out.WriteLineAsync();
                await Console.Out.WriteLineAsync("-updated    Used internally after an update");
                await Utils.ExitFunction(false);
                return;
            }

            if (args.Contains("-noupdate"))
            {
                if (args.Contains("-update"))
                {
                    throw new ArgumentException("conflicting arguments", nameof(args));
                }
                UpdatesEnabled = false;
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

        internal static bool UpdatesEnabled { get; private set; } = true;
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
                await Console.Out.WriteLineAsync("Failed to initialize Logger.");
                await Utils.ExitFunction(false);
                return;
            }

            if (args.Contains("-updated"))
            {
                await Console.Out.WriteLineAsync("Successfully installed update.");
            }
            else
            {
                await Update(args.Contains("-update"), args);
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
