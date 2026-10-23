using System;
using System.Drawing;
using System.Windows.Forms;

namespace FreeHub.UI
{
    /// <summary>
    /// Dialog for entering Google Cloud OAuth 2.0 Desktop credentials (Client ID and Client Secret).
    /// </summary>
    public class CloudSyncSetupForm : Form
    {
        private TextBox _txtClientId;
        private TextBox _txtClientSecret;
        private Button _btnSave;
        private Button _btnCancel;

        public string ClientId { get; private set; }

        public string ClientSecret { get; private set; }

        public CloudSyncSetupForm(string existingClientId)
        {
            InitializeLayout(existingClientId);
        }

        private void InitializeLayout(string existingClientId)
        {
            this.Text = "Google Drive OAuth 2.0 Setup";
            this.Size = new Size(500, 265);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(24, 24, 32);
            this.ForeColor = Color.FromArgb(220, 220, 235);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            Label lblTitle = new Label
            {
                Text = "Enter your Google Cloud OAuth 2.0 Desktop App Credentials:",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 230, 118),
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblClientId = new Label
            {
                Text = "Client ID:",
                Location = new Point(20, 52),
                AutoSize = true
            };

            _txtClientId = new TextBox
            {
                Location = new Point(20, 74),
                Size = new Size(440, 25),
                BackColor = Color.FromArgb(36, 36, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = existingClientId ?? string.Empty
            };

            Label lblClientSecret = new Label
            {
                Text = "Client Secret:",
                Location = new Point(20, 110),
                AutoSize = true
            };

            _txtClientSecret = new TextBox
            {
                Location = new Point(20, 132),
                Size = new Size(440, 25),
                BackColor = Color.FromArgb(36, 36, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            _btnSave = new Button
            {
                Text = "Save && Connect Google",
                Location = new Point(210, 178),
                Size = new Size(155, 32),
                BackColor = Color.FromArgb(0, 180, 90),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += OnSaveClicked;

            _btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(375, 178),
                Size = new Size(85, 32),
                BackColor = Color.FromArgb(55, 55, 70),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                DialogResult = DialogResult.Cancel
            };
            _btnCancel.FlatAppearance.BorderSize = 0;

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblClientId);
            this.Controls.Add(_txtClientId);
            this.Controls.Add(lblClientSecret);
            this.Controls.Add(_txtClientSecret);
            this.Controls.Add(_btnSave);
            this.Controls.Add(_btnCancel);

            this.AcceptButton = _btnSave;
            this.CancelButton = _btnCancel;
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtClientId.Text) || string.IsNullOrWhiteSpace(_txtClientSecret.Text))
            {
                MessageBox.Show(
                    "Please enter both your Google OAuth Client ID and Client Secret.",
                    "Validation",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            ClientId = _txtClientId.Text.Trim();
            ClientSecret = _txtClientSecret.Text.Trim();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
