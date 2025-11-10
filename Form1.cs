using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace frmwpsoffice
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        // Lưu ý: Các hàm FindFileRecursively và FindUninstallerRecursively cần được đặt trong cùng một class
        // (ví dụ: Form1 class) hoặc là static nếu bạn gọi từ class khác.

        // -------------------------------------------------------------
        // HÀM HỖ TRỢ (Các hàm này không cần thay đổi)
        // -------------------------------------------------------------

        /// <summary>
        /// Hàm tìm kiếm đệ quy (sâu) file trong thư mục con.
        /// </summary>
        private string FindFileRecursively(string rootPath, string fileName)
        {
            if (!Directory.Exists(rootPath)) return null;
            try
            {
                string[] foundFiles = Directory.GetFiles(
                    rootPath,
                    fileName,
                    SearchOption.AllDirectories // Tìm kiếm sâu
                );
                return foundFiles.FirstOrDefault();
            }
            catch { return null; }
        }
        /// <summary>
        /// Hàm tìm kiếm đệ quy (sâu) file uninstaller trong thư mục con (giống như FindFileRecursively).
        /// </summary>
        private string FindUninstallerRecursively(string rootPath, string fileName)
        {
            // Chúng ta có thể dùng lại hàm FindFileRecursively cho mục đích này.
            return FindFileRecursively(rootPath, fileName);
        }

        // -------------------------------------------------------------
        // LOGIC KIỂM TRA WPS OFFICE
        // -------------------------------------------------------------

        public void CheckWPSInstallLocation()
        {
            // Lấy đường dẫn AppData Local
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // --- 1. Kiểm tra trong AppData (vị trí cài đặt cá nhân) ---
            string wpsAppdataRoot = Path.Combine(appdata, "Kingsoft", "WPS Office");

            // SỬA LỖI QUAN TRỌNG: Dùng tìm kiếm đệ quy để tìm ksolaunch.exe ở bất kỳ thư mục con nào
            string ksolaunchAppdataPath = FindFileRecursively(wpsAppdataRoot, "ksolaunch.exe");

            if (ksolaunchAppdataPath != null)
            {
                txtcheck.Text = "WPS Office exists in appdata.";
                txtdirect.Text = wpsAppdataRoot;
                return; // Đã tìm thấy, kết thúc hàm
            }

            // --- 2. Kiểm tra Program Files (vị trí cài đặt chung) ---

            string programfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            // Vị trí Program Files (64-bit)
            string wpsProgramFilesRoot = Path.Combine(programfiles, "WPS Office");
            string ksolaunchProgramFiles = Path.Combine(wpsProgramFilesRoot, "ksolaunch.exe");

            if (File.Exists(ksolaunchProgramFiles))
            {
                txtcheck.Text = "WPS Office exists in Program Files.";
                txtdirect.Text = wpsProgramFilesRoot;
                return;
            }

            // Vị trí Program Files (x86 - 32-bit)
            string wpsProgramFilesX86Root = Path.Combine(programfilesx86, "WPS Office");
            string ksolaunchProgramFilesX86 = Path.Combine(wpsProgramFilesX86Root, "ksolaunch.exe");

            if (File.Exists(ksolaunchProgramFilesX86))
            {
                txtcheck.Text = "WPS Office exists in Program Files (x86).";
                txtdirect.Text = wpsProgramFilesX86Root;
                return;
            }
            // *Lưu ý: Biến 'appdata' PHẢI có sẵn từ đầu hàm.*

            txtcheck.Text = "WPS Office not found. Move to Clean Up Kingsoft residual data Mode.";

            // Lấy đường dẫn Kingsoft ROOT (thư mục mẹ)
            string kingsoftRootPath = Path.Combine(appdata, "Kingsoft");

            // Trỏ txtdirect.Text về thư mục Kingsoft
            txtdirect.Text = kingsoftRootPath;

        }
        private void KillWPSProcess()
        {
            // Cố gắng sử dụng tên chính xác, không kèm .exe
            const string targetName = "wps";
            int killedCount = 0;

            try
            {
                // Lấy tất cả các tiến trình và lọc theo tên
                var wpsProcesses = Process.GetProcesses()
                                          .Where(p => p.ProcessName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
                                          .ToList();

                if (wpsProcesses.Count > 0)
                {
                    foreach (Process p in wpsProcesses)
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            killedCount++;
                        }
                    }
                    txtcheck.Text = $"The {killedCount} '{targetName}.exe' ENDED .";
                }
                else
                {
                    txtcheck.Text = $"Can't find any '{targetName}.exe' processes running.";
                }
            }
            catch (Exception ex)
            {
                // Đây là nơi thường xảy ra lỗi "Access is denied"
                txtcheck.Text = $"ERROR: {ex.Message}. NEED Administrator permission.";
            }
        }
        private bool CleanWPSRoamingData()
        {
            string appdataRoaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string kingsoftRoamingPath = Path.Combine(appdataRoaming, "kingsoft");

            if (Directory.Exists(kingsoftRoamingPath))
            {
                try
                { Directory.Delete(kingsoftRoamingPath, true); return true; }
                catch (UnauthorizedAccessException authEx)
                { txtcheck.Text = $"Access permission Roaming error: Need run Admin permission. ({authEx.Message})"; return false; }
                catch (Exception ex)
                { txtcheck.Text = $"Error when deleting Roaming data: {ex.Message}"; return false; }
            }
            return true;
        }
        // FORM EVENTS
        // -------------------------------------------------------------
        private void Form1_Load(object sender, EventArgs e)
        {
            // Thông tin hệ điều hành (giữ nguyên)
            string r = "";
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"))
            {
                ManagementObjectCollection information = searcher.Get();
                if (information != null)
                {
                    foreach (ManagementObject obj in information)
                    {
                        r = obj["Caption"].ToString() + " - " + obj["OSArchitecture"].ToString();
                    }
                }
                r = r.Replace("NT 5.1.2600", "XP");
                r = r.Replace("NT 5.2.3790", "Server 2003");
                lblsys.Text = r;
            }

            // GỌI HÀM KIỂM TRA CHÍNH XÁC
            CheckWPSInstallLocation();

            // Loạt code tìm file AppData cũ BỊ XÓA HOẶC BỊ COMMENT:
            // Vì CheckWPSInstallLocation() đã làm công việc này, các dòng tìm file AppData cũ 
            // ngay sau khi gọi CheckWPSInstallLocation() là dư thừa và có thể gây lỗi.
            // Dòng code cũ:
            /*
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filepath0 = appdata + @"\Kingsoft\WPS Office\";
            string filepath = appdata + @"\Kingsoft\WPS Office\ksolaunch.exe";
            if (System.IO.File.Exists(filepath)) { ... } else { ... }
            */
            // Đã được thay thế bằng logic trong CheckWPSInstallLocation().
        }
        private void btnuninstall_Click(object sender, EventArgs e)
        {
            // Lấy đường dẫn gốc của WPS Office đã tìm thấy từ hàm CheckWPSInstallLocation()
            string wpsRootPath = txtdirect.Text;
            string uninstallerPath = null;
            KillWPSProcess();
            if (string.IsNullOrEmpty(wpsRootPath) || wpsRootPath.Equals("WPS Office not found.", StringComparison.OrdinalIgnoreCase))
            {
                txtcheck.Text = "ERROR: CAN NOT FIND WPS Office FOLDER.";
                return;
            }

            // 1. Tìm file uninstall.exe bằng tìm kiếm đệ quy (recursive search)
            // Hàm này tìm kiếm file 'uninstall.exe' trong toàn bộ thư mục con của wpsRootPath
            uninstallerPath = FindUninstallerRecursively(wpsRootPath, "uninst.exe");

            if (uninstallerPath != null)
            {
                txtcheck.Text = $"Found Uninstaller File: {uninstallerPath}";

                // 2. KẾT THÚC TIẾN TRÌNH 'wps.exe' TRƯỚC KHI CHẠY UNINSTALLER
                // Điều này khắc phục lỗi "wps.exe is running. Please close..."
                KillWPSProcess();

                // Thêm một chút delay ngắn để hệ điều hành kịp thời đóng tiến trình
                System.Threading.Thread.Sleep(500); // 500 mili-giây

                try
                {
                    // 3. Thực thi file uninstall.exe
                    // Sử dụng Process.Start(đường dẫn) để chạy chương trình
                    Process.Start(uninstallerPath);
                    txtcheck.Text = $"Run WPS Office Uninstaller file.";
                }
                catch (Exception ex)
                {
                    // Lỗi thường gặp: Lỗi quyền truy cập khi cố gắng chạy file
                    txtcheck.Text = $"Error when run Uninstaller: {ex.Message}";
                }
            }
            else
            {
                txtcheck.Text = "Error: Cannot find uninstall.exe in the installation folder.";
            }
        }

        private void btnCleanup_Click(object sender, EventArgs e)
        {
            // Gọi hàm dọn dẹp Local (AppData\Local\Kingsoft)
            CleanWPSResidualFiles();

            // Kiểm tra xem Local Clean Up có thành công không trước khi tiếp tục
            if (txtcheck.Text.Contains("Lỗi"))
            {
                return;
            }

            // GỌI HÀM MỚI: Dọn dẹp Roaming Data (AppData\Roaming\kingsoft)
            bool roamingSuccess = CleanWPSRoamingData();

            // Cập nhật thông báo cuối cùng
            if (roamingSuccess && !txtcheck.Text.Contains("Lỗi"))
            {
                txtcheck.Text = "Cleanup completed! Both Local and Roaming data have been processed.";
            }
            else if (!roamingSuccess)
            {
                // Thông báo lỗi Roaming đã được ghi trong hàm CleanWPSRoamingData()
                txtcheck.Text += "\nWarning: Failed to clean up Roaming data.";
            }
        }

        private void CleanWPSResidualFiles()
        {
            // Lấy đường dẫn AppData Local
            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // ĐƯỜNG DẪN MỤC TIÊU: ...AppData\Local\Kingsoft
            string kingsoftLocalPath = Path.Combine(appdata, "Kingsoft");

            if (!Directory.Exists(kingsoftLocalPath))
            {
                txtcheck.Text = "Warning: The Kingsoft Local folder is clean. Proceeding to clean up Roaming.";
                return;
            }

            // 1. KẾT THÚC TIẾN TRÌNH WPS
            KillWPSProcess();
            System.Threading.Thread.Sleep(500); // Chờ 0.5 giây

            // 2. THỰC HIỆN XÓA THƯ MỤC KINGSOFT VỚI VÒNG LẶP THỬ LẠI
            const int MaxRetries = 3;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    // XÓA TOÀN BỘ THƯ MỤC KINGSOFT VÀ TẤT CẢ NỘI DUNG (Recursive)
                    Directory.Delete(kingsoftLocalPath, true);
                    txtcheck.Text = $"The Local folder has been completely deleted: **{kingsoftLocalPath}**.";
                    return; // Xóa thành công
                }
                catch (IOException ioEx)
                {
                    // Xảy ra khi file đang bị khóa.
                    if (attempt < MaxRetries)
                    {
                        txtcheck.Text = $"Try ({attempt}/{MaxRetries}) again: File Local is locking...";
                        System.Threading.Thread.Sleep(1000);
                    }
                    else
                    {
                        // Thử hết số lần
                        txtcheck.Text = $"PERMANENT I/O Error: Unable to delete Kingsoft Local. File is locked.. ({ioEx.Message})";
                        return;
                    }
                }
                catch (UnauthorizedAccessException authEx)
                {
                    // Lỗi quyền truy cập (Dù đã chạy Admin, vẫn xảy ra nếu có quyền đặc biệt)
                    txtcheck.Text = $"ACCESS ERROR: Unable to delete Kingsoft Local. Check ownership permissions. ({authEx.Message})";
                    return;
                }
                catch (Exception ex)
                {
                    txtcheck.Text = $"Unknown error while deleting Local: {ex.Message}";
                    return;
                }
            }
        }
        private void btncleanup_Click_1(object sender, EventArgs e)
        {
            // 1. Dọn dẹp dữ liệu Local (xóa thư mục WPS Office và tất cả nội dung)
            CleanWPSResidualFiles();

            // Lưu lại trạng thái dọn dẹp Local (để không ghi đè nếu có lỗi)
            string localStatus = txtcheck.Text;

            // 2. Dọn dẹp dữ liệu Roaming (...AppData\Roaming\kingsoft)
            bool roamingSuccess = CleanWPSRoamingData();

            // 3. Cập nhật thông báo cuối cùng
            if (roamingSuccess)
            {
                if (localStatus.Contains("Error"))
                {
                    txtcheck.Text = $"{localStatus}\nRoaming cleanup completed successfully.";
                }
                else
                {
                    txtcheck.Text = "Cleanup completed! Local WPS Office and Roaming data have been deleted.";
                }
                RunEmbeddedBatchFile("block_wps.bat");
            }
            else
            {
                // Nếu Roaming thất bại, thông báo lỗi Roaming sẽ được ghi từ CleanWPSRoamingData()
                txtcheck.Text += "\nWarning: Roaming data cleanup failed";
            }
        }

        public static void RunEmbeddedBatchFile(string resourceName, string arguments = "")
        {
            // Tên tài nguyên (thường là [Tên Project].[Tên file .bat])
            // Ví dụ: YourProjectNamespace.block_wps.bat
            Assembly assembly = Assembly.GetExecutingAssembly();
            string fullResourceName = assembly.GetName().Name + "." + resourceName;

            // Tạo đường dẫn file tạm thời (trong thư mục Temp của hệ thống)
            string tempFilePath = Path.Combine(Path.GetTempPath(), resourceName);

            try
            {
                // 1. Đọc file .bat từ tài nguyên nhúng
                using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
                using (FileStream fileStream = File.Create(tempFilePath))
                {
                    if (stream == null)
                    {
                        MessageBox.Show($"Không tìm thấy tài nguyên nhúng: {fullResourceName}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    // 2. Trích xuất file .bat ra ổ đĩa
                    stream.CopyTo(fileStream);
                }

                // 3. Chạy file .bat với quyền Admin
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tempFilePath,
                    Arguments = arguments,
                    UseShellExecute = true,
                    Verb = "runas" // Yêu cầu chạy với quyền Quản trị viên
                };

                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi chạy file batch: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // (Tùy chọn) Xóa file tạm thời sau khi chạy
                if (File.Exists(tempFilePath)) 
                    File.Delete(tempFilePath); 
            }
        }
    }
}
