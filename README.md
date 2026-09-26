# Xbox UWP Drive Permission Tool (Source Repository)

A C# Windows Forms application that applies the Windows NTFS `ALL APPLICATION PACKAGES` (`S-1-15-2-1`) access control entry (ACE) with inheritance to external NTFS drives, enabling Xbox Universal Windows Platform (UWP) applications to read and write to the drive.

## Original Author & Credits

* **Original Creator:** **YTReviveMe**
* **Heritage:** This project is based on the original **Xbox UWP Permission Tool** created by **YTReviveMe**.
* The core Windows security descriptor / ACL application logic remains faithful to the original tool.

## Developer Changes & Improvements

This repository contains cleanup and usability enhancements over the original source:

1. **Strict NTFS-Only Drive Enumeration:**
   - Evaluates physical disks and logical partitions via WMI and `DriveInfo`.
   - Strictly verifies that the volume file system is formatted as `NTFS` before adding it to the list.
   - Non-NTFS drives (**FAT32**, **exFAT**, ReFS, RAW, optical, network drives) are completely excluded from the UI to prevent attempts to apply NTFS ACLs to unsupported filesystems.

2. **Empty / Incompatible State Handling:**
   - When no compatible NTFS drive is detected, the application displays `No compatible NTFS drives detected` and disables the `Auto Apply Permissions` button.
   - Pre-flight checks on execution verify the target drive is still connected and remains formatted as NTFS before executing worker threads.

3. **Rescan / Refresh Support:**
   - Includes a dedicated `Refresh` button to trigger re-enumeration without restarting the process.
   - Listens to `WM_DEVICECHANGE` window messages to automatically refresh the drive list when devices are connected or disconnected.

4. **Streamlined UI & Label Formatting:**
   - Removed confusing media-type tags (`(HDD)`, `(USB Stick)`).
   - Formats drive entries cleanly as: `DriveLetter - VolumeLabel - NTFS - Size` (or `DriveLetter - NTFS - Size`).

5. **Code & Compiler Cleanup:**
   - Resolved compiler warnings and unhandled exceptions.
   - Configured assembly metadata and Release build properties for single standalone binary deployment (`XB Drive Tool.exe`) without companion PDB or config dependencies.

## Project Structure

```
Xbox-UWP-Permission-Tool/
├── WindowsFormsApp1/
│   ├── Properties/
│   │   ├── AssemblyInfo.cs
│   │   ├── Resources.Designer.cs
│   │   ├── Resources.resx
│   │   ├── Settings.Designer.cs
│   │   └── Settings.settings
│   ├── App.config
│   ├── app.manifest
│   ├── Form1.cs
│   ├── Form1.Designer.cs
│   ├── ManagementObject.cs
│   ├── ManagementObjectSearcher.cs
│   ├── Program.cs
│   └── XboxDrivePermissionTool.csproj
├── docs/
│   └── images/
│       ├── Xbox drive tool.png
│       └── No drives found.png
├── .gitattributes
├── .gitignore
├── README.md
├── RELEASE_README.md
└── WindowsFormsApp1.sln
```

## Building from Source

### Prerequisites
* Visual Studio 2019 / 2022 (Community or higher) with **.NET desktop development** workload
* .NET Framework 4.7.2 targeting pack

### Command-Line Build (MSBuild)
```powershell
msbuild WindowsFormsApp1.sln /p:Configuration=Release /t:Clean,Build /v:minimal
```

The compiled standalone executable will be located in `WindowsFormsApp1\bin\Release\XB Drive Tool.exe`.

## User Documentation

For end-user instructions and usage details, refer to [RELEASE_README.md](RELEASE_README.md).
