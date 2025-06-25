using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FileComparerAppWin
{
    public partial class LoadingForm : Form
    {
        private Label lblLoading;
        private Label lblProgress;
        private ProgressBar progressBar;
        private Button btnCancel;

        public bool WasCancelled { get; private set; }
        public event EventHandler CancelRequested;

        public LoadingForm()
        {
            InitializeComponent();

            // Form style
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Opacity = 0.9;
            this.Width = 300;
            this.Height = 180;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.DoubleBuffered = true;

            // Rounded corners
            this.Load += (s, e) =>
            {
                var path = new GraphicsPath();
                int radius = 20;
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(this.Width - radius, 0, radius, radius, 270, 90);
                path.AddArc(this.Width - radius, this.Height - radius, radius, radius, 0, 90);
                path.AddArc(0, this.Height - radius, radius, radius, 90, 90);
                path.CloseAllFigures();
                this.Region = new Region(path);
            };

            // Progress bar
            progressBar = new ProgressBar()
            {
                Style = ProgressBarStyle.Continuous,
                Minimum = 0,
                Maximum = 100,
                Value = 0,
                Size = new Size(240, 20),
                Location = new Point(30, 60)
            };

            // Loading label
            lblLoading = new Label()
            {
                Text = "🔄 Comparing folders...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Location = new Point(0, 20),
                Width = this.Width
            };

            // Progress label
            lblProgress = new Label()
            {
                Text = "0%",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(0, 90),
                Width = this.Width
            };

            // Cancel button
            btnCancel = new Button()
            {
                Text = "Cancel",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(70, 70, 70),
                Size = new Size(100, 30),
                Location = new Point(100, 120)
            };
            btnCancel.Click += (s, e) =>
            {
                WasCancelled = true;
                CancelRequested?.Invoke(this, EventArgs.Empty);
                this.Close();
            };

            this.Controls.Add(progressBar);
            this.Controls.Add(lblLoading);
            this.Controls.Add(lblProgress);
            this.Controls.Add(btnCancel);
        }

        public void UpdateProgress(int percent, string status = null)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action<int, string>(UpdateProgress), percent, status);
                return;
            }

            progressBar.Value = Math.Min(Math.Max(percent, 0), 100);
            lblProgress.Text = $"{percent}%";

            if (!string.IsNullOrEmpty(status))
            {
                lblLoading.Text = status;
            }
        }
    }
}
