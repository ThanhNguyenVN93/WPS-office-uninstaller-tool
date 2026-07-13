using System.Drawing;
using System.Windows.Forms;

namespace frmwpsoffice
{
    partial class frmDonate
    {
        private System.ComponentModel.IContainer components = null;

        private const int FORM_WIDTH = 420;
        private const int CONTENT_HEIGHT = 210;
        private const int FOOTER_HEIGHT = 68;
        private const int IMAGE_PANEL_HEIGHT = 250;

        private Panel _footerPanel;
        private Panel _imagePanel;
        private CheckBox _chkDontShow;
        private Button _btnYes;
        private Button _btnNo;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Support WPS Office Uninstaller Tool";
            this.ClientSize = new Size(FORM_WIDTH, CONTENT_HEIGHT + FOOTER_HEIGHT);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Microsoft Sans Serif", 9F);

            BuildContentArea();
            BuildImagePanel();
            BuildFooter();

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void BuildContentArea()
        {
            PictureBox heart = new PictureBox
            {
                Location = new Point(20, 18),
                Size = new Size(40, 40),
                BackColor = Color.Transparent
            };
            heart.Paint += Heart_Paint;
            this.Controls.Add(heart);

            this.Controls.Add(new Label
            {
                Text = "Thanks for using WPS Office Uninstaller Tool!",
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(70, 24)
            });

            this.Controls.Add(new Label
            {
                Text = "If you found this tool helpful, please consider making a small\n" +
                       "donation to support development.",
                Font = new Font("Microsoft Sans Serif", 9F),
                ForeColor = Color.DimGray,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 70)
            });

            this.Controls.Add(new Label
            {
                Text = "Your support helps keep this project going!",
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                ForeColor = Color.Black,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 128)
            });

            this.Controls.Add(new Label
            {
                Text = "Click 'Yes' to show your support!",
                Font = new Font("Microsoft Sans Serif", 8.5F),
                ForeColor = Color.DimGray,
                BackColor = Color.Transparent,
                AutoSize = true,
                Location = new Point(20, 150)
            });

            _chkDontShow = new CheckBox
            {
                Text = "Don't show this again",
                Font = new Font("Microsoft Sans Serif", 8.5F),
                ForeColor = Color.DimGray,
                BackColor = Color.Transparent,
                Checked = DonateSettings.DontShowAgain,
                AutoSize = true,
                Location = new Point(18, 178)
            };
            this.Controls.Add(_chkDontShow);

            this.Controls.Add(new Panel
            {
                Location = new Point(0, CONTENT_HEIGHT - 1),
                Size = new Size(FORM_WIDTH, 1),
                BackColor = Color.FromArgb(220, 220, 220)
            });
        }

        private void BuildImagePanel()
        {
            _imagePanel = new Panel
            {
                Location = new Point(0, CONTENT_HEIGHT),
                Size = new Size(FORM_WIDTH, IMAGE_PANEL_HEIGHT),
                BackColor = Color.FromArgb(245, 245, 245),
                Visible = false
            };

            _imagePanel.Controls.Add(new PictureBox
            {
                Location = new Point(20, 14),
                Size = new Size(178, 178),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Image = TryLoadImage("tcb.jpg")
            });
            _imagePanel.Controls.Add(new Label
            {
                Text = "Techcombank",
                Font = new Font("Microsoft Sans Serif", 8.5F),
                ForeColor = Color.DimGray,
                BackColor = Color.Transparent,
                Location = new Point(20, 196),
                Size = new Size(178, 18),
                TextAlign = ContentAlignment.MiddleCenter
            });

            _imagePanel.Controls.Add(new PictureBox
            {
                Location = new Point(222, 14),
                Size = new Size(178, 178),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Image = TryLoadImage("momo.jpg")
            });
            _imagePanel.Controls.Add(new Label
            {
                Text = "MoMo",
                Font = new Font("Microsoft Sans Serif", 8.5F),
                ForeColor = Color.DimGray,
                BackColor = Color.Transparent,
                Location = new Point(222, 196),
                Size = new Size(178, 18),
                TextAlign = ContentAlignment.MiddleCenter
            });

            _imagePanel.Controls.Add(new Panel
            {
                Location = new Point(0, IMAGE_PANEL_HEIGHT - 1),
                Size = new Size(FORM_WIDTH, 1),
                BackColor = Color.FromArgb(220, 220, 220)
            });

            this.Controls.Add(_imagePanel);
        }

        private void BuildFooter()
        {
            _footerPanel = new Panel
            {
                Location = new Point(0, CONTENT_HEIGHT),
                Size = new Size(FORM_WIDTH, FOOTER_HEIGHT),
                BackColor = Color.White
            };

            _btnYes = new Button
            {
                Text = "Yes",
                Size = new Size(180, 34),
                Location = new Point(20, 18),
                Font = new Font("Microsoft Sans Serif", 9F, FontStyle.Bold),
                UseVisualStyleBackColor = true
            };
            _btnYes.Click += BtnYes_Click;
            _footerPanel.Controls.Add(_btnYes);

            _btnNo = new Button
            {
                Text = "No",
                Size = new Size(180, 34),
                Location = new Point(220, 18),
                Font = new Font("Microsoft Sans Serif", 9F),
                UseVisualStyleBackColor = true
            };
            _btnNo.Click += BtnNo_Click;
            _footerPanel.Controls.Add(_btnNo);

            this.Controls.Add(_footerPanel);
        }
    }
}
