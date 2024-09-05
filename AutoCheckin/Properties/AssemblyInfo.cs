
using System.Reflection;

[assembly: AssemblyVersion(BuildInfo.Version)]
[assembly: AssemblyFileVersion(BuildInfo.Version)]
[assembly: AssemblyTitle(BuildInfo.Title)]


static class BuildInfo
{
    internal const string Version = "1.2.0";
    internal const string Title = "HoyoLab AutoCheckin";
}