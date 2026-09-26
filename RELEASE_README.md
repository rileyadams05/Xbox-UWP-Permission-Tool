# Xbox UWP Drive Permission Tool

A lightweight, standalone Windows utility designed to prepare external USB storage drives for use with Xbox Universal Windows Platform (UWP) homebrew applications and tools by applying the necessary `ALL APPLICATION PACKAGES` (`S-1-15-2-1`) access permissions.

---

## Original Author

Original tool created by **YTReviveMe**.

This release maintains the original core permission functionality and includes minor warning cleanup and usability improvements.

---

## Changes in this Version

* **Strict NTFS-Only Drive Detection:** The tool checks volume file systems and only lists compatible **NTFS** drives.
* **Non-NTFS Drives Excluded:** Drives formatted as **FAT32**, **exFAT**, or other unsupported file systems will not appear as selectable targets.
* **Disabled Action on Incompatible State:** When no compatible NTFS drive is detected, the `Auto Apply Permissions` button is automatically disabled to prevent accidental misconfiguration.
* **Refresh Support:** Includes a **Refresh** button so connected drives can be rescanned at any time without restarting the application.
* **Clean Drive Formatting:** Simplified dropdown display labels without confusing media-type tags.
* **Standalone Executable:** Fully standalone executable (`XB Drive Tool.exe`) with no extra DLLs or installers required.

---

## Requirements

* **Operating System:** Windows 10 or Windows 11 (64-bit / 32-bit)
* **Storage:** External USB storage drive formatted as **NTFS**
* **Privileges:** Administrator privileges (required by Windows to modify NTFS Access Control Lists)

---

## Why Must the Drive Be Formatted as NTFS?

Xbox UWP applications run inside an AppContainer security sandbox and require Windows NTFS Access Control Lists (ACLs) with the `ALL APPLICATION PACKAGES` identity. 

Non-NTFS filesystems (such as **FAT32** and **exFAT**) do not support Windows NTFS security descriptors. If your drive is formatted as FAT32 or exFAT, it **will not appear** in the tool. Please format your external drive to NTFS in Windows before using this utility.

---

## How to Use

1. **Connect your external drive** to your Windows PC.
2. **Ensure the drive is formatted as NTFS**.
3. **Launch `XB Drive Tool.exe`** as Administrator.
4. **Select your detected NTFS drive** from the dropdown menu.
5. Click **Auto Apply Permissions**.
6. Wait for the progress bar to complete and the success dialog to appear.
7. **Safely unplug the drive** from your PC and connect it to your Xbox console.
8. When prompted by the Xbox dashboard, select **Use for Media** (*do NOT select "Games & Apps"*).

---

## Screenshots

### Compatible NTFS Drive Detected
![Compatible NTFS drive detected](docs/images/Xbox%20drive%20tool.png)

### No Compatible NTFS Drive Detected
![No compatible NTFS drive detected](docs/images/No%20drives%20found.png)

---

## User Interface & Controls

* **Drive Dropdown:** Lists all detected external NTFS volumes with drive letter, label, filesystem, and size.
* **Refresh Button:** Rescans connected storage devices immediately without restarting the application.
* **Auto Apply Permissions:** Recursively applies the `ALL APPLICATION PACKAGES` FullControl permission rule to the drive root and all existing subdirectories with object/container inheritance.
* **Progress Bar & Status Label:** Displays live execution status and confirmation upon completion.

---

## Troubleshooting

| Issue | Cause | Solution |
|---|---|---|
| Drive not listed in dropdown | Drive is formatted as FAT32, exFAT, or unformatted | Format the drive as NTFS in Windows Explorer, then click **Refresh**. |
| "Access Denied" or permission error | Tool was run without Administrator rights | Right-click `XB Drive Tool.exe` and select **Run as administrator**. |
| Permission button is grayed out | No compatible NTFS drive detected | Connect an NTFS-formatted external drive and click **Refresh**. |

---

## Safety Notes

* **No Data Loss:** Applying permissions does **not** format the drive or delete any existing files or folders.
* **Single Purpose:** This tool only applies security descriptors. It does not alter partition tables, format drives, or install software.
