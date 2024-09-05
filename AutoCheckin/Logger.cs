using AutoCheckin.Enums;
using MiscUtil.IO;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

namespace AutoCheckin
{
    public static class Logger
    {
        public static bool TryToString(object? source, [MaybeNullWhen(false)]out string message)
        {
            message = source?.ToString();
            return message is not null;
        }
        public static Verbosity logVerbosity => (Program.MainManager?.VerbosityString is null ? Verbosity.Detail : Program.MainManager.Verbosity);
        static FileStream? logStream;
        static StreamWriter LogWriter => new(new NonClosingStreamWrapper(logStream));
        public static readonly string DateFormat = "[yy-MM-d H:mm:ss]";
        public static string DateNow => DateTime.Now.ToString(DateFormat);
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
            if (logVerbosity == Verbosity.Off)
            {
                return;
            }
            logStream = new FileStream("Latest.log", FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            await Log($"Logger initialized.", verbosity: Verbosity.Detail);
        }
        public static async Task LogDirect(string message, Verbosity verbosity)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            if (verbosity > logVerbosity)
            {
                return;
            }
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
        }
        /// <summary>
        /// Unlike the others, this will ignore the log request if the object is null.
        /// </summary>
        public static async Task LogDirect(object message, Verbosity verbosity)
        {
            if (TryToString(message, out var log))
            {
                await LogDirect(log, verbosity);
            }
        }
        internal static async Task Log(string message, Verbosity verbosity, string[]? prefixNamespaces = null, string[]? postfixNamespaces = null)
        {
            ArgumentNullException.ThrowIfNull(message, nameof(message));
            if (verbosity > logVerbosity)
            {
                return;
            }
            var log = ConstructMessage(Utils.AppendElement(postfixNamespaces, message).PrependElements(prefixNamespaces))!;
            await LogDirect(log, verbosity);
        }
    }
}
