using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace frmwpsoffice
{
    internal sealed partial class frmDonate : Form
    {
        public frmDonate()
        {
            InitializeComponent();
        }

        private void BtnYes_Click(object sender, EventArgs e)
        {
            if (_imagePanel.Visible) return;

            _imagePanel.Location = new Point(0, CONTENT_HEIGHT);
            _footerPanel.Location = new Point(0, CONTENT_HEIGHT + IMAGE_PANEL_HEIGHT);
            this.ClientSize = new Size(FORM_WIDTH, CONTENT_HEIGHT + IMAGE_PANEL_HEIGHT + FOOTER_HEIGHT);
            _imagePanel.Visible = true;

            foreach (Button btn in _footerPanel.Controls.OfType<Button>())
                btn.Enabled = false;
        }

        private void BtnNo_Click(object sender, EventArgs e)
        {
            DonateSettings.DontShowAgain = true;
            this.Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!DonateSettings.DontShowAgain)
                DonateSettings.DontShowAgain = _chkDontShow.Checked;
            base.OnFormClosing(e);
        }

        private static void Heart_Paint(object sender, PaintEventArgs e)
        {
            PictureBox pb = (PictureBox)sender;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            using (Font font = new Font("Segoe UI", 24F))
            using (SolidBrush brush = new SolidBrush(Color.Crimson))
            using (StringFormat sf = new StringFormat
                   {
                       Alignment = StringAlignment.Center,
                       LineAlignment = StringAlignment.Center
                   })
                e.Graphics.DrawString("♥", font, brush, new RectangleF(0, 0, pb.Width, pb.Height), sf);
        }

        private static Image TryLoadImage(string fileName)
        {
            string resourceName = "frmwpsoffice." + fileName;
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    if (stream != null) return Image.FromStream(new MemoryStream(ReadAll(stream)));
            }
            catch { }

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            string path = Path.Combine(dir, fileName);
            try { return File.Exists(path) ? Image.FromFile(path) : null; }
            catch { return null; }
        }

        private static byte[] ReadAll(Stream stream)
        {
            byte[] buf = new byte[stream.Length];
            stream.Read(buf, 0, buf.Length);
            return buf;
        }
    }
}
