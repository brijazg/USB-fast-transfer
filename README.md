# USB Turbo Transfer

A Windows 10/11 desktop utility for copying selected files or folders to a removable USB drive. It uses asynchronous sequential file I/O with a 4 MiB buffer, displays transfer progress and average speed, and optionally verifies copied files with SHA-256.

## Features
- Select multiple files or a complete folder
- Detect removable USB drives
- Asynchronous sequential copying with a 4 MiB buffer
- Live progress and average transfer speed
- Pause, resume, and cancel
- Optional SHA-256 verification

## Requirements
- Windows 10/11, x64
- .NET 8 SDK to build locally
- Inno Setup 6 to create the installer locally

## Build locally
1. Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
2. Open PowerShell in this folder.
3. Run `publish.bat`.
4. Run `publish/USB Turbo Transfer.exe`.

To create a setup wizard locally, install [Inno Setup 6](https://jrsoftware.org/isdl.php), open `installer.iss`, and choose **Build > Compile**. The installer is written to `Output/USB-Turbo-Transfer-Setup.exe`.

## Download from GitHub Actions
The workflow in `.github/workflows/build-windows.yml` builds the Windows installer on every push to `main`. Open the repository's **Actions** tab, select the latest successful **Build Windows installer** run, and download the `USB-Turbo-Transfer-Setup` artifact.

To publish a GitHub Release containing the installer, push a version tag such as `v1.0.0`.

## Use
1. Connect the USB drive.
2. Launch USB Turbo Transfer.
3. Choose files or a folder.
4. Select the removable USB destination.
5. Optionally enable SHA-256 verification.
6. Start the transfer. Use Pause/Resume or Cancel as needed.

## Performance and data safety
- USB 3.0 describes the bus interface, not a guaranteed write speed. Actual throughput depends on the USB stick, port, cable, source drive, and filesystem.
- The 4 MiB buffer and asynchronous sequential I/O are intended to reduce overhead, but cannot exceed the hardware's real limits.
- SHA-256 verification reads the source and destination again and takes extra time.
- The application does not format, partition, or alter the USB filesystem.
- Files with matching destination names are overwritten after a confirmation prompt. Keep a backup of important data.

## License
MIT. See [LICENSE](LICENSE).
