using AutoCheckin.Enums;
using MiscUtil.IO;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace AutoCheckin
{
    public static class Logger
    {
        public static bool TryToString(object? source, [MaybeNullWhen(false)]out string message)
        {
            message = source?.ToString();
            return message is not null;
        }
        public static Verbosity LogVerbosity => (Program.MainManager?.VerbosityString is null ? Verbosity.FullDebug : Program.MainManager.Verbosity);
        static FileStream? logStream;
        public static StreamWriter LogWriter => new(new NonClosingStreamWrapper(logStream));
        public static readonly string DateFormat = "[yy-MM-d H:mm:ss]";
        public static string DateNow => DateTime.Now.ToString(DateFormat);

        public static readonly ConsoleColor DefaultColor = Console.ForegroundColor;
        public static string? ConstructMessage(params string[] args)
        {
            ArgumentNullException.ThrowIfNull(args, nameof(args));

            if (args.Length == 0)
            {
                return null;
            }
            var sb = new StringBuilder();
            sb.Append(DateNow);
            if (args.Length == 1)
            {
                sb.Append(' ');
                sb.Append(args[0]);
                return sb.ToString();
            }

            foreach (var item in args.SkipLast(1))
            {
                if (item is null)
                {
                    sb.Length = 0;
                    throw new ArgumentException("message may not contain null", nameof(args));
                }
                sb.Append(" [");
                sb.Append(item);
                sb.Append(']');
            }
            sb.Append(' ');
            sb.Append(args.Last());

            return sb.ToString();
        }
        internal static async Task Init()
        {
            if (LogVerbosity == Verbosity.Off)
            {
                return;
            }
            logStream = new FileStream("Latest.log", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            await Log($"Logger initialized.", verbosity: Verbosity.Detail);
        }
        public static async Task LogDirect(string message, Verbosity verbosity, ConsoleColor? consoleColor = null)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            if (verbosity > LogVerbosity)
            {
                return;
            }
            var color = consoleColor ?? DefaultColor;
            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            try
            {
                await Console.Out.WriteLineAsync(message);
                await using var writer = LogWriter;
                await writer.WriteLineAsync(message);
            }
            catch (Exception)
            {
                //ignore
            }
            finally
            {
                Console.ForegroundColor = originalColor;
            }
        }
        /// <summary>
        /// Unlike the others, this will ignore the log request if the object is null.
        /// </summary>
        public static async Task LogDirect(object message, Verbosity verbosity, ConsoleColor? color = null)
        {
            if (TryToString(message, out var log))
            {
                await LogDirect(log, verbosity, color);
            }
        }
        internal static async Task Log(string message, Verbosity verbosity, ConsoleColor? color = null, string[] ? namespaces = null)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            if (verbosity > LogVerbosity)
            {
                return;
            }
            var log = ConstructMessage(namespaces.AppendElement(message))!;
            await LogDirect(log, verbosity, color);
        }
    }
}
