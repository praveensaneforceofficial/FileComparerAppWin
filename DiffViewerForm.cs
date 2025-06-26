using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace FileComparerAppWin
{
    public partial class DiffViewerForm : Form
    {
        private SyncRichTextBox txtPrimary;
        private SyncRichTextBox txtSecondary;
        private RichTextBox lineNumbersPrimary; // 👈 Line numbers for Primary
        private RichTextBox lineNumbersSecondary; // 👈 Line numbers for Secondary
        private Panel diffMapPanel;
        private RichTextBox lineNumbersMerged;
        private SyncRichTextBox txtMerged;
        private List<int> diffLines = new List<int>();
        private SplitContainer splitContainer;

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

            // 👇 Setup line number boxes
            lineNumbersPrimary = new RichTextBox
            {
                Dock = DockStyle.Left,
                Width = 100,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };

            lineNumbersSecondary = new RichTextBox
            {
                Dock = DockStyle.Left,
                Width = 100,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };
            // 👇 Merged output
            lineNumbersMerged = new RichTextBox
            {
                Dock = DockStyle.Left,
                Width = 100,
                ReadOnly = true,
                BackColor = Color.LightGray,
                Font = new Font("Consolas", 10),
                ScrollBars = RichTextBoxScrollBars.None,
                BorderStyle = BorderStyle.None
            };

            // 👇 Setup diff boxes
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


            txtMerged = new SyncRichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = false,
                WordWrap = false,
                ScrollBars = RichTextBoxScrollBars.Both,
                Font = new Font("Consolas", 10),
                BackColor = Color.LightYellow
            };


            txtPrimary.Scroll += (s, e) =>
            {
                txtMerged.SyncScrollWith(txtPrimary);
                txtSecondary.SyncScrollWith(txtPrimary);    
                SyncScroll(lineNumbersPrimary, txtPrimary);   
                SyncScroll(lineNumbersSecondary, txtPrimary);
                SyncScroll(lineNumbersMerged, txtMerged);
            };

            txtSecondary.Scroll += (s, e) =>
            {
                txtMerged.SyncScrollWith(txtPrimary);
                txtPrimary.SyncScrollWith(txtSecondary);       
                SyncScroll(lineNumbersPrimary, txtPrimary);
                SyncScroll(lineNumbersSecondary, txtSecondary);
                SyncScroll(lineNumbersMerged, txtMerged);
            };


            txtMerged.Scroll += (s, e) =>
            {
                txtPrimary.SyncScrollWith(txtMerged); 
                txtSecondary.SyncScrollWith(txtMerged); 
                SyncScroll(lineNumbersPrimary, txtMerged); 
                SyncScroll(lineNumbersSecondary, txtMerged);
                SyncScroll(lineNumbersMerged, txtMerged);
            };

            var primaryPanel = new Panel { Dock = DockStyle.Fill };
            primaryPanel.Controls.Add(txtPrimary);
            primaryPanel.Controls.Add(lineNumbersPrimary);

            var secondaryPanel = new Panel { Dock = DockStyle.Fill };
            secondaryPanel.Controls.Add(txtSecondary);
            secondaryPanel.Controls.Add(lineNumbersSecondary);


            diffMapPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Black,
                Width = 16
            };
            diffMapPanel.Paint += DiffMapPanel_Paint;
            diffMapPanel.MouseClick += DiffMapPanel_MouseClick;

            layout.Controls.Add(primaryPanel, 0, 0);
            layout.Controls.Add(secondaryPanel, 1, 0);
            layout.Controls.Add(diffMapPanel, 2, 0);


      

            // 👇 Wrap merged view and line numbers in a panel
            var mergedPanel = new Panel { Dock = DockStyle.Fill };
            mergedPanel.Controls.Add(lineNumbersMerged);
            mergedPanel.Controls.Add(txtMerged);
           

            // 👇 Add to outer layout
            outerLayout.Controls.Add(buttonPanel, 0, 0);
            outerLayout.Controls.Add(layout, 0, 1);
            outerLayout.Controls.Add(mergedPanel, 0, 2); // ✅ REPLACE with panel

            this.Controls.Add(outerLayout);

            // 👇 Show diffs
            ShowDifferences(primaryCode, secondaryCode);

            // 👇 Merging button actions
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

            txtPrimary.Clear();
            txtSecondary.Clear();
            diffLines.Clear();
            lineNumbersPrimary.Clear();
            lineNumbersSecondary.Clear();

            int lineNum = 0;

            foreach (var diff in diffs)
            {
                bool isPrimaryMissing = string.IsNullOrEmpty(diff.primary);
                bool isSecondaryMissing = string.IsNullOrEmpty(diff.secondary);

                // 👇 Append actual code lines
                if (diff.isMatch)
                {
                    AppendColoredLine(txtMerged, diff.primary, Color.LightGreen);
                    AppendColoredLine(txtPrimary, diff.primary, Color.LightGreen);
                    AppendColoredLine(txtSecondary, diff.secondary, Color.LightGreen);
                }
                else
                {
                    diffLines.Add(lineNum);

                    AppendColoredLine(txtPrimary, isPrimaryMissing ? "" : diff.primary,
                        isPrimaryMissing ? Color.MediumVioletRed : Color.LightPink);

                    AppendColoredLine(txtSecondary, isSecondaryMissing ? "" : diff.secondary,
                        isSecondaryMissing ? Color.MediumVioletRed : Color.LightSkyBlue);
                }

                // 👇 Append line numbers with + or - markers
                string primaryMarker = isPrimaryMissing ? "   " : (isSecondaryMissing ? " -" : "  ");
                string secondaryMarker = isSecondaryMissing ? "   " : (isPrimaryMissing ? " +" : "  ");

                string lineLabel = (lineNum + 1).ToString().PadLeft(4);
                lineNumbersPrimary.AppendText(lineLabel + primaryMarker + "\n");
                lineNumbersSecondary.AppendText(lineLabel + secondaryMarker + "\n");

                lineNumbersMerged.AppendText(lineLabel+ "\n");
                lineNum++;
            }

            diffMapPanel.Invalidate();
        }

        private void UpdateLineNumbers(RichTextBox source, RichTextBox numberBox)
        {
            var lines = source.Lines;
            numberBox.Clear();
            for (int i = 0; i < lines.Length; i++)
            {
                numberBox.AppendText((i + 1).ToString().PadLeft(4) + "\n");
            }
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

        private void SyncScroll(RichTextBox target, RichTextBox reference)
        {
            int index = reference.GetCharIndexFromPosition(new Point(1, 1));
            int line = reference.GetLineFromCharIndex(index);
            int firstChar = target.GetFirstCharIndexFromLine(line);
            target.SelectionStart = firstChar;
            target.ScrollToCaret();
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
