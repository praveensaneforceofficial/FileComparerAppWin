using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FileComparerAppWin
{
    public partial class DiffViewerControl : UserControl
    {
        private SyncRichTextBox txtPrimary;
        private SyncRichTextBox txtSecondary;
        private Panel diffMapPanel;
        private RichTextBox txtMerged;
        private List<int> diffLines = new List<int>();

        public DiffViewerControl(string primaryCode, string secondaryCode)
        {
            InitializeComponent(); // You can leave this empty or remove if not needed

            // 👇 Outer layout to hold all content
            var outerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1
            };
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
            outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

            // 👇 Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10)
            };
            var btnPrimary = new Button { Text = "Primary", Width = 100 };
            var btnSecondary = new Button { Text = "Secondary", Width = 100 };
            var btnBoth = new Button { Text = "Both", Width = 100 };
            buttonPanel.Controls.AddRange(new Control[] { btnPrimary, btnSecondary, btnBoth });

            // 👇 Diff viewer area
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3
            };
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

            txtMerged = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightYellow
            };

            outerLayout.Controls.Add(buttonPanel, 0, 0);
            outerLayout.Controls.Add(layout, 0, 1);
            outerLayout.Controls.Add(txtMerged, 0, 2);

            this.Controls.Add(outerLayout);

            // 👉 Show the diff
            ShowDifferences(primaryCode, secondaryCode);

            // 👉 Merge buttons
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
}
