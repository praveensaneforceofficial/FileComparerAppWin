using System;
using System.Drawing;
using System.Windows.Forms;

namespace FileComparerAppWin
{

    public partial class MainForm : Form
    {
        private Dictionary<TextBox, string> fullPathMap = new Dictionary<TextBox, string>();

        private TextBox txtFolder1;
        private TextBox txtFolder2;
        private TextBox txtFolder3;
        private Button btnBrowse1;
        private Button btnBrowse2;
        private Button btnBrowse3;
        private Button btnCompare;
        private Button btnSave;
        private Button btnLoad;

        private TreeView treePrimary;
        private TreeView treeCompare;
        private TreeView treeThird;

        private ListBox lstDifferences;

        private Button btnSame;
        private Button btnDiff;
        private Button btnMissingInPrimary;
        private Button btnMissingInCompare;
        private Button btnClear; 

        private TableLayoutPanel mainLayout;
        private FlowLayoutPanel summaryPanel;

        private string GetTrimmedPath(string fullPath, int maxSegments = 3)
        {
            string[] parts = fullPath.Split(Path.DirectorySeparatorChar);
            if (parts.Length <= maxSegments)
                return fullPath;

            return "~" + Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar.ToString(), parts.Skip(parts.Length - maxSegments));
        }

        private void InitializeComponent()
        {
            // 💡 Create controls
            this.txtFolder1 = new TextBox();
            this.txtFolder2 = new TextBox();
            this.txtFolder3 = new TextBox();

            this.btnBrowse1 = new Button();
            this.btnBrowse2 = new Button();
            this.btnBrowse3 = new Button();

            this.btnCompare = new Button();
            this.btnSave = new Button();
            this.btnLoad = new Button();

            this.treePrimary = new TreeView();
            this.treeCompare = new TreeView();
            this.treeThird = new TreeView();

            this.lstDifferences = new ListBox();

            this.btnSame = new Button();
            this.btnDiff = new Button();
            this.btnMissingInCompare = new Button();
            this.btnMissingInPrimary = new Button();
           

            this.mainLayout = new TableLayoutPanel();
            this.summaryPanel = new FlowLayoutPanel();

            btnClear = new Button(); // 🆕 Initialize Clear
            btnClear.Text = "Clear";
            btnClear.Size = new Size(100, 30);
            btnClear.Click += btnClear_Click;

            this.SuspendLayout();

            // 🧱 Form properties
            this.ClientSize = new Size(1000, 700);
            this.MinimumSize = new Size(850, 620);
            this.Text = "Folder Compare Tool";

            // 🧱 Main Layout
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.ColumnCount = 1;
            mainLayout.RowCount = 4;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));    // Top bar (Compare | Save | Load)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));    // Folder inputs
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));    // TreeViews
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));    // Summary + Differences
            this.Controls.Add(mainLayout);

            // 🔘 Top Bar: Compare | Save | Load
            var topBar = new FlowLayoutPanel();
            topBar.Dock = DockStyle.Fill;
            topBar.Padding = new Padding(10);
            topBar.FlowDirection = FlowDirection.LeftToRight;

            btnCompare.Text = "Compare";
            btnCompare.Size = new Size(100, 30);
            btnCompare.Click += btnCompare_Click;

            btnSave.Text = "Save";
            btnSave.Size = new Size(100, 30);
            btnSave.Click += btnSave_Click;

            btnLoad.Text = "Load";
            btnLoad.Size = new Size(100, 30);
            btnLoad.Click += btnLoad_Click;

            topBar.Controls.AddRange(new Control[] { btnCompare, btnSave, btnLoad,btnClear });
            mainLayout.Controls.Add(topBar, 0, 0); // 🧱 Row 0

            // 📁 Folder Input Layout
            var folderLayout = new TableLayoutPanel();
            folderLayout.Dock = DockStyle.Fill;
            folderLayout.ColumnCount = 3;
            folderLayout.RowCount = 2;

            folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            folderLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            txtFolder1.Dock = DockStyle.Fill;
            txtFolder2.Dock = DockStyle.Fill;
            txtFolder3.Dock = DockStyle.Fill;

            btnBrowse1.Text = "Browse";
            btnBrowse1.Dock = DockStyle.Fill;
            btnBrowse1.Click += btnBrowse1_Click;

            btnBrowse2.Text = "Browse";
            btnBrowse2.Dock = DockStyle.Fill;
            btnBrowse2.Click += btnBrowse2_Click;

            btnBrowse3.Text = "Browse";
            btnBrowse3.Dock = DockStyle.Fill;
            btnBrowse3.Click += btnBrowse3_Click;

            folderLayout.Controls.Add(txtFolder1, 0, 0);
            folderLayout.Controls.Add(txtFolder2, 1, 0);
            folderLayout.Controls.Add(txtFolder3, 2, 0);

            folderLayout.Controls.Add(btnBrowse1, 0, 1);
            folderLayout.Controls.Add(btnBrowse2, 1, 1);
            folderLayout.Controls.Add(btnBrowse3, 2, 1);

            mainLayout.Controls.Add(folderLayout, 0, 1); // 🧱 Row 1

            // 🌲 TreeViews
            var treeLayout = new TableLayoutPanel();
            treeLayout.Dock = DockStyle.Fill;
            treeLayout.ColumnCount = 3;
            treeLayout.RowCount = 1;

            treeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            treeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            treeLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));

            treePrimary.Dock = DockStyle.Fill;
            treeCompare.Dock = DockStyle.Fill;
            treeThird.Dock = DockStyle.Fill;

            treePrimary.CheckBoxes = true;
            treeCompare.CheckBoxes = true;
            treeThird.CheckBoxes = true;

            treeLayout.Controls.Add(treePrimary, 0, 0);
            treeLayout.Controls.Add(treeCompare, 1, 0);
            treeLayout.Controls.Add(treeThird, 2, 0);

            mainLayout.Controls.Add(treeLayout, 0, 2); // 🧱 Row 2

            // 📦 Bottom Panel (Summary + Differences)
            var bottomPanel = new Panel();
            bottomPanel.Dock = DockStyle.Fill;

            summaryPanel.Dock = DockStyle.Top;
            summaryPanel.Height = 40;
            summaryPanel.Padding = new Padding(10);
            summaryPanel.FlowDirection = FlowDirection.LeftToRight;
            summaryPanel.WrapContents = false;

            btnSame.Text = "✅ Same Items: 0";
            btnSame.Size = new Size(160, 30);
            btnSame.BackColor = Color.Green;
            btnSame.ForeColor = Color.White;
            btnSame.Click += BtnSame_Click;

            btnDiff.Text = "❌ Different Items: 0";
            btnDiff.Size = new Size(160, 30);
            btnDiff.BackColor = Color.HotPink;
            btnDiff.ForeColor = Color.White;
            btnDiff.Click += BtnDiff_Click;

            btnMissingInCompare.Text = "🔺 Primary Missing in Secondary: 0";
            btnMissingInCompare.Size = new Size(240, 30);
            btnMissingInCompare.BackColor = Color.Red;
            btnMissingInCompare.ForeColor = Color.White;
            btnMissingInCompare.Click += BtnMissingInCompare_Click;

            btnMissingInPrimary.Text = "🔻 Compare Missing in Primary: 0";
            btnMissingInPrimary.Size = new Size(240, 30);
            btnMissingInPrimary.BackColor = Color.Blue;
            btnMissingInPrimary.ForeColor = Color.White;
            btnMissingInPrimary.Click += BtnMissingInPrimary_Click;

            summaryPanel.Controls.AddRange(new Control[]
            {
                btnSame, btnDiff, btnMissingInPrimary,btnMissingInCompare
            });

            lstDifferences.Dock = DockStyle.Fill;
             lstDifferences.DisplayMember = "RelativePath";
            lstDifferences.DoubleClick += lstDifferences_DoubleClick;

            bottomPanel.Controls.Add(lstDifferences);
            bottomPanel.Controls.Add(summaryPanel);

            mainLayout.Controls.Add(bottomPanel, 0, 3); // 🧱 Row 3

            this.ResumeLayout(false);
        }

     //   private void lstDifferences_DoubleClick(object sender, EventArgs e) { /* Show detail */ }
    }
}
