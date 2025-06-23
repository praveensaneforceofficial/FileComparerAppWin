using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FileComparerAppWin
{
    public partial class DiffViewerForm : Form
    {
        // 👇 Replaced RichTextBox with SyncRichTextBox
        private SyncRichTextBox txtPrimary;
        private SyncRichTextBox txtSecondary;
        private Panel diffMapPanel;
        private RichTextBox txtMerged; // 👈 Added for merged output
        private List<int> diffLines = new List<int>();

        public DiffViewerForm(string primaryCode, string secondaryCode)
        {
            this.Text = "🔍 Code Difference Viewer";
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(800, 600);

            // 👇 Create outer layout to include buttons, code view, and merged panel
            var outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));     // Buttons row
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));     // Code viewer
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));     // Merged output

            // 👇 Add buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };
            var btnPrimary = new Button { Text = "Primary", Width = 100 };
            var btnSecondary = new Button { Text = "Secondary", Width = 100 };
            var btnBoth = new Button { Text = "Both", Width = 100 };
            buttonPanel.Controls.Add(btnPrimary);
            buttonPanel.Controls.Add(btnSecondary);
            buttonPanel.Controls.Add(btnBoth);

            // 👇 Create main diff layout (primary + secondary + diff map)
            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 3;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49.5f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 49.5f));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1f));

            txtPrimary = new SyncRichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 10)
            };

            txtSecondary = new SyncRichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 10)
            };

            // 👇 Scroll sync setup
            txtPrimary.Scroll += (s, e) => txtSecondary.SyncScrollWith(txtPrimary);
            txtSecondary.Scroll += (s, e) => txtPrimary.SyncScrollWith(txtSecondary);

            diffMapPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Width = 16
            };
            diffMapPanel.Paint += DiffMapPanel_Paint;
            diffMapPanel.MouseClick += DiffMapPanel_MouseClick;

            layout.Controls.Add(txtPrimary, 0, 0);
            layout.Controls.Add(txtSecondary, 1, 0);
            layout.Controls.Add(diffMapPanel, 2, 0);

            // 👇 Merged output richtextbox
            txtMerged = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightYellow
            };

            // 👇 Add all components to outer layout
            outerLayout.Controls.Add(buttonPanel, 0, 0); // Top row
            outerLayout.Controls.Add(layout, 0, 1);      // Middle row (code)
            outerLayout.Controls.Add(txtMerged, 0, 2);    // Bottom row (merged)

            this.Controls.Add(outerLayout);

            // 👇 Show diffs
            ShowDifferences(primaryCode, secondaryCode);

            // 👇 Button click events for merging
            btnPrimary.Click += (s, e) =>
            {
                txtMerged.Clear();
                txtMerged.AppendText(txtPrimary.Text);
            };

            btnSecondary.Click += (s, e) =>
            {
                txtMerged.Clear();
                txtMerged.AppendText(txtSecondary.Text);
            };

            btnBoth.Click += (s, e) =>
            {
                txtMerged.Clear();
                for (int i = 0; i < txtPrimary.Lines.Length; i++)
                {
                    string pLine = txtPrimary.Lines[i];
                    string sLine = i < txtSecondary.Lines.Length ? txtSecondary.Lines[i] : "<missing>";
                    txtMerged.AppendText($"P: {pLine}\nS: {sLine}\n\n");
                }
            };
        }

        private void ShowDifferences(string primaryCode, string secondaryCode)
        {
            var primaryLines = primaryCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var secondaryLines = secondaryCode.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            var diffs = GetLineDiffs(primaryLines, secondaryLines);

            int lineNum = 0;
            foreach (var diff in diffs)
            {
                string linePrefix = (lineNum + 1).ToString().PadLeft(4) + ": ";

                if (diff.isMatch)
                {
                    AppendColoredLine(txtPrimary, linePrefix + diff.primary, Color.LightGreen);
                    AppendColoredLine(txtSecondary, linePrefix + diff.secondary, Color.LightGreen);
                }
                else
                {
                    diffLines.Add(lineNum);
                    AppendColoredLine(txtPrimary, linePrefix + (string.IsNullOrEmpty(diff.primary) ? "<missing>" : diff.primary), string.IsNullOrEmpty(diff.primary) ? Color.MediumVioletRed : Color.LightPink);
                    AppendColoredLine(txtSecondary, linePrefix + (string.IsNullOrEmpty(diff.secondary) ? "<missing>" : diff.secondary), string.IsNullOrEmpty(diff.secondary) ? Color.MediumVioletRed : Color.LightSkyBlue);
                }
                lineNum++;
            }
            diffMapPanel.Invalidate();
        }

        private void AppendColoredLine(RichTextBox box, string text, Color color)
        {
            int start = box.TextLength;
            box.AppendText(text + Environment.NewLine);
            box.Select(start, text.Length);
            box.SelectionBackColor = color;
            box.SelectionLength = 0;
        }

        private string Normalize(string line)
        {
            return line?.Trim().Replace(" ", "").Replace("\t", "").ToLowerInvariant();
        }

        private List<(string primary, string secondary, bool isMatch)> GetLineDiffs(string[] primaryLines, string[] secondaryLines)
        {
            int m = primaryLines.Length;
            int n = secondaryLines.Length;
            int[,] lcs = new int[m + 1, n + 1];

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (Normalize(primaryLines[i]) == Normalize(secondaryLines[j]))
                        lcs[i + 1, j + 1] = lcs[i, j] + 1;
                    else
                        lcs[i + 1, j + 1] = Math.Max(lcs[i + 1, j], lcs[i, j + 1]);
                }
            }

            List<(string, string, bool)> result = new List<(string, string, bool)>();
            int x = m, y = n;
            while (x > 0 && y > 0)
            {
                if (Normalize(primaryLines[x - 1]) == Normalize(secondaryLines[y - 1]))
                {
                    result.Add((primaryLines[x - 1], secondaryLines[y - 1], true));
                    x--; y--;
                }
                else if (lcs[x - 1, y] >= lcs[x, y - 1])
                {
                    result.Add((primaryLines[x - 1], "", false));
                    x--;
                }
                else
                {
                    result.Add(("", secondaryLines[y - 1], false));
                    y--;
                }
            }
            while (x > 0) result.Add((primaryLines[x-- - 1], "", false));
            while (y > 0) result.Add(("", secondaryLines[y-- - 1], false));

            result.Reverse();
            return result;
        }

        private void DiffMapPanel_Paint(object sender, PaintEventArgs e)
        {
            int totalLines = txtPrimary.Lines.Length;
            if (totalLines == 0) return;

            foreach (int lineIndex in diffLines)
            {
                float y = (float)lineIndex / totalLines * diffMapPanel.Height;
                e.Graphics.FillRectangle(Brushes.Red, 0, y, diffMapPanel.Width, 3);
            }
        }

        private void DiffMapPanel_MouseClick(object sender, MouseEventArgs e)
        {
            int totalLines = txtPrimary.Lines.Length;
            if (totalLines == 0) return;

            int clickedLine = (int)((float)e.Y / diffMapPanel.Height * totalLines);
            if (clickedLine >= totalLines) clickedLine = totalLines - 1;

            txtPrimary.SelectionStart = txtPrimary.GetFirstCharIndexFromLine(clickedLine);
            txtPrimary.ScrollToCaret();

            txtSecondary.SelectionStart = txtSecondary.GetFirstCharIndexFromLine(clickedLine);
            txtSecondary.ScrollToCaret();
        }
    }

    // ✅ Custom RichTextBox for scroll event
    public class SyncRichTextBox : RichTextBox
    {
        public event EventHandler Scroll;

        private const int WM_VSCROLL = 0x0115;
        private const int WM_MOUSEWHEEL = 0x020A;

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_VSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                Scroll?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SyncScrollWith(SyncRichTextBox other)
        {
            int index = other.GetCharIndexFromPosition(new Point(1, 1));
            int line = other.GetLineFromCharIndex(index);
            int firstChar = GetFirstCharIndexFromLine(line);
            this.SelectionStart = firstChar;
            this.ScrollToCaret();
        }
    }
}
