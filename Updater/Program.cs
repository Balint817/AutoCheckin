using Constants;
using System;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Xml.Linq;

namespace Updater
{
    public enum UpdaterExitCode
    {
        Error = -1,
        UpToDate = 1,
        UpdateSkipped = 2,
        Update = 3,
    }
    internal class Program
    {
        static readonly string downloadUrl = "https://github.com/Balint817/AutoCheckin/releases/latest/download/net6.0-windows.zip";
        static readonly string versionApiUrl = "https://api.github.com/repos/Balint817/AutoCheckin/tags";
        internal static bool Popupless { get; private set; } = false;
        internal static bool RestartAfter { get; private set; } = false;
        internal static string[] OriginalArgs { get; private set; } = Array.Empty<string>();
        static async Task Main(string[] args)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule!.FileName!)!;
            try
            {
                await WrapMain(args);
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync(ex.ToString());
                //Environment.Exit(-1);
            }
        }

        private static async Task WrapMain(string[] args)
        {
            args ??= Array.Empty<string>();
            await Console.Out.WriteLineAsync($"Launched updater with args: {string.Join(' ', args)}");
            var cutoffIndex = args.ToList().IndexOf("--original");
            if (cutoffIndex != -1)
            {
                if (cutoffIndex == args.Length - 1)
                {
                    args = args[..cutoffIndex];
                }
                else
                {
                    OriginalArgs = args[(cutoffIndex + 1)..];
                    args = args[..cutoffIndex];
                }
            }

            if (args.Contains("-popupless"))
            {
                Popupless = true;
            }

            if (args.Contains("-restart"))
            {
                RestartAfter = true;
            }

            if (args.Contains("-forceupdate"))
            {
                await Update();
                return;
            }

            var exitCode = await UpdateCheck();
            Environment.Exit((int)exitCode);
        }

        private static async Task Update()
        {
            if (File.Exists("download"))
            {
                File.Delete("download");
            }
            if (!Directory.Exists("download"))
            {
                Directory.CreateDirectory("download");
            }
            var client = new HttpClient();
            var requestMsg = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
            requestMsg.Headers.UserAgent.ParseAdd("HoyoLab-AutoCheckin-Updater");
            var responseMsg = await client.SendAsync(requestMsg);
            responseMsg.EnsureSuccessStatusCode();
            var responseStream = await responseMsg.Content.ReadAsStreamAsync();
            await Console.Out.WriteLineAsync("1");
            if (File.Exists("download.zip"))
            {
                File.Delete("download.zip");
            }
            await Console.Out.WriteLineAsync("2");
            using (var fileStream = File.Create("download.zip"))
            {
                await responseStream.CopyToAsync(fileStream);
                await fileStream.FlushAsync();
            }
            await Console.Out.WriteLineAsync("3");
            System.IO.Compression.ZipFile.ExtractToDirectory("download.zip", "download", true);
            await Console.Out.WriteLineAsync("4");
            File.Delete("download.zip");
            await Console.Out.WriteLineAsync("5");
            while (true)
            {
                try
                {
                    using var fs = File.OpenWrite("AutoCheckin.exe");
                    break;
                }
                catch (Exception)
                {

                }
                await Task.Delay(10);
            }
            foreach (var filePath in Directory.EnumerateFiles("download"))
            {
                var filename = Path.GetFileName(filePath);
                if (filename.ToLowerInvariant().StartsWith("updater."))
                {
                    continue;
                }
                File.Move(filePath, Path.GetFileName(filePath), true);
            }
            await Console.Out.WriteLineAsync("6");
            if (RestartAfter)
            {
                var psi = new ProcessStartInfo("AutoCheckin.exe")
                {
                    UseShellExecute = true
                };
                if (OriginalArgs is not null)
                {
                    foreach (var item in OriginalArgs)
                    {
                        psi.ArgumentList.Add(item);
                    }
                }
                psi.ArgumentList.Add("-updated");
                Process.Start(psi);
                return;

            }
        }

        static async Task<UpdaterExitCode> UpdateCheck()
        {

            var fileInfo = FileVersionInfo.GetVersionInfo("AutoCheckin.exe");
            var currentVersion = new Version(fileInfo.FileVersion!);

            var client = new HttpClient();

            var requestMsg = new HttpRequestMessage(HttpMethod.Get, versionApiUrl);
            requestMsg.Headers.UserAgent.ParseAdd("HoyoLab-AutoCheckin-Updater");
            var responseMsg = await client.SendAsync(requestMsg);
            var responseBody = await responseMsg.Content.ReadAsStringAsync();
            await Console.Out.WriteLineAsync(responseBody);
            var responseJson = JsonSerializer.Deserialize<GithubResponse[]>(responseBody)!;
            var latestRelease = responseJson[0];
            var updateVersion = new Version(latestRelease.TagName);

            if (updateVersion <= currentVersion)
            {
                return UpdaterExitCode.UpToDate;
            }

            var lastSkippedVersion = new Version(0, 0);
            bool changed = false;
            JsonObject settingsJsonObj = null!; // assignment required because the compiler doesn't realize the for loop will always run and assign to this
            if (File.Exists(Values.SettingsPath))
            {
                try
                {
                    for (var i = 0; i < 1; i++)
                    {

                        settingsJsonObj = JsonSerializer.Deserialize<JsonObject>(await File.ReadAllTextAsync(Values.SettingsPath))!;
                        // let it throw nullref if it is, since then we need to re-write it anyway
                        if (settingsJsonObj.TryGetPropertyValue(Values.LastVersionKey, out var node))
                        {
                            var jValue = node!.AsValue();
                            if (jValue.TryGetValue<string>(out var lastSkippedVersionString))
                            {

                                if (Version.TryParse(lastSkippedVersionString, out var parsedVersion))
                                {
                                    lastSkippedVersion = parsedVersion;
                                    break;
                                }
                            }
                        }
                        settingsJsonObj[Values.LastVersionKey] = "0.0";
                        changed = true;
                    }
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.ToString());
                    settingsJsonObj = new()
                    {
                        { Values.LastVersionKey, "0.0" }
                    };
                    changed = true;
                }
            }
            await Console.Out.WriteLineAsync(lastSkippedVersion.ToString());
            await Console.Out.WriteLineAsync(updateVersion.ToString());
            if (lastSkippedVersion >= updateVersion)
            {
                return UpdaterExitCode.UpdateSkipped;
            }

            var exitCode = UpdaterExitCode.Update;
            if (!Popupless)
            {
                var text = "A new update is available.\nWould you like to install this update?";
                var title = "Update available";
                var msgBoxResult = MessageBox.Show(text, title, MessageBoxButton.YesNo);
                if (msgBoxResult != MessageBoxResult.Yes)
                {
                    exitCode = UpdaterExitCode.UpdateSkipped;
                    settingsJsonObj[Values.LastVersionKey] = updateVersion.ToString();
                    changed = true;
                }
            }

            if (changed)
            {
                var settingsJsonString = JsonSerializer.Serialize(settingsJsonObj, Values.JsonOptions);
                // since the program wouldn't remember we skipped a version if it errored,
                // it's better to just let this throw and make it ask for an update again.
                await File.WriteAllTextAsync(Values.SettingsPath, settingsJsonString);
            }

            return exitCode;
        }
    }
}
