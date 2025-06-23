using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FileComparerAppWin
{
    public partial class LoadingForm : Form
    {
        private Label lblLoading;
        private ProgressBar progressSpinner;

        public LoadingForm()
        {
            InitializeComponent();

            // Form style
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.Opacity = 0.9;
            this.Width = 240;
            this.Height = 130;
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

            // Spinner (indeterminate style)
            progressSpinner = new ProgressBar()
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Size = new Size(180, 20),
                Location = new Point(30, 30)
            };

            // Label
            lblLoading = new Label()
            {
                Text = "🔄 Comparing folders...",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Dock = DockStyle.Bottom,
                Height = 50,
                Font = new Font("Segoe UI", 10, FontStyle.Regular)
            };

            this.Controls.Add(progressSpinner);
            this.Controls.Add(lblLoading);
        }
    }
}
