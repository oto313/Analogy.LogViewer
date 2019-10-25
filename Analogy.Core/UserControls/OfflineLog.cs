﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Analogy.Interfaces;
using DevExpress.XtraBars;

namespace Analogy
{

    public partial class OfflineUCLogs : UserControl
    {
        private UserSettingsManager Settings { get; } = UserSettingsManager.UserSettings;
        private List<string> extrenalFiles = new List<string>();
        public string SelectedPath { get; set; }
        private IAnalogyOfflineDataProvider DataProvider { get; }
        public OfflineUCLogs(string initSelectedPath)
        {
            SelectedPath = initSelectedPath;
            InitializeComponent();
        }

        public OfflineUCLogs(IAnalogyOfflineDataProvider dataProvider, string[] fileNames = null, string initialSelectedPath = null) : this(initialSelectedPath)
        {

            DataProvider = dataProvider;
            if (fileNames != null)
                extrenalFiles.AddRange(fileNames);
            ucLogs1.OnlineMode = false;
            ucLogs1.SetFileDataSource(dataProvider);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            ucLogs1.ProcessCmdKeyFromParent(keyData);
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private async void OfflineUCLogs_Load(object sender, EventArgs e)
        {
            if (DesignMode) return;
            folderTreeViewUC1.FolderChanged += FolderTreeViewUC1_FolderChanged;
            spltMain.Panel1Collapsed = false;
            ucLogs1.btswitchRefreshLog.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            ucLogs1.btsAutoScrollToBottom.Visibility = DevExpress.XtraBars.BarItemVisibility.Never;
            if (extrenalFiles.Any())
            {
                if (File.Exists(extrenalFiles.First()))
                    SelectedPath = Path.GetDirectoryName(extrenalFiles.First());
            }

            folderTreeViewUC1.SetFolder(SelectedPath, DataProvider);
            PopulateFiles(SelectedPath);

            if (extrenalFiles.Any())
            {
                await LoadFilesAsync(extrenalFiles, false);
            }
        }

        private async void FolderTreeViewUC1_FolderChanged(object sender, Types.FolderSelectionEventArgs e)
        {
            if (Directory.Exists(e.SelectedFolderPath))
            {
                PopulateFiles(e.SelectedFolderPath);
            }
        }

        private void AnalogyUCLogs_DragEnter(object sender, DragEventArgs e) =>
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        private async void AnalogyUCLogs_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            await LoadFilesAsync(files.ToList(), chkbSelectionMode.Checked);
        }

        private void PopulateFiles(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;
            SelectedPath = folder;
            treeList1.SelectionChanged -= TreeList1_SelectionChanged;
            bool recursiveLoad = checkEditRecursiveLoad.Checked;
            DirectoryInfo dirInfo = new DirectoryInfo(folder);
            List<FileInfo> fileInfos = DataProvider.GetSupportedFiles(dirInfo, recursiveLoad).ToList();
            treeList1.Nodes.Clear();
            foreach (FileInfo fi in fileInfos)
            {
                treeList1.Nodes.Add(fi.Name, fi.LastWriteTime, fi.Length, fi.FullName);
            }

            treeList1.BestFitColumns();
            treeList1.ClearSelection();
            treeList1.SelectionChanged += TreeList1_SelectionChanged;
        }

        private async Task LoadFilesAsync(List<string> fileNames, bool clearLog)
        {
            await ucLogs1.LoadFilesAsync(fileNames, clearLog);

        }

        private void bBtnOpen_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (treeList1.Selection.Any())
            {
                var filename = (string)treeList1.Selection.First().GetValue(colFullPath);
                if (filename == null || !File.Exists(filename)) return;
                Process.Start("explorer.exe", "/select, \"" + filename + "\"");
            }
        }

        private void bBtnDelete_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (treeList1.Selection.Any())
            {
                var filename = (string)treeList1.Selection.First().GetValue(colFullPath);
                if (filename == null || !File.Exists(filename)) return;
                var result = MessageBox.Show($"Are you sure you want to delete {filename}?", "Delete confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    if (File.Exists(filename))
                        try
                        {
                            File.Delete(filename);
                            PopulateFiles(SelectedPath);
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message, @"Error deleting file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                }
            }

        }

        private void bBtnRefresh_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            PopulateFiles(SelectedPath);
        }

        private void bBtnSelectAll_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            treeList1.SelectAll();
        }

        private async void TreeList1_SelectionChanged(object sender, EventArgs e)
        {
            List<string> files = treeList1.Selection.Select(node => (string)node.GetValue(colFullPath)).ToList();
            await LoadFilesAsync(files, chkbSelectionMode.Checked);
        }
    }

}


