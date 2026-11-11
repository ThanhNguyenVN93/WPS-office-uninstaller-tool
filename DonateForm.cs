// DonateForm.cs — copy vào project là dùng (C# 2.0 / .NET 3.5 trở lên).
//
//   Dùng:   NTDonate.DonateForm.ShowDonate(this);
//           NTDonate.DonateForm.ShowDonate(this, false);   // false = nền sáng
//
// Chọn engine tự động:
//   - Windows 8 trở lên + có WebView2 Runtime + app chạy .NET 4.x + có 2 file DLL WebView2
//       -> WebView2 (Chromium)
//   - Còn lại (Windows 7 SP1, thiếu runtime/DLL, app .NET 3.5...) -> WebBrowser (IE) cũ
//
// WebView2 được nạp bằng reflection nên project KHÔNG cần reference nó để build.
// Muốn bật WebView2: chép 2 file vào cạnh file .exe (lấy từ NuGet Microsoft.Web.WebView2, thư mục lib\net462):
//     Microsoft.Web.WebView2.Core.dll
//     Microsoft.Web.WebView2.WinForms.dll
//   và WebView2Loader.dll (runtimes\win-x86\native hoặc win-x64\native, đúng bitness của app).
// Không chép cũng không sao: tự dùng WebBrowser cũ.
//
// Win7: WebBrowser dùng Internet Explorer của máy. Code tự bật TLS 1.1/1.2 cho IE (HKCU,
// không cần admin) trước khi mở trang. Nếu máy chưa có IE11 / KB3140245 mà vẫn không mở được
// https thì cài IE11 (hoặc bật tay trong Internet Options > Advanced).

using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace NTDonate
{
    public class DonateForm : Form
    {
        // Đổi sang domain của bạn nếu cần
        private const string BaseUrl = "https://nguyenthanhdev.com/?ntd_embed=1";

        private readonly string _url;
        private Control _view;
        private bool _usingWebView2;
        private CheckBox _chkDontShow;

        public static void ShowDonate(IWin32Window owner)
        {
            ShowDonate(owner, true);
        }

        public static void ShowDonate(IWin32Window owner, bool dark)
        {
            using (DonateForm f = new DonateForm(dark))
            {
                if (owner != null) f.ShowDialog(owner);
                else f.ShowDialog();
            }
        }

        public DonateForm(bool dark)
        {
            _url = BaseUrl + "&theme=" + (dark ? "dark" : "light");

            Text = "Ủng hộ tác giả";
            ClientSize = new Size(440, 660);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = dark ? Color.FromArgb(27, 29, 35) : Color.White;

            BuildFooter(dark);

            if (!TryCreateWebView2())
            {
                CreateOldBrowser();
            }
        }

        /// <summary>true nếu người dùng tick "Không hiện lại".</summary>
        public bool DontShowAgain
        {
            get { return _chkDontShow != null && _chkDontShow.Checked; }
        }

        private void BuildFooter(bool dark)
        {
            Panel footer = new Panel();
            footer.Dock = DockStyle.Bottom;
            footer.Height = 40;
            footer.BackColor = dark ? Color.FromArgb(27, 29, 35) : Color.White;

            _chkDontShow = new CheckBox();
            _chkDontShow.Text = "Don't show this again";
            _chkDontShow.AutoSize = true;
            _chkDontShow.Location = new Point(12, 11);
            _chkDontShow.ForeColor = dark ? Color.Gainsboro : Color.Black;

            Button close = new Button();
            close.Text = "Close";
            close.Size = new Size(90, 26);
            close.Location = new Point(440 - 90 - 12, 7);
            close.DialogResult = DialogResult.OK;
            close.UseVisualStyleBackColor = true;

            footer.Controls.Add(_chkDontShow);
            footer.Controls.Add(close);
            Controls.Add(footer);
            AcceptButton = close;
            CancelButton = close;
        }

        /// <summary>
        /// true nếu máy có thể chạy bản WebView2 (Win8+, CLR4, đủ 2 DLL cạnh exe, đã cài WebView2 Runtime).
        /// Trang donate là web hiện đại: engine IE cũ hiển thị trống, nên chỉ nên dùng form web khi WebView2 dùng được.
        /// </summary>
        public static bool IsWebView2Usable()
        {
            try
            {
                if (!IsWin8OrNewer() || Environment.Version.Major < 4) return false;
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string core = Path.Combine(dir, "Microsoft.Web.WebView2.Core.dll");
                if (!File.Exists(core) || !File.Exists(Path.Combine(dir, "Microsoft.Web.WebView2.WinForms.dll"))) return false;
                Assembly coreAsm = Assembly.LoadFrom(core);
                Type envType = coreAsm.GetType("Microsoft.Web.WebView2.Core.CoreWebView2Environment", true);
                MethodInfo ver = envType.GetMethod("GetAvailableBrowserVersionString", Type.EmptyTypes);
                if (ver == null) return false;
                string s = ver.Invoke(null, null) as string;
                return !string.IsNullOrEmpty(s);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra nhanh xem có mạng và trang donate có truy cập được không (tối đa ~2.5s).
        /// Dùng để quyết định hiện form web hay quay về form QR cục bộ.
        /// </summary>
        public static bool IsSiteReachable()
        {
            try
            {
                if (!System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable()) return false;
                System.Net.ServicePointManager.SecurityProtocol |= (System.Net.SecurityProtocolType)3072; // TLS 1.2
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(BaseUrl);
                req.Method = "HEAD";
                req.Timeout = 2500;
                req.AllowAutoRedirect = true;
                using (System.Net.WebResponse r = req.GetResponse())
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>Windows 8 / Server 2012 (6.2) trở lên. Win7 SP1 là 6.1.</summary>
        private static bool IsWin8OrNewer()
        {
            Version v = Environment.OSVersion.Version;
            return v.Major > 6 || (v.Major == 6 && v.Minor >= 2);
        }

        // ---------------- WebView2 (reflection) ----------------

        private bool TryCreateWebView2()
        {
            try
            {
                if (!IsWin8OrNewer()) return false;
                // Process .NET 2.0/3.5 (CLR2) không nạp được WebView2
                if (Environment.Version.Major < 4) return false;

                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string core = Path.Combine(dir, "Microsoft.Web.WebView2.Core.dll");
                string wf = Path.Combine(dir, "Microsoft.Web.WebView2.WinForms.dll");
                if (!File.Exists(core) || !File.Exists(wf)) return false;

                Assembly coreAsm = Assembly.LoadFrom(core);
                Assembly wfAsm = Assembly.LoadFrom(wf);

                // Kiểm tra đã cài WebView2 Runtime chưa (ném lỗi nếu chưa có)
                Type envType = coreAsm.GetType("Microsoft.Web.WebView2.Core.CoreWebView2Environment", true);
                MethodInfo ver = envType.GetMethod("GetAvailableBrowserVersionString", Type.EmptyTypes);
                if (ver == null) return false;
                object verStr = ver.Invoke(null, null);
                if (verStr == null || ((string)verStr).Length == 0) return false;

                // Thư mục dữ liệu ghi được (tránh lỗi khi app nằm trong Program Files)
                if (Environment.GetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER") == null)
                {
                    string exeName = Path.GetFileNameWithoutExtension(Application.ExecutablePath);
                    string data = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Path.Combine(exeName, "WebView2"));
                    Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", data);
                }

                Type wvType = wfAsm.GetType("Microsoft.Web.WebView2.WinForms.WebView2", true);
                Control wv = (Control)Activator.CreateInstance(wvType);
                wv.Dock = DockStyle.Fill;

                // Nếu khởi tạo thất bại về sau -> chuyển sang WebBrowser cũ
                EventInfo ev = wvType.GetEvent("CoreWebView2InitializationCompleted");
                if (ev != null)
                {
                    MethodInfo h = typeof(DonateForm).GetMethod("OnWebView2Init",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    ev.AddEventHandler(wv, Delegate.CreateDelegate(ev.EventHandlerType, this, h));
                }

                Controls.Add(wv);
                wv.BringToFront();
                _view = wv;
                _usingWebView2 = true;

                // Gán Source trước khi khởi tạo -> control tự khởi tạo rồi điều hướng
                wvType.GetProperty("Source").SetValue(wv, new Uri(_url), null);
                return true;
            }
            catch
            {
                RemoveView();
                return false;
            }
        }

        // Được gọi qua reflection (tham số thật là CoreWebView2InitializationCompletedEventArgs)
        private void OnWebView2Init(object sender, EventArgs e)
        {
            try
            {
                PropertyInfo ok = e.GetType().GetProperty("IsSuccess");
                bool success = ok != null && (bool)ok.GetValue(e, null);
                if (!success && !IsDisposed)
                {
                    RemoveView();
                    CreateOldBrowser();
                }
            }
            catch
            {
                if (!IsDisposed)
                {
                    RemoveView();
                    CreateOldBrowser();
                }
            }
        }

        // ---------------- WebBrowser (IE) cũ ----------------

        /// <summary>
        /// Chỉ Win7: bật TLS 1.1 + 1.2 cho IE (HKCU, không cần admin) nếu đang tắt.
        /// Chỉ ghi khi thiếu bit; IE11 thường đã có sẵn nên sẽ không đổi gì.
        /// </summary>
        private static void EnsureTls12ForIE()
        {
            try
            {
                if (IsWin8OrNewer()) return;
                const int Tls11And12 = 0xA00; // TLS 1.1 (0x200) + TLS 1.2 (0x800)
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Internet Settings"))
                {
                    if (k == null) return;
                    object cur = k.GetValue("SecureProtocols");
                    int v = (cur is int) ? (int)cur : 0xA0;
                    if ((v & Tls11And12) != Tls11And12)
                    {
                        k.SetValue("SecureProtocols", v | Tls11And12, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// WebBrowser mặc định chạy chế độ IE7 nên trang hiện đại hiển thị trống. Đặt FEATURE_BROWSER_EMULATION
        /// (HKCU, không cần admin) theo phiên bản IE của máy trước khi tạo control.
        /// </summary>
        private static void EnsureBrowserEmulation()
        {
            try
            {
                int major = 0;
                using (Microsoft.Win32.RegistryKey ie = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Internet Explorer"))
                {
                    if (ie == null) return;
                    object sv = ie.GetValue("svcVersion") ?? ie.GetValue("Version");
                    string s = sv as string;
                    if (s == null) return;
                    int dot = s.IndexOf('.');
                    if (!int.TryParse(dot > 0 ? s.Substring(0, dot) : s, out major)) return;
                }
                int mode = major >= 11 ? 11001 : major == 10 ? 10001 : major == 9 ? 9999 : 0;
                if (mode == 0) return;

                string exe = Path.GetFileName(Application.ExecutablePath);
                using (Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (k == null) return;
                    object cur = k.GetValue(exe);
                    if (!(cur is int) || (int)cur != mode)
                        k.SetValue(exe, mode, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch { }
        }

        private void CreateOldBrowser()
        {
            EnsureTls12ForIE();
            EnsureBrowserEmulation();
            WebBrowser wb = new WebBrowser();
            wb.Dock = DockStyle.Fill;
            wb.ScriptErrorsSuppressed = true;
            wb.IsWebBrowserContextMenuEnabled = false;
            wb.AllowWebBrowserDrop = false;
            wb.WebBrowserShortcutsEnabled = false;
            Controls.Add(wb);
            wb.BringToFront();
            _view = wb;
            _usingWebView2 = false;
            wb.Navigate(_url);
        }

        private void RemoveView()
        {
            if (_view != null)
            {
                try
                {
                    Controls.Remove(_view);
                    _view.Dispose();
                }
                catch { }
                _view = null;
            }
            _usingWebView2 = false;
        }

        /// <summary>true nếu đang chạy bằng WebView2 (tiện để debug).</summary>
        public bool UsingWebView2
        {
            get { return _usingWebView2; }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) RemoveView();
            base.Dispose(disposing);
        }
    }
}
