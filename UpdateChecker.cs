using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace frmwpsoffice
{
    internal sealed class UpdateInfo
    {
        public Version Latest { get; set; }
        public string Tag { get; set; }
        public string PageUrl { get; set; }
        public bool IsNewer { get; set; }
        /// <summary>Release asset URLs (null when the release has no exe / no SHA256SUMS.txt).</summary>
        public string ExeUrl { get; set; }
        public string ExeName { get; set; }
        public string SumsUrl { get; set; }
        /// <summary>Release notes as plain text (the "What's new" text), empty when the release has none.</summary>
        public string Notes { get; set; }
    }

    /// <summary>
    /// Checks the latest GitHub release of the project and compares it to the running version.
    /// </summary>
    internal static class UpdateChecker
    {
        private const string Repo = "ThanhNguyenVN93/WPS-office-uninstaller-tool";
        private const string LatestApi = "https://api.github.com/repos/" + Repo + "/releases/latest";
        private const string TagApi = "https://api.github.com/repos/" + Repo + "/releases/tags/";
        public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";

        public static Version CurrentVersion => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>Returns null when the check fails (offline, rate limited, no release yet...).</summary>
        public static Task<UpdateInfo> CheckAsync()
        {
            return FetchAsync(LatestApi);
        }

        /// <summary>Release info (with notes) of the version that is running now; null when unavailable.</summary>
        public static Task<UpdateInfo> GetCurrentReleaseAsync()
        {
            return FetchAsync(TagApi + "v" + CurrentVersion.ToString(3));
        }

        private static Task<UpdateInfo> FetchAsync(string apiUrl)
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
                        wc.Encoding = Encoding.UTF8;
                        string json = wc.DownloadString(apiUrl);

                        string tag = Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                        if (tag == null) return null;

                        Version latest;
                        if (!Version.TryParse(tag.TrimStart('v', 'V'), out latest)) return null;

                        string page = Match(json, "\"html_url\"\\s*:\\s*\"(https://github\\.com/[^\"]+/releases/tag/[^\"]+)\"") ?? ReleasesPage;

                        string exeUrl = null, exeName = null, sumsUrl = null;
                        foreach (Match m in Regex.Matches(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+)\""))
                        {
                            string u = m.Groups[1].Value;
                            string name = u.Substring(u.LastIndexOf('/') + 1);
                            if (exeUrl == null && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) { exeUrl = u; exeName = name; }
                            else if (name.Equals(Updater.SumsFileName, StringComparison.OrdinalIgnoreCase)) sumsUrl = u;
                        }

                        string body = Match(json, "\"body\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
                        string notes = body == null ? "" : ToPlainText(JsonUnescape(body));

                        // Compare using only the parts that are set so "1.3" == "1.3.0.0".
                        Version current = CurrentVersion;
                        return new UpdateInfo
                        {
                            Latest = latest,
                            Tag = tag,
                            PageUrl = page,
                            ExeUrl = exeUrl,
                            ExeName = exeName,
                            SumsUrl = sumsUrl,
                            Notes = notes,
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

        internal static Version Normalize(Version v)
        {
            return new Version(Math.Max(v.Major, 0), Math.Max(v.Minor, 0), Math.Max(v.Build, 0), Math.Max(v.Revision, 0));
        }

        private static string Match(string input, string pattern)
        {
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        // ------------------------------------------------------------------
        // Release notes helpers
        // ------------------------------------------------------------------

        /// <summary>Decodes the escapes of a JSON string literal (the part between the quotes).</summary>
        internal static string JsonUnescape(string s)
        {
            if (s == null) return "";
            var sb = new StringBuilder(s.Length);
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c != '\\' || i == s.Length - 1) { sb.Append(c); continue; }
                char n = s[++i];
                switch (n)
                {
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case '/': sb.Append('/'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    case 'u':
                        int code;
                        if (i + 4 < s.Length + 0 && int.TryParse(s.Substring(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                        {
                            sb.Append((char)code);
                            i += 4;
                        }
                        else sb.Append("\\u");
                        break;
                    default: sb.Append(n); break;
                }
            }
            return sb.ToString();
        }

        /// <summary>Turns the Markdown of a release into readable plain text with Windows line breaks.</summary>
        internal static string ToPlainText(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown)) return "";
            string t = markdown.Replace("\r\n", "\n").Replace('\r', '\n');

            t = Regex.Replace(t, @"```[a-zA-Z]*\n?", "");                                  // code fences
            t = Regex.Replace(t, @"!?\[([^\]]*)\]\(([^)]*)\)", m =>                       // [text](url) -> text
                m.Groups[1].Value.Length > 0 ? m.Groups[1].Value : m.Groups[2].Value);
            t = Regex.Replace(t, @"^[ \t]{0,3}#{1,6}[ \t]*(.*?)[ \t]*#*[ \t]*$", "$1", RegexOptions.Multiline); // headings
            t = Regex.Replace(t, @"^[ \t]*[-*+][ \t]+", "• ", RegexOptions.Multiline);     // bullets
            t = Regex.Replace(t, @"(\*\*|__)(.+?)\1", "$2");                               // bold
            t = Regex.Replace(t, @"(?<![\w*])\*(?!\s)([^*\n]+?)\*(?![\w*])", "$1");        // italic
            t = t.Replace("`", "");
            t = Regex.Replace(t, @"\n{3,}", "\n\n").Trim();
            return t.Replace("\n", "\r\n");
        }

        // ------------------------------------------------------------------
        // "What's new" after an update: remember the last version that was run
        // ------------------------------------------------------------------

        private static readonly string StateFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WPSOfficeUninstaller", "last_version");

        /// <summary>The version that ran last time, or null on the very first run.</summary>
        public static Version LastRunVersion
        {
            get
            {
                try
                {
                    Version v;
                    return File.Exists(StateFile) && Version.TryParse(File.ReadAllText(StateFile).Trim(), out v) ? v : null;
                }
                catch { return null; }
            }
        }

        private static readonly string PendingFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WPSOfficeUninstaller", "whatsnew_pending");

        /// <summary>Set by the update step (it runs the NEW code), so the first start after an update shows the notes
        /// even when the previous version never recorded its version.</summary>
        public static void MarkWhatsNewPending()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PendingFile));
                File.WriteAllText(PendingFile, "1");
            }
            catch { }
        }

        /// <summary>True once after an update; the marker is removed.</summary>
        public static bool ConsumeWhatsNewPending()
        {
            try
            {
                if (!File.Exists(PendingFile)) return false;
                File.Delete(PendingFile);
                return true;
            }
            catch { return false; }
        }

        public static void RememberCurrentVersion()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(StateFile));
                File.WriteAllText(StateFile, CurrentVersion.ToString(3));
            }
            catch { }
        }
    }
}
