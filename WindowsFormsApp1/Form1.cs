using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Management;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        BackgroundWorker worker = new BackgroundWorker();

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

            // ComboBox white text
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

        private void DriveBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();
            using (Brush b = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(driveBox.Items[e.Index].ToString(), e.Font, b, e.Bounds);
            }
            e.DrawFocusRectangle();
        }

        void LoadDrives()
        {
            driveBox.Items.Clear();

            try
            {
                // Step 1: Get all USB physical drives
                ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher(
                    "SELECT DeviceID, Model, Size, MediaType FROM Win32_DiskDrive WHERE InterfaceType='USB'"
                );

                foreach (ManagementObject disk in diskSearcher.Get())
                {
                    // Determine if it's a HDD or USB stick
                    string typeLabel = "USB Stick"; // default
                    try
                    {
                        if (disk["MediaType"] != null && disk["MediaType"].ToString().ToLower().Contains("fixed"))
                            typeLabel = "HDD";
                        else if (disk["Size"] != null)
                        {
                            long size = Convert.ToInt64(disk["Size"]);
                            if (size > 64L * 1024 * 1024 * 1024) // >64GB
                                typeLabel = "HDD";
                        }
                    }
                    catch { }

                    // Step 2: Find partitions
                    var partitionQuery = "ASSOCIATORS OF {Win32_DiskDrive.DeviceID='" + disk["DeviceID"] +
                                         "'} WHERE AssocClass = Win32_DiskDriveToDiskPartition";
                    ManagementObjectSearcher partitionSearcher = new ManagementObjectSearcher(partitionQuery);

                    foreach (ManagementObject partition in partitionSearcher.Get())
                    {
                        // Step 3: Find logical drives
                        var logicalQuery = "ASSOCIATORS OF {Win32_DiskPartition.DeviceID='" + partition["DeviceID"] +
                                           "'} WHERE AssocClass = Win32_LogicalDiskToPartition";
                        ManagementObjectSearcher logicalSearcher = new ManagementObjectSearcher(logicalQuery);

                        foreach (ManagementObject logical in logicalSearcher.Get())
                        {
                            string letter = (string)logical["DeviceID"];
                            string label = (string)logical["VolumeName"];
                            string fs = (string)logical["FileSystem"];

                            if (string.IsNullOrEmpty(label))
                                label = "Removable Drive";

                            string displayName = $"{letter} - {label} ({typeLabel})";
                            if (string.IsNullOrEmpty(fs))
                                displayName += " (Not ready)";

                            driveBox.Items.Add(displayName);
                        }
                    }
                }
            }
            catch
            {
                statusLabel.Text = "Failed to enumerate drives.";
            }

            if (driveBox.Items.Count > 0)
                driveBox.SelectedIndex = 0;
            else
                statusLabel.Text = "No removable drives detected";
        }

        private void applyButton_Click(object sender, EventArgs e)
        {
            if (driveBox.SelectedItem == null) return;

            string selectedDrive = driveBox.SelectedItem.ToString().Split(' ')[0];

            progressBar1.Value = 0;
            statusLabel.Text = "Starting...";
            applyButton.Enabled = false;

            worker.RunWorkerAsync(selectedDrive);
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            string root = (string)e.Argument;

            DriveInfo drive = new DriveInfo(root);
            if (!drive.IsReady)
                throw new Exception("Drive is not ready.");
            if (drive.DriveFormat != "NTFS")
                throw new Exception("Drive must be NTFS. (Please format drive to NTFS first)");

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
            applyButton.Enabled = true;

            if (e.Error != null)
            {
                MessageBox.Show(e.Error.Message, "Error");
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
