using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace frmwpsoffice
{
    internal sealed class UpdateInfo
    {
        public Version Latest { get; set; }
        public string Tag { get; set; }
        public string PageUrl { get; set; }
        public string DownloadUrl { get; set; }
        public bool IsNewer { get; set; }
    }

    /// <summary>
    /// Checks the latest GitHub release of the project and compares it to the running version.
    /// </summary>
    internal static class UpdateChecker
    {
        private const string Repo = "ThanhNguyenVN93/WPS-office-uninstaller-tool";
        private const string LatestApi = "https://api.github.com/repos/" + Repo + "/releases/latest";
        public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";

        public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>Returns null when the check fails (offline, rate limited, no release yet...).</summary>
        public static Task<UpdateInfo> CheckAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
                    using (var wc = new WebClient())
                    {
                        wc.Headers[HttpRequestHeader.UserAgent] = "WPS-Uninstaller-Tool";
                        wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                        string json = wc.DownloadString(LatestApi);

                        string tag = Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                        if (tag == null) return null;

                        Version latest;
                        if (!Version.TryParse(tag.TrimStart('v', 'V'), out latest)) return null;

                        string page = Match(json, "\"html_url\"\\s*:\\s*\"(https://github\\.com/[^\"]+/releases/tag/[^\"]+)\"") ?? ReleasesPage;
                        string exe = Match(json, "\"browser_download_url\"\\s*:\\s*\"(https://github\\.com/[^\"]+\\.exe)\"");

                        // Compare using only the parts that are set so "1.3" == "1.3.0.0".
                        Version current = CurrentVersion;
                        return new UpdateInfo
                        {
                            Latest = latest,
                            Tag = tag,
                            PageUrl = page,
                            DownloadUrl = exe,
                            IsNewer = Normalize(latest).CompareTo(Normalize(current)) > 0
                        };
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Update check failed: " + ex.Message);
                    return null;
                }
            });
        }

        public static void OpenInBrowser(string url)
        {
            try
            {
                if (url != null && url.StartsWith("https://github.com/", StringComparison.OrdinalIgnoreCase))
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }

        private static Version Normalize(Version v)
        {
            return new Version(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
        }

        private static string Match(string input, string pattern)
        {
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }
    }
}
