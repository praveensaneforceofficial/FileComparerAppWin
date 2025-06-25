using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Diagnostics;
using System.Threading;
using System.ComponentModel;


namespace FileComparerAppWin
{
    public partial class MainForm : Form
    {
        private string folderPath1;
        private string folderPath2;
        private string folderPath3;
        private string saveFolderPath;

        private List<ComparedFile> sameItems = new List<ComparedFile>();
        private List<ComparedFile> diffItems = new List<ComparedFile>();
        private List<ComparedFile> missingInCompare = new List<ComparedFile>();
        private List<ComparedFile> missingInPrimary = new List<ComparedFile>();

        private MainContainerForm parentContainer;


        public MainForm(MainContainerForm mainContainerForm)
        {
            parentContainer = mainContainerForm;
            InitializeComponent();
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

            var allFiles = sameItems.Concat(diffItems)
                                  .Concat(missingInCompare)
                                  .Concat(missingInPrimary)
                                  .ToList();

            TreeNode rootNode = new TreeNode("Compared Files and Folders");
            treeThird.Nodes.Add(rootNode);

            foreach (var file in allFiles)
            {
                string[] parts = file.RelativePath.Split(Path.DirectorySeparatorChar);
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

                Color color = Color.Black;
                float opacity = 1.0f;

                if (sameItems.Contains(file))
                {
                    color = Color.Green;
                }
                else if (diffItems.Contains(file))
                {
                    color = Color.HotPink;
                }
                else if (missingInCompare.Contains(file))
                {
                    color = Color.Red;
                    opacity = 0.4f;
                }
                else if (missingInPrimary.Contains(file))
                {
                    color = Color.Blue;
                    opacity = 0.4f;
                }

                currentNode.ForeColor = Color.FromArgb((int)(255 * opacity), color);
                currentNode.Tag = file;
            }

            rootNode.Expand();
        }

        private void LoadFolderToTree(string rootPath, TreeView treeView)
        {
            if (string.IsNullOrWhiteSpace(rootPath)) return;

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
        }

        private void LoadSubDirs(DirectoryInfo dir, TreeNode node)
        {
            foreach (var subDir in dir.GetDirectories())
            {
                TreeNode subNode = new TreeNode(subDir.Name)
                {
                    Tag = subDir.FullName
                };
                node.Nodes.Add(subNode);
                LoadSubDirs(subDir, subNode);
            }

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

        private async void TreeView_AfterCheck(object sender, TreeViewEventArgs e)
        {
            if (e.Action == TreeViewAction.Unknown) return;

            _checkOperationCts?.Cancel();
            _checkOperationCts = new CancellationTokenSource();

            try
            {
                bool shouldShowLoader = e.Node.Nodes.Count > 10;
                if (shouldShowLoader)
                {
                    using (var loader = new LoadingForm())
                    {
                        var showTask = Task.Delay(200);
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
            catch (OperationCanceledException) { }
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
                        treePrimary.EndUpdate();
                        treeCompare.EndUpdate();
                        treeThird.EndUpdate();
                    }
                }));
            }, ct);
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
            using (var cts = new CancellationTokenSource())
            {
                var progress = new Progress<int>(percent =>
                {
                    loader.UpdateProgress(percent, $"🔄 Comparing folders... ({percent}%)");
                });

                loader.CancelRequested += (s, args) =>
                {
                    cts.Cancel();
                };

                loader.Show();
                loader.Refresh();

                try
                {
                    await Task.Run(() => CompareFolders(progress, cts.Token), cts.Token);

                    this.Invoke(() =>
                    {
                        lstDifferences.Items.Clear();
                        btnSame.Text = $"Same Items: {sameItems.Count}";
                        btnDiff.Text = $"Different Items: {diffItems.Count}";
                        btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
                        btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
                        lstDifferences.Items.Add("Click any summary button to view file list.");
                        PopulateThirdTree();
                    });
                }
                catch (OperationCanceledException)
                {
                    this.Invoke(() =>
                    {
                        lstDifferences.Items.Clear();
                        lstDifferences.Items.Add("Comparison was cancelled by user. Showing partial results:");

                        // Show partial results
                        btnSame.Text = $"Same Items: {sameItems.Count}";
                        btnDiff.Text = $"Different Items: {diffItems.Count}";
                        btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
                        btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
                        PopulateThirdTree();
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke(() =>
                    {
                        MessageBox.Show($"Error during comparison: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    });
                }
                finally
                {
                    loader.Close();
                }
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


        private void CompareFolders(IProgress<int> progress, CancellationToken cancellationToken)
        {
            sameItems.Clear();
            diffItems.Clear();
            missingInCompare.Clear();
            missingInPrimary.Clear();

            var primaryFiles = GetCheckedFilesWithRelativePath(treePrimary);
            var compareFiles = GetCheckedFilesWithRelativePath(treeCompare);

            var files1 = primaryFiles.Select(f => f.RelativePath).ToList();
            var files2 = compareFiles.Select(f => f.RelativePath).ToList();

            var onlyInPrimary = files1.Except(files2).ToList();
            var onlyInCompare = files2.Except(files1).ToList();
            var inBoth = files1.Intersect(files2).ToList();

            int totalFiles = inBoth.Count + onlyInPrimary.Count + onlyInCompare.Count;
            int processedFiles = 0;

            foreach (var relativePath in inBoth)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                var primaryFile = primaryFiles.First(f => f.RelativePath == relativePath);
                var compareFile = compareFiles.First(f => f.RelativePath == relativePath);

                if (!File.Exists(primaryFile.FullPath))
                {
                    missingInCompare.Add(new ComparedFile
                    {
                        RelativePath = relativePath,
                        PrimaryPath = primaryFile.FullPath,
                        Status = "MissingInCompare"
                    });
                    continue;
                }

                if (!File.Exists(compareFile.FullPath))
                {
                    missingInPrimary.Add(new ComparedFile
                    {
                        RelativePath = relativePath,
                        ComparePath = compareFile.FullPath,
                        Status = "MissingInPrimary"
                    });
                    continue;
                }

                if (!FileEquals(primaryFile.FullPath, compareFile.FullPath))
                {
                    diffItems.Add(new ComparedFile
                    {
                        RelativePath = relativePath,
                        PrimaryPath = primaryFile.FullPath,
                        ComparePath = compareFile.FullPath,
                        Status = "Different"
                    });
                }
                else
                {
                    sameItems.Add(new ComparedFile
                    {
                        RelativePath = relativePath,
                        PrimaryPath = primaryFile.FullPath,
                        ComparePath = compareFile.FullPath,
                        Status = "Same"
                    });
                }

                processedFiles++;
                int percent = (int)((double)processedFiles / totalFiles * 100);
                progress?.Report(percent);
            }

            // Check for cancellation before processing remaining files
            cancellationToken.ThrowIfCancellationRequested();

            missingInCompare.AddRange(onlyInPrimary.Select(relPath => new ComparedFile
            {
                RelativePath = relPath,
                PrimaryPath = primaryFiles.First(f => f.RelativePath == relPath).FullPath,
                Status = "MissingInCompare"
            }));

            missingInPrimary.AddRange(onlyInCompare.Select(relPath => new ComparedFile
            {
                RelativePath = relPath,
                ComparePath = compareFiles.First(f => f.RelativePath == relPath).FullPath,
                Status = "MissingInPrimary"
            }));

            progress?.Report(100);

            this.Invoke(() =>
            {
                lstDifferences.Items.Clear();
                btnSame.Text = $"Same Items: {sameItems.Count}";
                btnDiff.Text = $"Different Items: {diffItems.Count}";
                btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
                btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
                lstDifferences.Items.Add("Click any summary button to view file list.");
                PopulateThirdTree();
            });
        }

        private string Normalize(string line)
        {
            return line?.Trim().Replace(" ", "").Replace("\t", "").ToLowerInvariant();
        }

        private bool FileEquals(string file1, string file2)
        {
            // Read lines from both files
            var primaryLines = File.ReadAllLines(file1); // Read primary file
            var secondaryLines = File.ReadAllLines(file2); // Read secondary file

            // Run line diff using GetLineDiffs logic
            int m = primaryLines.Length;
            int n = secondaryLines.Length;
            int[,] lcs = new int[m + 1, n + 1];

            // Build LCS matrix
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

            // Backtrack to find diff and detect mismatch
            int x = m, y = n;
            while (x > 0 && y > 0)
            {
                if (Normalize(primaryLines[x - 1]) == Normalize(secondaryLines[y - 1]))
                {
                    x--; y--;
                }
                else if (lcs[x - 1, y] >= lcs[x, y - 1])
                {
                    return false; // Mismatch found
                }
                else
                {
                    return false; // Mismatch found
                }
            }

            // If any lines are left in either file, they don't match
            if (x > 0 || y > 0)
                return false;

            return true; // All lines match
        }

        private void BtnSame_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in sameItems)
            {
                lstDifferences.Items.Add(file);

            }
        }

        private void BtnDiff_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in diffItems)
            {
                lstDifferences.Items.Add(file);

            }
        }

        private void BtnMissingInCompare_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in missingInCompare)
            {
                lstDifferences.Items.Add(file);

            }
        }

        private void BtnMissingInPrimary_Click(object sender, EventArgs e)
        {
            lstDifferences.Items.Clear();
            foreach (var file in missingInPrimary)
            {
                lstDifferences.Items.Add(file);

            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Comparison Files (*.cmp)|*.cmp|All Files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var result = new ComparisonResult
                    {
                        FolderPath1 = folderPath1,
                        FolderPath2 = folderPath2,
                        FolderPath3 = folderPath3,
                        CheckedFilesPrimary = GetCheckedFiles(treePrimary),
                        CheckedFilesCompare = GetCheckedFiles(treeCompare),
                        CheckedFilesThird = GetCheckedFiles(treeThird),
                        SameItems = sameItems.Select(f => f.RelativePath).ToList(),
                        DiffItems = diffItems.Select(f => f.RelativePath).ToList(),
                        MissingInCompare = missingInCompare.Select(f => f.RelativePath).ToList(),
                        MissingInPrimary = missingInPrimary.Select(f => f.RelativePath).ToList(),
                        ExpandedNodesPrimary = GetExpandedNodes(treePrimary),
                        ExpandedNodesCompare = GetExpandedNodes(treeCompare),
                        ExpandedNodesThird = GetExpandedNodes(treeThird)
                    };

                    File.WriteAllText(dialog.FileName, JsonSerializer.Serialize(result));
                }
            }
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Comparison Files (*.cmp)|*.cmp|All Files (*.*)|*.*";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var result = JsonSerializer.Deserialize<ComparisonResult>(File.ReadAllText(dialog.FileName));
                    if (result != null)
                    {
                        sameItems = result.SameItems.Select(relPath => new ComparedFile { RelativePath = relPath }).ToList();
                        diffItems = result.DiffItems.Select(relPath => new ComparedFile { RelativePath = relPath }).ToList();
                        missingInCompare = result.MissingInCompare.Select(relPath => new ComparedFile { RelativePath = relPath }).ToList();
                        missingInPrimary = result.MissingInPrimary.Select(relPath => new ComparedFile { RelativePath = relPath }).ToList();

                        folderPath1 = result.FolderPath1;
                        folderPath2 = result.FolderPath2;
                        folderPath3 = result.FolderPath3;

                        txtFolder1.Text = folderPath1;
                        txtFolder2.Text = folderPath2;
                        txtFolder3.Text = folderPath3;

                        LoadFolderToTree(folderPath1, treePrimary);
                        LoadFolderToTree(folderPath2, treeCompare);
                        LoadFolderToTree(folderPath3, treeThird);

                        this.BeginInvoke(async () =>
                        {
                            SetCheckedNodes(treePrimary, result.CheckedFilesPrimary);
                            SetCheckedNodes(treeCompare, result.CheckedFilesCompare);
                            SetCheckedNodes(treeThird, result.CheckedFilesThird);
                            ExpandNodes(treePrimary, result.ExpandedNodesPrimary);
                            ExpandNodes(treeCompare, result.ExpandedNodesCompare);
                            ExpandNodes(treeThird, result.ExpandedNodesThird);
                            await Task.Delay(100);
                            RefreshSummaryUI();
                        });
                    }
                }
            }
        }


      private void lstDifferences_DoubleClick(object sender, EventArgs e)
{
    // 📌 Validate sender and selected item
    if (!(sender is ListBox listBox) || listBox.SelectedItem == null)
        return;

    // 📌 Try casting to your data model
    if (!(listBox.SelectedItem is ComparedFile fileData))
    {
        MessageBox.Show("No file data associated with this item.", "Error",
                       MessageBoxButtons.OK, MessageBoxIcon.Error);
        return;
    }

    // 📌 Check if files exist
    bool primaryExists = !string.IsNullOrEmpty(fileData.PrimaryPath) && File.Exists(fileData.PrimaryPath);
    bool secondaryExists = !string.IsNullOrEmpty(fileData.ComparePath) && File.Exists(fileData.ComparePath);

    if (!primaryExists || !secondaryExists)
    {
        string message = $"Primary: {fileData.PrimaryPath ?? "N/A"}\nStatus: {(primaryExists ? "Found" : "Missing")}\n\n" +
                        $"Secondary: {fileData.ComparePath ?? "N/A"}\nStatus: {(secondaryExists ? "Found" : "Missing")}";

        MessageBox.Show(message, "File Status", MessageBoxButtons.OK,
            primaryExists && secondaryExists ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        return;
    }

    // 🧪 Only handle "Different" status files
    if (fileData.Status == "Different")
    {
        var loader = new LoadingForm();
        loader.Show();

        Task.Run(() =>
        {
            try
            {
                // 🧾 Load both file contents
                string primaryText = File.ReadAllText(fileData.PrimaryPath);
                string secondaryText = File.ReadAllText(fileData.ComparePath);

                this.Invoke((MethodInvoker)delegate
                {
                    loader.Close();

                    // 🧩 Instead of showing a modal window...
                    // ❗ Make sure tabControl1 exists on your form

                    parentContainer.OpenDiffTab(
                        $"{Path.GetFileName(fileData.PrimaryPath)} vs {Path.GetFileName(fileData.ComparePath)}",
                        primaryText,
                        secondaryText
                    );

                    /*

                    var tabPage = new TabPage($"{Path.GetFileName(fileData.PrimaryPath)} vs {Path.GetFileName(fileData.ComparePath)}");

                    // 🔄 Replace this with your actual diff viewer control
                    var diffViewer = new DiffViewerControl(primaryText, secondaryText); 
                    diffViewer.Dock = DockStyle.Fill;

                    tabPage.Controls.Add(diffViewer);
                    */
                  //  tabControl1.TabPages.Add(tabPage);
                    //tabControl1.SelectedTab = tabPage;
                });
            }
            catch (Exception ex)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    loader.Close();
                    MessageBox.Show($"Error comparing files:\n{ex.Message}",
                                  "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                });
            }
        });
    }
}

        private void UpdateParentCheckState(TreeNode node)
        {
            if (node.Parent == null) return;
            node.Parent.Checked = node.Parent.Nodes.Cast<TreeNode>().All(n => n.Checked);
            UpdateParentCheckState(node.Parent);
        }

        private List<string> GetExpandedNodes(TreeView treeView)
        {
            var expanded = new List<string>();
            foreach (TreeNode node in treeView.Nodes)
            {
                CollectExpandedNodes(node, expanded);
            }
            return expanded;
        }

        private void CollectExpandedNodes(TreeNode node, List<string> expanded)
        {
            if (node.IsExpanded && node.Tag is string tag)
            {
                expanded.Add(tag);
            }
            foreach (TreeNode child in node.Nodes)
            {
                CollectExpandedNodes(child, expanded);
            }
        }

        private void ExpandNodes(TreeView treeView, List<string> expandedPaths)
        {
            foreach (TreeNode node in treeView.Nodes)
            {
                ExpandMatchingNodes(node, expandedPaths);
            }
        }

        private void ExpandMatchingNodes(TreeNode node, List<string> expandedPaths)
        {
            if (node.Tag is string tag && expandedPaths.Contains(tag))
            {
                node.Expand();
            }
            foreach (TreeNode child in node.Nodes)
            {
                ExpandMatchingNodes(child, expandedPaths);
            }
        }

        private void SetCheckedNodes(TreeView treeView, List<string> fullPaths)
        {
            var normalizedPaths = fullPaths.Select(p => Path.GetFullPath(p).ToLowerInvariant()).ToHashSet();
            foreach (TreeNode node in treeView.Nodes)
            {
                SetCheckedNodesRecursive(node, normalizedPaths);
            }
        }

        private void SetCheckedNodesRecursive(TreeNode node, HashSet<string> normalizedPaths)
        {
            if (node.Tag is string tag && normalizedPaths.Contains(Path.GetFullPath(tag).ToLowerInvariant()))
            {
                node.Checked = true;
            }
            foreach (TreeNode child in node.Nodes)
            {
                SetCheckedNodesRecursive(child, normalizedPaths);
            }
        }

        private void btnBrowse3_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtFolder3.Text = dialog.SelectedPath;
                    saveFolderPath = dialog.SelectedPath;
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

            btnSame.Text = $"Same Items: 0";
            btnDiff.Text = $"Different Items: 0";
            btnMissingInCompare.Text = $"Missing in Compare: 0";
            btnMissingInPrimary.Text = $"Missing in Primary: 0";
        }

        private void RefreshSummaryUI()
        {
            lstDifferences.Items.Clear();
            btnSame.Text = $"Same Items: {sameItems.Count}";
            btnDiff.Text = $"Different Items: {diffItems.Count}";
            btnMissingInCompare.Text = $"Missing in Compare: {missingInCompare.Count}";
            btnMissingInPrimary.Text = $"Missing in Primary: {missingInPrimary.Count}";
            lstDifferences.Items.Add("Click any summary button to view file list.");
        }
        private CancellationTokenSource _checkOperationCts;

    }


    [Serializable]
    public class ComparisonResult
    {
        public List<string> ExpandedNodesPrimary { get; set; } = new List<string>();
        public List<string> ExpandedNodesCompare { get; set; } = new List<string>();
        public List<string> ExpandedNodesThird { get; set; } = new List<string>();

        public string FolderPath1 { get; set; }
        public string FolderPath2 { get; set; }
        public string FolderPath3 { get; set; }

        public List<string> CheckedFilesPrimary { get; set; } = new List<string>();
        public List<string> CheckedFilesCompare { get; set; } = new List<string>();
        public List<string> CheckedFilesThird { get; set; } = new List<string>();

        public List<string> SameItems { get; set; } = new List<string>();
        public List<string> DiffItems { get; set; } = new List<string>();
        public List<string> MissingInCompare { get; set; } = new List<string>();
        public List<string> MissingInPrimary { get; set; } = new List<string>();
    }

    public class ComparedFile
    {
        public string RelativePath { get; set; }
        public string PrimaryPath { get; set; }
        public string ComparePath { get; set; }
        public string Status { get; set; }

        public override bool Equals(object obj)
        {
            return obj is ComparedFile file &&
                   RelativePath == file.RelativePath;
        }

        public override int GetHashCode()
        {
            return RelativePath.GetHashCode();
        }
    }
}