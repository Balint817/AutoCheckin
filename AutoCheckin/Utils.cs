using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace AutoCheckin
{
    public static class Utils
    {
        public static void ClearCurrentConsoleLine()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.BufferWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
        static Dictionary<MethodInfo, dynamic> _delegateCache = new();
        static Type[] _actionTypes = Enumerable.Range(1, 16).Select(x => Type.GetType("System.Action`"+x, true)).ToArray()!;
        static Type[] _funcTypes = Enumerable.Range(1, 17).Select(x => Type.GetType("System.Func`" + x, true)).ToArray()!;
        static dynamic ToDynamicDelegateSlow(MethodInfo mi, object? instance = null)
        {
            throw new NotImplementedException();
        }
        public static dynamic ToDynamicDelegate(this MethodInfo mi, object? instance = null)
        {
            if (!_delegateCache.TryGetValue(mi, out var result))
            {
                _delegateCache[mi] = result = ToDynamicDelegateUncached(mi, instance);
            }
            return result;
        }
        static dynamic ToDynamicDelegateUncached(MethodInfo mi, object? instance = null)
        {
            if (mi.IsGenericMethodDefinition)
            {
                throw new ArgumentException("Unconstructed generic method", nameof(mi));
            }
            if (mi.IsAbstract)
            {
                throw new ArgumentException("Abstract method", nameof(mi));
            }
            bool isStatic = mi.IsStatic;
            if (!isStatic)
            {
                ArgumentNullException.ThrowIfNull(instance, nameof(instance));
                var declaringType = mi.DeclaringType ?? throw new ArgumentException("DeclaringType was null", nameof(mi));
                if (!declaringType.IsAssignableFrom(instance.GetType()))
                {
                    throw new ArgumentException("method's instance cannot be assigned from passed instance", nameof(instance));
                };
            }

            var parameters = mi.GetParameters();
            if (parameters.Length > 16)
            {
                return ToDynamicDelegateSlow(mi, instance);
            }
            var returnType = mi.ReturnType;
            Type delegateType;
            Type[] parameterTypes;
            if (returnType == typeof(void))
            {
                if (parameters.Length == 0)
                {
                    return isStatic ? mi.CreateDelegate<Action>() : mi.CreateDelegate<Action>(instance);
                }
                parameterTypes = parameters.Select(x => x.ParameterType).ToArray();
                delegateType = _actionTypes[parameters.Length-1].MakeGenericType(parameterTypes);
                return mi.CreateDelegate(delegateType);
            }
            if (parameters.Length == 0)
            {
                delegateType = typeof(Func<>).MakeGenericType(returnType);
            }
            else
            {
                parameterTypes = parameters.Select(x => x.ParameterType).Append(returnType).ToArray();
                delegateType = _funcTypes[parameters.Length - 1].MakeGenericType(parameterTypes);
            }
            return isStatic ? mi.CreateDelegate(delegateType) : mi.CreateDelegate(delegateType, instance);
        }
        public static async Task ExitFunction(bool exitEnv = true)
        {
            if (Program.DefaultMessageBoxAction == MessageBoxAction.Reject)
            {
                if (exitEnv)
                {
                    Environment.Exit(0);
                }
                return;
            }
            await Console.Out.WriteLineAsync();
            await Console.Out.WriteLineAsync("Press Enter to exit.");
            while (true)
            {
                var keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    if (exitEnv)
                    {
                        Environment.Exit(0);
                    }
                    return;
                }
                await Task.Delay(10);
            }
        }
        public static bool OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
                return true;
            }
            catch
            {
                try
                {
                    // hack because of this: https://github.com/dotnet/corefx/issues/10361
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        url = url.Replace("&", "^&");
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        Process.Start("xdg-open", url);
                    }
                    else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    {
                        Process.Start("open", url);
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return true;
        }
        public static T[] AppendElement<T>(this T[]? original, T element)
        {
            if (original is null)
            {
                return new T[] { element };
            }
            var newArray = new T[original.Length + 1];
            original.CopyTo(newArray, 0);
            newArray[original.Length] = element;
            return newArray;
        }
        public static T[] AppendElements<T>(this T[]? original, params T[]? elements)
        {
            T[] newArray;
            if (original is null)
            {
                if (elements is null)
                {
                    return Array.Empty<T>();
                }
                newArray = new T[elements.Length];
                elements.CopyTo(newArray, 0);
                return newArray;
            }
            else if (elements is null)
            {
                newArray = new T[original.Length];
                original.CopyTo(newArray, 0);
                return newArray;
            }
            newArray = new T[original.Length + elements.Length];
            original.CopyTo(newArray, 0);
            elements.CopyTo(newArray, original.Length);
            return newArray;
        }
        public static T[] PrependElement<T>(this T[]? original, T element)
        {
            if (original is null)
            {
                return new T[] { element };
            }
            var newArray = new T[original.Length + 1];
            original.CopyTo(newArray, 1);
            newArray[0] = element;
            return newArray;
        }
        public static T[] PrependElements<T>(this T[]? original, params T[]? elements)
        {
            T[] newArray;
            if (original is null)
            {
                if (elements is null)
                {
                    return Array.Empty<T>();
                }
                newArray = new T[elements.Length];
                elements.CopyTo(newArray, 0);
                return newArray;
            }
            else if (elements is null)
            {
                newArray = new T[original.Length];
                original.CopyTo(newArray, 0);
                return newArray;
            }
            newArray = new T[original.Length + elements.Length];
            original.CopyTo(newArray, elements.Length);
            elements.CopyTo(newArray, 0);
            return newArray;
        }
        internal static async ValueTask ExecuteScript(string pathToScript, string? args = null)
        {
            var scriptArguments = "-ExecutionPolicy Bypass -File \"" + pathToScript + $"\" {args}";
            var processStartInfo = new ProcessStartInfo("powershell.exe", scriptArguments);

            var process = new Process
            {
                StartInfo = processStartInfo
            };
            process.Start();
            await process.WaitForExitAsync();
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public static Task NotImplementedAsync()
        {
            static async Task f() { throw new NotImplementedException(); }
            return f();
        }
        public static Task<T> NotImplementedAsync<T>()
        {
            static async Task<T> f() { throw new NotImplementedException(); }
            return f();
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously

        public static MessageBoxResult ShowQuestion(string text, string title)
        {
            return Program.DefaultMessageBoxAction switch
            {
                MessageBoxAction.None => MessageBox.Show(text, title, MessageBoxButton.YesNo),
                MessageBoxAction.Accept => MessageBoxResult.Yes,
                MessageBoxAction.Reject => MessageBoxResult.No,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
        public static MessageBoxResult ShowInfo(string text, string title)
        {
            return Program.DefaultMessageBoxAction switch
            {
                MessageBoxAction.None => MessageBox.Show(text, title, MessageBoxButton.OK),
                MessageBoxAction.Accept or MessageBoxAction.Reject => MessageBoxResult.None,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }
    }
}
