using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Diagnostics;

namespace FileComparerAppWin
{
    public partial class MainForm : Form
    {
        private string folderPath1;
        private string folderPath2;
        private string folderPath3;
        private List<string> sameItems = new();
        private List<string> diffItems = new();
        private List<string> missingInCompare = new();
        private List<string> missingInPrimary = new();
        private string saveFolderPath;

        public MainForm()
        {
            InitializeComponent();

            // 📌 Hook TreeView checkbox cascade
            treePrimary.AfterCheck += TreeView_AfterCheck;
            treeCompare.AfterCheck += TreeView_AfterCheck;
            treeThird.AfterCheck += TreeView_AfterCheck;
        }

        private void btnBrowse1_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolder1.Text = dialog.SelectedPath;
                    LoadFolderToTree(dialog.SelectedPath, treePrimary);
                }
            }
        }

        private void btnBrowse2_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolder2.Text = dialog.SelectedPath;
                    LoadFolderToTree(dialog.SelectedPath, treeCompare);
                }
            }
        }

        private void PopulateThirdTree()
        {
            if (string.IsNullOrWhiteSpace(folderPath1) || string.IsNullOrWhiteSpace(folderPath2))
                return;

            treeThird.Nodes.Clear();

            var allPaths = sameItems
                .Concat(diffItems)
                .Concat(missingInCompare)
                .Concat(missingInPrimary)
                .Distinct()
                .ToList();

            TreeNode rootNode = new TreeNode("Compared Files and Folders");
            treeThird.Nodes.Add(rootNode);

            foreach (string relativePath in allPaths)
            {
                string[] parts = relativePath.Split(Path.DirectorySeparatorChar);
                TreeNode currentNode = rootNode;

                for (int i = 0; i < parts.Length; i++)
                {
                    string part = parts[i];
                    TreeNode existingNode = currentNode.Nodes.Cast<TreeNode>().FirstOrDefault(n => n.Text == part);
                    if (existingNode == null)
                    {
                        existingNode = new TreeNode(part);
                        currentNode.Nodes.Add(existingNode);
                    }
                    currentNode = existingNode;
                }

                // At leaf level, mark status
                Color color = Color.Black;
                float opacity = 1.0f;

                if (sameItems.Contains(relativePath))
                {
                    color = Color.Green;
                }
                else if (diffItems.Contains(relativePath))
                {
                    color = Color.HotPink;
                }
                else if (missingInCompare.Contains(relativePath))
                {
                    color = Color.Red;
                    opacity = 0.4f;
                }
                else if (missingInPrimary.Contains(relativePath))
                {
                    color = Color.Blue;
                    opacity = 0.4f;
                }

                currentNode.ForeColor = Color.FromArgb((int)(255 * opacity), color);
            }

            rootNode.Expand();
        }


        private void LoadFolderToTree(string rootPath, TreeView treeView)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
               
                return;
            }

            if (!Directory.Exists(rootPath))
            {
                MessageBox.Show($"Folder not found:\n{rootPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            treeView.Nodes.Clear();
            DirectoryInfo rootDir = new DirectoryInfo(rootPath);

            TreeNode rootNode = new TreeNode(rootDir.Name)
            {
                Tag = rootDir.FullName
            };

            treeView.Nodes.Add(rootNode);
            LoadSubDirs(rootDir, rootNode);
          //  treeView.ExpandAll(); // Optional: Expand by default
        }

        private void LoadSubDirs(DirectoryInfo dir, TreeNode node)
        {
            // Add folders
            foreach (var subDir in dir.GetDirectories())
            {
                TreeNode subNode = new TreeNode(subDir.Name)
                {
                    Tag = subDir.FullName
                };
                node.Nodes.Add(subNode);
                LoadSubDirs(subDir, subNode);
            }

            // Add files
            foreach (var file in dir.GetFiles())
            {
                TreeNode fileNode = new TreeNode(file.Name)
                {
                    Tag = file.FullName
                };
                node.Nodes.Add(fileNode);
            }
        }

        private List<(string FullPath, string RelativePath)> GetCheckedFilesWithRelativePath(TreeView treeView)
        {
            var files = new List<(string, string)>();
            foreach (TreeNode node in treeView.Nodes)
            {
                CollectCheckedFilesWithRelativePathRecursive(node, files);
            }
            return files;
        }

        private void CollectCheckedFilesWithRelativePathRecursive(TreeNode node, List<(string, string)> files)
        {
            if (node.Checked)
            {
                string rootPath = GetCheckedRootPath(node);
                string relativePath = node.Tag.ToString().Substring(rootPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar);

                if (File.Exists(node.Tag.ToString()))
                {
                    files.Add((node.Tag.ToString(), relativePath));
                }
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollectCheckedFilesWithRelativePathRecursive(child, files);
            }
        }

        private void CollectCheckedFilesWithRelativePathRecursive(TreeNode node, List<(string, string)> files, string rootPath)
        {
            if (node.Checked && File.Exists(node.Tag.ToString()))
            {
                string relativePath = node.Tag.ToString().Substring(rootPath.Length)
                    .TrimStart(Path.DirectorySeparatorChar);
                files.Add((node.Tag.ToString(), relativePath));
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollectCheckedFilesWithRelativePathRecursive(child, files, rootPath);
            }
        }

        // ✅ Only get checked files
        private List<string> GetCheckedFiles(TreeView treeView)
        {
            var files = new List<string>();
            foreach (TreeNode node in treeView.Nodes)
            {
                CollectCheckedFilesRecursive(node, files);
            }
            return files;
        }

        private void CollectCheckedFilesRecursive(TreeNode node, List<string> files)
        {
            if (node.Checked && File.Exists(node.Tag.ToString()))
            {
                files.Add(node.Tag.ToString());
            }

            foreach (TreeNode child in node.Nodes)
            {
                CollectCheckedFilesRecursive(child, files);
            }
        }

        private CancellationTokenSource _cts;
        private DateTime _lastCheckTime;

        private CancellationTokenSource _checkOperationCts;

        private async void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown) return;

            // Cancel any previous operation
            _checkOperationCts?.Cancel();
            _checkOperationCts = new CancellationTokenSource();

            try
            {
                // Only show loader for operations that might take time
                bool shouldShowLoader = e.Node.Nodes.Count > 10; // Adjust threshold as needed

                if (shouldShowLoader)
                {
                    using (var loader = new LoadingForm())
                    {
                        // Show loader after a small delay (avoids flicker for quick operations)
                        var showTask = Task.Delay(200); // 200ms delay
                        var workTask = ProcessCheckOperationAsync(e.Node, _checkOperationCts.Token);

                        if (await Task.WhenAny(showTask, workTask) == showTask)
                        {
                            loader.Show();
                            loader.Refresh();
                            await workTask;
                        }
                    }
                }
                else
                {
                    await ProcessCheckOperationAsync(e.Node, _checkOperationCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled - ignore
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Checkbox operation failed: {ex.Message}");
            }
        }


        private Task ProcessCheckOperationAsync(TreeNode node, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                if (ct.IsCancellationRequested) return;

                this.Invoke((MethodInvoker)(() =>
                {
                    // Suspend drawing for performance
                    treePrimary.BeginUpdate();
                    treeCompare.BeginUpdate();
                    treeThird.BeginUpdate();

                    try
                    {
                        CheckAllChildNodes(node, node.Checked);
                        UpdateParentCheckState(node);
                    }
                    finally
                    {
                        // Resume drawing
                        treePrimary.EndUpdate();
                        treeCompare.EndUpdate();
                        treeThird.EndUpdate();
                    }
                }));
            }, ct);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _checkOperationCts?.Dispose();
            base.OnFormClosing(e);
        }

        private void CheckAllChildNodes(TreeNode node, bool isChecked)
        {
            foreach (TreeNode child in node.Nodes)
            {
                child.Checked = isChecked;
                if (child.Nodes.Count > 0)
                    CheckAllChildNodes(child, isChecked);
            }
        }

        // ✅ Async comparison with loader
        private async void btnCompare_Click(object sender, EventArgs e)
        {
            folderPath1 = txtFolder1.Text;
            folderPath2 = txtFolder2.Text;

            if (string.IsNullOrEmpty(folderPath1) || string.IsNullOrEmpty(folderPath2))
            {
                MessageBox.Show("Please select both folders.");
                return;
            }

            using (var loader = new LoadingForm())
            {
                loader.Show();
                loader.Refresh();

                await Task.Run(() => CompareFolders());

                loader.Close();
            }
        }

        private string GetCheckedRootPath(TreeNode node)
        {
            TreeNode current = node;
            string rootPath = node.Tag.ToString();

            while (current.Parent != null)
            {
                if (current.Parent.Checked)
                {
                    current = current.Parent;
                    rootPath = current.Tag.ToString();
                }
                else
                {
                    break;
                }
            }

            return rootPath;
        }

        private void CompareFolders()
        {
            sameItems.Clear();
            diffItems.Clear();
            missingInCompare.Clear();
            missingInPrimary.Clear();

            // Get checked files with relative paths from their checked roots
            var primaryFiles = GetCheckedFilesWithRelativePath(treePrimary);
            var compareFiles = GetCheckedFilesWithRelativePath(treeCompare);

            // Extract just the relative paths for comparison
            var files1 = primaryFiles.Select(f => f.RelativePath).ToList();
            var files2 = compareFiles.Select(f => f.RelativePath).ToList();

            // Split into three categories
            var onlyInPrimary = files1.Except(files2).ToList();
            var onlyInCompare = files2.Except(files1).ToList();
            var inBoth = files1.Intersect(files2).ToList();

            // Compare files that exist on both sides
            foreach (var relativePath in inBoth)
            {
                var fullPath1 = primaryFiles.First(f => f.RelativePath == relativePath).FullPath;
                var fullPath2 = compareFiles.First(f => f.RelativePath == relativePath).FullPath;

                if (!File.Exists(fullPath1))
                {
                    missingInCompare.Add(relativePath);
                    continue;
                }

                if (!File.Exists(fullPath2))
                {
                    missingInPrimary.Add(relativePath);
                    continue;
                }

                if (!FileEquals(fullPath1, fullPath2))
                {
                    diffItems.Add(relativePath);
                }
                else
                {
                    sameItems.Add(relativePath);
                }
            }

            // Add non-matching files
            missingInCompare.AddRange(onlyInPrimary);
            missingInPrimary.AddRange(onlyInCompare);

            // Update UI
            this.Invoke(() =>
            {
                lstDifferences.Items.Clear();

                btnSame.Text = $"Same Items: {sameItems.Count}";
                btnSame.BackColor = Color.Green;
                btnSame.ForeColor = Color.White;

                btnDiff.Text = $"Different Items: {diffItems.Count}";
                btnDiff.BackColor = Color.HotPink;
                btnDiff.ForeColor = Color.White;

                btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
                btnMissingInCompare.BackColor = Color.Red;
                btnMissingInCompare.ForeColor = Color.White;

                btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
                btnMissingInPrimary.BackColor = Color.Blue;
                btnMissingInPrimary.ForeColor = Color.White;

                lstDifferences.Items.Add("Click any summary button to view file list.");
                PopulateThirdTree();
            });
        }

        private bool FileEquals(string file1, string file2)
        {
            var content1 = File.ReadAllBytes(file1);
            var content2 = File.ReadAllBytes(file2);
            return content1.SequenceEqual(content2);
        }

        private void BtnSame_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in sameItems)
                lstDifferences.Items.Add(file);
        }

        private void BtnDiff_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in diffItems)
                lstDifferences.Items.Add(file);
        }

        private void BtnMissingInCompare_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in missingInCompare)
                lstDifferences.Items.Add(file);
        }

        private void BtnMissingInPrimary_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in missingInPrimary)
                lstDifferences.Items.Add(file);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Comparison Files (*.cmp)|*.cmp|All Files (*.*)|*.*";
                dialog.Title = "Save Comparison Result";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var result = new ComparisonResult
                    {
                        FolderPath1 = folderPath1,
                        FolderPath2 = folderPath2,
                        FolderPath3 = folderPath3,
                        CheckedFilesPrimary = GetCheckedFiles(treePrimary),
                        CheckedFilesCompare = GetCheckedFiles(treeCompare),
                        CheckedFilesThird = GetCheckedFiles(treeCompare),
                        SameItems = sameItems,
                        DiffItems = diffItems,
                        MissingInCompare = missingInCompare,
                        MissingInPrimary = missingInPrimary,
                        ExpandedNodesPrimary = GetExpandedNodes(treePrimary),
                        ExpandedNodesCompare = GetExpandedNodes(treeCompare),
                        ExpandedNodesThird = GetExpandedNodes(treeThird)
                    };

                    string json = JsonSerializer.Serialize(result);
                    File.WriteAllText(dialog.FileName, json);
                }
            }
        }


        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Comparison Files (*.cmp)|*.cmp|All Files (*.*)|*.*";
                dialog.Title = "Load Comparison Result";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string json = File.ReadAllText(dialog.FileName);
                    var result = JsonSerializer.Deserialize<ComparisonResult>(json);

                    if (result != null)
                    {
                        sameItems = result.SameItems ?? new();
                        diffItems = result.DiffItems ?? new();
                        missingInCompare = result.MissingInCompare ?? new();
                        missingInPrimary = result.MissingInPrimary ?? new();

                        folderPath1 = result.FolderPath1;
                        folderPath2 = result.FolderPath2;
                        folderPath3 = result.FolderPath3;
                        // txtFolder1.Text = GetTrimmedPath(folderPath1);
                        //txtFolder2.Text = GetTrimmedPath(folderPath2);
                        // txtFolder2.Text = GetTrimmedPath(folderPath3);

                        txtFolder1.Text = folderPath1;
                        txtFolder2.Text = folderPath2;
                        txtFolder3.Text = folderPath3;

                        LoadFolderToTree(folderPath1, treePrimary);
                        LoadFolderToTree(folderPath2, treeCompare);
                        LoadFolderToTree(folderPath3, treeThird);
                        // 📌 Delay checkbox restore after UI tree is built
                        this.BeginInvoke(async () =>

                        {
                            SetCheckedNodes(treePrimary, result.CheckedFilesPrimary);
                            SetCheckedNodes(treeCompare, result.CheckedFilesCompare);
                            SetCheckedNodes(treeThird, result.CheckedFilesThird);
                            ExpandNodes(treePrimary, result.ExpandedNodesPrimary);
                            ExpandNodes(treeCompare, result.ExpandedNodesCompare);
                            ExpandNodes(treeThird, result.ExpandedNodesThird);
                            // 📌 NEW CODE: Force UI to process checkbox update fully
                            treePrimary.EndUpdate(); // 🔁 Force TreeView to process changes
                            treeCompare.EndUpdate();
                            treeThird.EndUpdate();
                            await Task.Delay(100);
                        });


                        RefreshSummaryUI();
                    }
                }
            }
        }

        private void RefreshSummaryUI()
        {
            lstDifferences.Items.Clear();


            btnSame.Text = $"Same Items: {sameItems.Count}";
            btnSame.BackColor = Color.Green;
            btnSame.ForeColor = Color.White;

            btnDiff.Text = $"Different Items: {diffItems.Count}";
            btnDiff.BackColor = Color.HotPink;
            btnDiff.ForeColor = Color.White;

            btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
            btnMissingInCompare.BackColor = Color.Red;
            btnMissingInCompare.ForeColor = Color.White;

            btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
            btnMissingInPrimary.BackColor = Color.Blue;
            btnMissingInPrimary.ForeColor = Color.White;


            /*
            btnSame.Text = $"✅ Same Items: {sameItems.Count}";
            btnDiff.Text = $"❌ Different Items: {diffItems.Count}";
            btnMissingInCompare.Text = $"🔺 Missing in Secondary: {missingInCompare.Count}";
            btnMissingInPrimary.Text = $"🔻 Missing in Primary: {missingInPrimary.Count}";*/
            lstDifferences.Items.Add("Click any summary button to view file list.");
        }

        private void btnBrowse3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select folder to save comparison result";
                if (dialog.ShowDialog() == DialogResult.OK)
                {

                    txtFolder3.Text = dialog.SelectedPath;
                    saveFolderPath = dialog.SelectedPath;
                    MessageBox.Show($"Selected folder: {saveFolderPath}");
                }
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            folderPath1 = folderPath2 = folderPath3 = string.Empty;
            txtFolder1.Text = txtFolder2.Text = txtFolder3.Text = "";
            treePrimary.Nodes.Clear();
            treeCompare.Nodes.Clear();

            treeThird.Nodes.Clear();
            sameItems.Clear();
            diffItems.Clear();
            missingInCompare.Clear();
            missingInPrimary.Clear();
            lstDifferences.Items.Clear();

            btnSame.Text = $"Same Items: {sameItems.Count}";
            btnSame.BackColor = Color.Green;
            btnSame.ForeColor = Color.White;

            btnDiff.Text = $"Different Items: {diffItems.Count}";
            btnDiff.BackColor = Color.HotPink;
            btnDiff.ForeColor = Color.White;

            btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
            btnMissingInCompare.BackColor = Color.Red;
            btnMissingInCompare.ForeColor = Color.White;

            btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
            btnMissingInPrimary.BackColor = Color.Blue;
            btnMissingInPrimary.ForeColor = Color.White;
            /*

            btnSame.Text = "✅ Same Items";
            btnDiff.Text = "❌ Different Items";
            btnMissingInCompare.Text = "🔺 Missing in Secondary";
            btnMissingInPrimary.Text = "🔻 Missing in Primary"; */
        }

        private void SetCheckedNodes(TreeView treeView, List<string> fullPaths)
        {
            var normalizedPaths = fullPaths
                .Select(p => Path.GetFullPath(p).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant())
                .ToHashSet();

            void CheckMatching(TreeNode node)
            {
                if (node.Tag is string tag &&
                    normalizedPaths.Contains(Path.GetFullPath(tag).TrimEnd(Path.DirectorySeparatorChar).ToLowerInvariant()))
                {
                    node.Checked = true;
                    CheckAllChildNodes(node, true);
                    UpdateParentCheckState(node);
                }

                foreach (TreeNode child in node.Nodes)
                    CheckMatching(child);
            }

            foreach (TreeNode node in treeView.Nodes)
                CheckMatching(node);
        }

        private void lstDifferences_DoubleClick(object sender, EventArgs e)
        {
            if (lstDifferences.SelectedItem == null) return;

            string selectedDifference = lstDifferences.SelectedItem.ToString();

            string primaryPath = Path.Combine(folderPath1, selectedDifference);
            string secondaryPath = Path.Combine(folderPath2, selectedDifference);

            // Check which files exist and which are missing
            bool primaryExists = File.Exists(primaryPath);
            bool secondaryExists = File.Exists(secondaryPath);

            if (!primaryExists || !secondaryExists)
            {
                string message = "File status:\n\n";
                message += $"Primary location: {primaryPath}\n";
                message += $"Status: {(primaryExists ? "Found" : "Missing")}\n\n";
                message += $"Secondary location: {secondaryPath}\n";
                message += $"Status: {(secondaryExists ? "Found" : "Missing")}";

                MessageBox.Show(message, "File Comparison Status",
                              MessageBoxButtons.OK,
                              primaryExists && secondaryExists ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                return;
            }

            // Show loading form
            LoadingForm loading = new LoadingForm();
            loading.Show();

            // Compare files in background
            Task.Run(() =>
            {
                try
                {
                    string primaryCode = File.ReadAllText(primaryPath);
                    string secondaryCode = File.ReadAllText(secondaryPath);

                    this.Invoke((MethodInvoker)delegate
                    {
                        loading.Close();
                        var viewer = new DiffViewerForm(primaryCode, secondaryCode);
                        viewer.ShowDialog();
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        loading.Close();
                        MessageBox.Show($"Error comparing files:\n{ex.Message}",
                                       "Comparison Error",
                                       MessageBoxButtons.OK,
                                       MessageBoxIcon.Error);
                    });
                }
            });
        }

        private void UpdateParentCheckState(TreeNode node)
        {
            if (node.Parent == null) return;

            bool allChecked = node.Parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
            bool anyChecked = node.Parent.Nodes.Cast<TreeNode>().Any(n => n.Checked);

            node.Parent.Checked = allChecked;

            // Recursively update parent
            UpdateParentCheckState(node.Parent);
        }

        private List<string> GetExpandedNodes(TreeView treeView)
        {
            var expanded = new List<string>();

            void Collect(TreeNode node)
            {
                // ✅ Only process if Tag is a string and points to an existing directory
                if (node.IsExpanded && node.Tag is string tag && Directory.Exists(tag))
                {
                    expanded.Add(tag);
                }

                // ✅ Recurse through all children
                foreach (TreeNode child in node.Nodes)
                    Collect(child);
            }

            // ✅ Start from all root nodes
            foreach (TreeNode node in treeView.Nodes)
                Collect(node);

            return expanded;
        }

        private void ExpandNodes(TreeView treeView, List<string> expandedPaths)
        {
            void ExpandMatching(TreeNode node)
            {
                if (node.Tag is string tag && expandedPaths.Contains(tag))
                    node.Expand();

                foreach (TreeNode child in node.Nodes)
                    ExpandMatching(child);
            }

            foreach (TreeNode node in treeView.Nodes)
                ExpandMatching(node);
        }


    }

    [Serializable]
    public class ComparisonResult
    {
        public List<string> ExpandedNodesPrimary { get; set; }   // 📌 For storing expanded folders
        public List<string> ExpandedNodesCompare { get; set; }
        public List<string> ExpandedNodesThird { get; set; }

        
        public string FolderPath1 { get; set; }   // 📌 Save Primary path
        public string FolderPath2 { get; set; }   // 📌 Save Compare path

        public string FolderPath3 { get; set; }

        public List<string> CheckedFilesPrimary { get; set; }   // 📌 Save selected files
        public List<string> CheckedFilesCompare { get; set; }

        public List<string> CheckedFilesThird { get; set; }

        public List<string> SameItems { get; set; }
        public List<string> DiffItems { get; set; }
        public List<string> MissingInCompare { get; set; }
        public List<string> MissingInPrimary { get; set; }

    }


}
