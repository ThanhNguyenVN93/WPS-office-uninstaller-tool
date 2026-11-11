using System;
using System.Drawing;
using System.Windows.Forms;

namespace frmwpsoffice
{
    internal enum UpdateChoice { Later, UpdateNow, OpenPage }

    /// <summary>
    /// "Update available" window (version + release notes + buttons) and the "What's new" window shown
    /// once after an update.
    /// </summary>
    internal sealed class UpdateDialog : Form
    {
        private UpdateChoice _choice = UpdateChoice.Later;

        private UpdateDialog(string title, string heading, string sub, string notes, bool askUpdate)
        {
            Text = title;
            Font = new Font("Segoe UI", 9F);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(520, 400);
            BackColor = SystemColors.Window;

            var headingLabel = new Label
            {
                Text = heading,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(16, 14),
                Size = new Size(488, 26)
            };
            var subLabel = new Label
            {
                Text = sub,
                ForeColor = SystemColors.GrayText,
                AutoSize = false,
                Location = new Point(16, 42),
                Size = new Size(488, 20)
            };
            var notesTitle = new Label
            {
                Text = "What's new",
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(16, 72),
                Size = new Size(488, 20)
            };
            var notesBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = SystemColors.Window,
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(16, 94),
                Size = new Size(488, 238),
                Text = string.IsNullOrWhiteSpace(notes) ? "No release notes were published for this version." : notes,
                TabStop = false
            };
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = SystemColors.Control
            };

            Controls.AddRange(new Control[] { headingLabel, subLabel, notesTitle, notesBox, footer });

            if (askUpdate)
            {
                Button update = MakeButton("Update now", 110, true);
                Button page = MakeButton("Open page", 100, false);
                Button later = MakeButton("Later", 80, false);
                update.Location = new Point(520 - 16 - 80 - 8 - 100 - 8 - 110, 14);
                page.Location = new Point(520 - 16 - 80 - 8 - 100, 14);
                later.Location = new Point(520 - 16 - 80, 14);
                update.Click += (s, e) => { _choice = UpdateChoice.UpdateNow; Close(); };
                page.Click += (s, e) => { _choice = UpdateChoice.OpenPage; Close(); };
                later.Click += (s, e) => { _choice = UpdateChoice.Later; Close(); };
                footer.Controls.AddRange(new Control[] { update, page, later });
                AcceptButton = update;
                CancelButton = later;
            }
            else
            {
                Button ok = MakeButton("OK", 90, true);
                ok.Location = new Point(520 - 16 - 90, 14);
                ok.Click += (s, e) => Close();
                footer.Controls.Add(ok);
                AcceptButton = ok;
                CancelButton = ok;
            }

            Shown += (s, e) => notesBox.SelectionLength = 0;
        }

        private static Button MakeButton(string text, int width, bool primary)
        {
            return new Button
            {
                Text = text,
                Size = new Size(width, 28),
                UseVisualStyleBackColor = true,
                Font = primary ? new Font("Segoe UI", 9F, FontStyle.Bold) : new Font("Segoe UI", 9F)
            };
        }

        /// <summary>New version window; returns what the user picked.</summary>
        public static UpdateChoice ShowUpdate(IWin32Window owner, UpdateInfo info)
        {
            string current = "v" + UpdateChecker.CurrentVersion.ToString(3);
            using (var dlg = new UpdateDialog(
                "Update available",
                "Version " + info.Tag + " is available",
                "You are using " + current + ".",
                info.Notes,
                true))
            {
                dlg.ShowDialog(owner);
                return dlg._choice;
            }
        }

        /// <summary>One-time window after the app was updated.</summary>
        public static void ShowWhatsNew(IWin32Window owner, UpdateInfo info)
        {
            using (var dlg = new UpdateDialog(
                "What's new",
                "WPS Office Uninstaller Tool " + info.Tag,
                "The app was updated. Here is what changed.",
                info.Notes,
                false))
            {
                dlg.ShowDialog(owner);
            }
        }
    }
}
