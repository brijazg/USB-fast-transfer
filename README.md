# USB Turbo Transfer

A Windows 10/11 desktop utility for copying selected files or folders to a removable USB drive. It uses asynchronous sequential file I/O with a 4 MiB buffer, displays transfer progress and average throughput, and can verify copied files with SHA-256.

## Features
- Detects connected removable drives and displays available space.
- Copies multiple selected files or an entire folder while preserving folder structure.
- Shows progress, bytes copied, and average transfer speed.
- Pause, resume, and cancel controls.
- Optional SHA-256 verification after copying.
- Self-contained Windows x64 build; the target PC does not need .NET installed.

## Requirements
- Windows 10 or Windows 11, 64-bit.
- .NET 8 SDK to build from source.
- Inno Setup 6 (optional, for creating the setup wizard).

## Build and run from source
1. Install the .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0
2. Open PowerShell in this folder.
3. Run:
   ```powershell
   dotnet restore
   dotnet run
   ```

## Build the Windows installer
Run `publish.bat` to publish a self-contained Windows x64 application into `publish\\`. To create the setup wizard, install Inno Setup 6, open `installer.iss`, and select **Build > Compile**. The installer is written to `Output\\USB-Turbo-Transfer-Setup.exe`.

## Use
1. Connect the USB drive.
2. Open USB Turbo Transfer.
3. Choose one or more files, or choose a folder.
4. Select the removable USB drive as the destination.
5. Optionally enable SHA-256 verification (this takes extra time).
6. Start the transfer. You can pause, resume, or cancel it.

## Performance and safety notes
- The app cannot exceed the real speed of the USB drive, USB port, cable, source disk, or filesystem.
- USB 3.x is a bus standard, not a guaranteed copy speed. A slow flash drive may still write slowly.
- The app uses a 4 MiB buffer and sequential asynchronous I/O. This may help with large files, but cannot make a slow USB device faster than its hardware.
- Verification performs additional reads of the source and destination and therefore takes longer.
- The app copies files and folders; it does not format, partition, or otherwise alter the USB filesystem.
- Files with matching destination names are overwritten after a confirmation prompt. Back up important data first.

## GitHub Actions
Every push to `main` builds the self-contained Windows x64 application and Inno Setup installer. Download the `USB-Turbo-Transfer-Setup` artifact from the successful workflow run. The workflow also publishes a GitHub Release with the installer when a version tag beginning with `v` is pushed.
