# Xbox UWP Drive Permission Tool

Original tool created by **YTReviveMe**.

This tool prepares an external drive so it can be used correctly with Xbox UWP applications.

## Before You Start

Your drive must be formatted as **NTFS**.

If it is already NTFS, skip this step.

If it is not NTFS:

1. Open File Explorer.
2. Find the drive you want to use.
3. Back up anything important on the drive.
4. Format the drive as **NTFS**.

**Formatting a drive erases everything on it.**

## How to Use

1. Connect the drive to your PC.
2. Run `XB Drive Tool.exe`.
3. Select the correct drive from the dropdown.
4. Double-check the **drive letter**, **drive name**, and **drive size** to make sure you selected the right drive.
5. Click **Auto Apply Permissions**.
6. Wait for the tool to finish.
7. When it reports success, safely eject the drive and connect it to your Xbox.

## Important

Only **NTFS** drives are shown in the tool.

FAT32, exFAT, and other unsupported drives will not appear.

If you connect or format a drive while the tool is open, click **Refresh**.

## Screenshots

### Compatible NTFS Drive Detected

![Compatible NTFS Drive](docs/images/Xbox%20drive%20tool.png)

### No Compatible NTFS Drive Detected

![No Compatible NTFS Drive](docs/images/No%20drives%20found.png)

## Credits

Original tool created by **YTReviveMe**.

This version only includes minor cleanup and usability improvements.
