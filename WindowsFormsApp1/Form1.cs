using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Management;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private const int WM_DEVICECHANGE = 0x0219;
        readonly BackgroundWorker worker = new BackgroundWorker();

        public class DriveItem
        {
            public string DriveLetter { get; set; } // e.g. "G:"
            public string RootPath => DriveLetter.EndsWith("\\") ? DriveLetter : DriveLetter + "\\";
            public string VolumeLabel { get; set; } // e.g. "Xbox"
            public string FileSystem { get; set; }  // e.g. "NTFS"
            public long TotalSizeBytes { get; set; }
            public string FormattedSize { get; set; }

            public override string ToString()
            {
                if (!string.IsNullOrWhiteSpace(VolumeLabel))
                    return $"{DriveLetter} - {VolumeLabel.Trim()} - {FileSystem} - {FormattedSize}";
                else
                    return $"{DriveLetter} - {FileSystem} - {FormattedSize}";
            }
        }

        public Form1()
        {
            InitializeComponent();

            // Dark mode
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ForeColor = Color.White;
            foreach (Control c in this.Controls)
            {
                if (c is Button || c is ComboBox || c is Label || c is ProgressBar)
                    c.BackColor = Color.FromArgb(45, 45, 45);
                if (c is Button || c is Label)
                    c.ForeColor = Color.White;
            }

            // ComboBox custom draw
            driveBox.ForeColor = Color.White;
            driveBox.DrawMode = DrawMode.OwnerDrawFixed;
            driveBox.DrawItem += DriveBox_DrawItem;

            // BackgroundWorker setup
            worker.WorkerReportsProgress = true;
            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_Completed;

            // Load drives
            LoadDrives();
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == WM_DEVICECHANGE && (worker == null || !worker.IsBusy))
            {
                LoadDrives();
            }
        }

        private void DriveBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= driveBox.Items.Count) return;
            e.DrawBackground();

            object item = driveBox.Items[e.Index];
            bool isPlaceholder = !(item is DriveItem);
            Color textColor = isPlaceholder ? Color.FromArgb(170, 170, 170) : Color.White;

            using (Brush b = new SolidBrush(textColor))
            {
                e.Graphics.DrawString(item.ToString(), e.Font, b, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            if (worker.IsBusy) return;
            LoadDrives();
        }

        public void LoadDrives()
        {
            driveBox.Items.Clear();
            var validDrives = new List<DriveItem>();
            var processedLetters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string systemRoot = null;
            try
            {
                systemRoot = Path.GetPathRoot(Environment.SystemDirectory);
            }
            catch { }

            try
            {
                // Step 1: Query USB & Removable / External physical drives via WMI
                using (var diskSearcher = new ManagementObjectSearcher(
                    "SELECT DeviceID, Model, Size, MediaType, InterfaceType FROM Win32_DiskDrive WHERE InterfaceType='USB' OR MediaType LIKE '%External%' OR MediaType LIKE '%Removable%'"))
                {
                    foreach (ManagementObject disk in diskSearcher.Get().Cast<ManagementObject>())
                    {
                        // Step 2: Find partitions for this physical disk
                        string diskId = disk["DeviceID"]?.ToString();
                        if (string.IsNullOrEmpty(diskId)) continue;

                        var partitionQuery = "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + diskId +
                                             "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition";

                        using (var partitionSearcher = new ManagementObjectSearcher(partitionQuery))
                        {
                            foreach (ManagementObject partition in partitionSearcher.Get().Cast<ManagementObject>())
                            {
                                string partId = partition["DeviceID"]?.ToString();
                                if (string.IsNullOrEmpty(partId)) continue;

                                // Step 3: Find logical disks for this partition
                                var logicalQuery = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partId +
                                                   "'} WHERE AssocClass = Win32_LogicalDiskToPartition";

                                using (var logicalSearcher = new ManagementObjectSearcher(logicalQuery))
                                {
                                    foreach (ManagementObject logical in logicalSearcher.Get().Cast<ManagementObject>())
                                    {
                                        string letter = logical["DeviceID"]?.ToString();
                                        if (string.IsNullOrEmpty(letter)) continue;

                                        // Skip system drive
                                        if (!string.IsNullOrEmpty(systemRoot) && letter.StartsWith(systemRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                                            continue;

                                        // Check file system - STRICT NTFS ONLY
                                        DriveItem driveItem = TryCreateNtfsDriveItem(letter, logical["VolumeName"]?.ToString());
                                        if (driveItem != null && processedLetters.Add(driveItem.DriveLetter))
                                        {
                                            validDrives.Add(driveItem);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("WMI Drive Search error: " + ex.Message);
            }

            // Step 4: Supplemental check using DriveInfo for any removable drives
            try
            {
                foreach (DriveInfo di in DriveInfo.GetDrives())
                {
                    if (di.DriveType == DriveType.Removable)
                    {
                        string letter = di.Name.TrimEnd('\\');
                        if (!string.IsNullOrEmpty(systemRoot) && letter.StartsWith(systemRoot.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (processedLetters.Contains(letter))
                            continue;

                        DriveItem driveItem = TryCreateNtfsDriveItem(letter, null);
                        if (driveItem != null && processedLetters.Add(driveItem.DriveLetter))
                        {
                            validDrives.Add(driveItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DriveInfo check error: " + ex.Message);
            }

            // Populate UI based on valid NTFS drive count
            if (validDrives.Count > 0)
            {
                foreach (var d in validDrives)
                {
                    driveBox.Items.Add(d);
                }
                driveBox.SelectedIndex = 0;
                driveBox.Enabled = true;
                applyButton.Enabled = true;
                statusLabel.Text = "Ready to apply permissions.";
            }
            else
            {
                driveBox.Items.Add("No compatible NTFS drives detected");
                driveBox.SelectedIndex = 0;
                driveBox.Enabled = false;
                applyButton.Enabled = false;
                statusLabel.Text = "No compatible NTFS drives detected. Connect an NTFS-formatted USB drive.";
            }
        }

        private static DriveItem TryCreateNtfsDriveItem(string letter, string fallbackLabel)
        {
            try
            {
                string root = letter.EndsWith("\\") ? letter : letter + "\\";
                DriveInfo di = new DriveInfo(root);

                // Drive must be ready and accessible
                if (!di.IsReady)
                    return null;

                // Drive must NOT be optical (CD/DVD), network, or RAM disk
                if (di.DriveType == DriveType.CDRom || di.DriveType == DriveType.Network || di.DriveType == DriveType.NoRootDirectory)
                    return null;

                // STRICT FILTER: Filesystem MUST be NTFS
                if (!string.Equals(di.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                    return null;

                string volumeLabel = !string.IsNullOrWhiteSpace(di.VolumeLabel)
                    ? di.VolumeLabel.Trim()
                    : (!string.IsNullOrWhiteSpace(fallbackLabel) ? fallbackLabel.Trim() : "");

                long totalSize = di.TotalSize;
                string formattedSize = FormatBytes(totalSize);

                return new DriveItem
                {
                    DriveLetter = letter.TrimEnd('\\'),
                    VolumeLabel = volumeLabel,
                    FileSystem = "NTFS",
                    TotalSizeBytes = totalSize,
                    FormattedSize = formattedSize
                };
            }
            catch
            {
                return null;
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 B";
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < suffixes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.#} {suffixes[order]}";
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            if (driveBox.SelectedItem == null || !(driveBox.SelectedItem is DriveItem selectedItem))
            {
                MessageBox.Show("Please select a valid NTFS drive.", "No Drive Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string root = selectedItem.RootPath;

            // Pre-flight check immediately before applying permissions
            try
            {
                DriveInfo drive = new DriveInfo(root);
                if (!drive.IsReady)
                {
                    MessageBox.Show($"The selected drive ({selectedItem.DriveLetter}) is not ready or has been disconnected.", "Drive Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LoadDrives();
                    return;
                }

                if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show($"The selected drive ({selectedItem.DriveLetter}) is formatted as {drive.DriveFormat}.\n\nOnly NTFS volumes are supported for Xbox UWP permissions.", "Unsupported File System", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LoadDrives();
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to access drive ({selectedItem.DriveLetter}): {ex.Message}", "Drive Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadDrives();
                return;
            }

            progressBar1.Value = 0;
            statusLabel.Text = "Starting...";
            applyButton.Enabled = false;
            refreshButton.Enabled = false;

            worker.RunWorkerAsync(root);
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string root = (string)e.Argument;

            DriveInfo drive = new DriveInfo(root);
            if (!drive.IsReady)
                throw new InvalidOperationException("Drive is not ready or has been disconnected.");
            if (!string.Equals(drive.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Drive filesystem is {drive.DriveFormat}. It must be formatted as NTFS to apply Xbox UWP permissions.");

            var sid = new SecurityIdentifier("S-1-15-2-1");
            var rule = new FileSystemAccessRule(
                sid,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            ApplyPermission(root, rule);
            ProcessDirectory(root, rule);
        }

        void ProcessDirectory(string path, FileSystemAccessRule rule)
        {
            try
            {
                foreach (var dir in Directory.GetDirectories(path))
                {
                    ApplyPermission(dir, rule);
                    ProcessDirectory(dir, rule);
                }
            }
            catch { }
        }

        void ApplyPermission(string path, FileSystemAccessRule rule)
        {
            try
            {
                DirectoryInfo d = new DirectoryInfo(path);
                var sec = d.GetAccessControl();
                sec.AddAccessRule(rule);
                d.SetAccessControl(sec);
            }
            catch { }
        }

        void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
            statusLabel.Text = "Applying permissions...";
        }

        void Worker_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            applyButton.Enabled = driveBox.SelectedItem is DriveItem;
            refreshButton.Enabled = true;

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                statusLabel.Text = "Failed";
                return;
            }

            progressBar1.Value = 100;
            statusLabel.Text = "Completed";

            MessageBox.Show(
@"Permissions applied successfully!

Once you plug in the newly formatted drive into Your Xbox,
You NEED to set it up as MEDIA!
(DO NOT CHOOSE GAMES AND APPS). 

HAPPY GAMING! - ReviveMe",
                "Done",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
    }
}
