using System.Diagnostics;
using System.Security.Cryptography;

namespace USBTurboTransfer;

public sealed class MainForm : Form
{
    private readonly TextBox sourceBox = new() { ReadOnly = true, Width = 430 };
    private readonly Button fileButton = new() { Text = "Choose files…" };
    private readonly Button folderButton = new() { Text = "Choose folder…" };
    private readonly ComboBox driveBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
    private readonly CheckBox verifyBox = new() { Text = "Verify copied files (slower)", Checked = false, AutoSize = true };
    private readonly ProgressBar progress = new() { Width = 590, Height = 22, Minimum = 0, Maximum = 1000 };
    private readonly Label status = new() { AutoSize = true, Text = "Ready." };
    private readonly Label speed = new() { AutoSize = true, Text = "0 MB/s" };
    private readonly Button startButton = new() { Text = "Start transfer", Width = 130, Height = 36 };
    private readonly Button pauseButton = new() { Text = "Pause", Width = 100, Height = 36, Enabled = false };
    private readonly Button cancelButton = new() { Text = "Cancel", Width = 100, Height = 36, Enabled = false };
    private readonly System.Windows.Forms.Timer driveTimer = new() { Interval = 3000 };
    private readonly List<string> selectedFiles = new();
    private string? selectedFolder;
    private CancellationTokenSource? cts;
    private TaskCompletionSource<bool>? resumeSignal;
    private bool paused;
    private bool running;
    private readonly Stopwatch clock = new();
    private long totalBytes;
    private long copiedBytes;

    public MainForm()
    {
        Text = "USB Turbo Transfer";
        Width = 900;
        Height = 560;
        MinimumSize = new Size(820, 520);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 9.5f);
        BackColor = Color.FromArgb(246, 248, 251);

        var heading = new Label { Text = "USB TURBO TRANSFER", Font = new Font("Segoe UI", 19, FontStyle.Bold), AutoSize = true, ForeColor = Color.FromArgb(25, 48, 78) };
        var subtitle = new Label { Text = "Fast, monitored file copying for USB drives", AutoSize = true, ForeColor = Color.DimGray };
        var sourceLabel = new Label { Text = "Source", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
        var driveLabel = new Label { Text = "USB destination", AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
        var sourceRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        sourceRow.Controls.Add(sourceBox); sourceRow.Controls.Add(fileButton); sourceRow.Controls.Add(folderButton);
        var driveRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = false };
        driveRow.Controls.Add(driveBox);
        var actionRow = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true };
        actionRow.Controls.Add(startButton); actionRow.Controls.Add(pauseButton); actionRow.Controls.Add(cancelButton);
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28), ColumnCount = 1, RowCount = 12, AutoScroll = true };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.Controls.Add(heading); panel.Controls.Add(subtitle);
        panel.Controls.Add(new Label { Height = 8 }); panel.Controls.Add(sourceLabel); panel.Controls.Add(sourceRow);
        panel.Controls.Add(driveLabel); panel.Controls.Add(driveRow); panel.Controls.Add(verifyBox);
        panel.Controls.Add(progress); panel.Controls.Add(status); panel.Controls.Add(speed); panel.Controls.Add(actionRow);
        Controls.Add(panel);

        fileButton.Click += (_, _) => ChooseFiles();
        folderButton.Click += (_, _) => ChooseFolder();
        startButton.Click += async (_, _) => await StartTransferAsync();
        pauseButton.Click += (_, _) => TogglePause();
        cancelButton.Click += (_, _) => cts?.Cancel();
        driveTimer.Tick += (_, _) => RefreshDrives();
        RefreshDrives();
        driveTimer.Start();
    }

    private void ChooseFiles()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = "Select files to copy" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        selectedFiles.Clear(); selectedFiles.AddRange(dialog.FileNames);
        selectedFolder = null;
        sourceBox.Text = selectedFiles.Count == 1 ? selectedFiles[0] : $"{selectedFiles.Count} files selected";
    }

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select a folder to copy" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        selectedFolder = dialog.SelectedPath;
        selectedFiles.Clear();
        sourceBox.Text = selectedFolder;
    }

    private void RefreshDrives()
    {
        if (running) return;
        var previous = (driveBox.SelectedItem as DriveChoice)?.Root;
        var drives = DriveInfo.GetDrives()
            .Where(d => d.IsReady && d.DriveType == DriveType.Removable)
            .Select(d => new DriveChoice(d.Name, $"{d.Name}  ({FormatBytes(SafeFree(d))} free)"))
            .ToList();
        driveBox.Items.Clear();
        foreach (var d in drives) driveBox.Items.Add(d);
        driveBox.DisplayMember = nameof(DriveChoice.Display);
        var match = drives.FirstOrDefault(d => d.Root == previous);
        if (match != null) driveBox.SelectedItem = match;
        else if (driveBox.Items.Count > 0) driveBox.SelectedIndex = 0;
    }

    private static long SafeFree(DriveInfo d) { try { return d.AvailableFreeSpace; } catch { return 0; } }

    private async Task StartTransferAsync()
    {
        if (running) return;
        if (selectedFiles.Count == 0 && string.IsNullOrWhiteSpace(selectedFolder))
        { MessageBox.Show(this, "Choose one or more files, or a folder first."); return; }
        if (driveBox.SelectedItem is not DriveChoice drive)
        { MessageBox.Show(this, "Connect a USB drive and select it as the destination."); return; }

        var sourceRoot = selectedFolder;
        List<(string Source, string Relative)> items;
        try
        {
            if (sourceRoot != null)
            {
                items = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories)
                    .Select(f => (f, Path.GetRelativePath(sourceRoot, f))).ToList();
            }
            else
            {
                items = selectedFiles.Select(f => (f, Path.GetFileName(f))).ToList();
            }
            if (items.Count == 0) { MessageBox.Show(this, "The selected source contains no files."); return; }
            totalBytes = items.Sum(x => new FileInfo(x.Source).Length);
        }
        catch (Exception ex) { MessageBox.Show(this, $"Could not read the source: {ex.Message}"); return; }

        var targetRoot = Path.GetFullPath(drive.Root);
        if (sourceRoot != null)
        {
            var sourceFull = Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (targetRoot.StartsWith(sourceFull, StringComparison.OrdinalIgnoreCase))
            { MessageBox.Show(this, "Choose a destination outside the selected source folder."); return; }
        }
        if (items.Any(x => string.Equals(Path.GetFullPath(x.Source), Path.GetFullPath(Path.Combine(targetRoot, x.Relative)), StringComparison.OrdinalIgnoreCase)))
        { MessageBox.Show(this, "Source and destination resolve to the same file. Choose another USB drive."); return; }

        if (SafeFree(new DriveInfo(targetRoot)) < totalBytes)
        { MessageBox.Show(this, "The USB drive may not have enough free space."); return; }

        if (MessageBox.Show(this, $"Copy {items.Count} file(s) ({FormatBytes(totalBytes)}) to {targetRoot}?\nFiles with the same names will be overwritten.", "Confirm transfer", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        running = true; paused = false; copiedBytes = 0; progress.Value = 0;
        cts = new CancellationTokenSource();
        SetRunningUi(true);
        clock.Restart();
        try
        {
            const int bufferSize = 4 * 1024 * 1024;
            var buffer = new byte[bufferSize];
            foreach (var item in items)
            {
                cts.Token.ThrowIfCancellationRequested();
                await WaitIfPausedAsync(cts.Token);
                var relativePath = sourceRoot != null ? Path.Combine(Path.GetFileName(sourceRoot), item.Relative) : item.Relative;
                var destination = Path.GetFullPath(Path.Combine(targetRoot, relativePath));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                await using var input = new FileStream(item.Source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
                await using var output = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
                int read;
                while ((read = await input.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token)) > 0)
                {
                    await WaitIfPausedAsync(cts.Token);
                    await output.WriteAsync(buffer.AsMemory(0, read), cts.Token);
                    copiedBytes += read;
                    UpdateProgress();
                }
                await output.FlushAsync(cts.Token);
                if (verifyBox.Checked)
                {
                    status.Text = $"Verifying {Path.GetFileName(item.Source)}…";
                    var sourceHash = await HashFileAsync(item.Source, cts.Token);
                    var targetHash = await HashFileAsync(destination, cts.Token);
                    if (!CryptographicOperations.FixedTimeEquals(sourceHash, targetHash))
                        throw new IOException($"Verification failed for {Path.GetFileName(item.Source)}.");
                }
            }
            progress.Value = 1000;
            status.Text = $"Completed: {items.Count} file(s) copied.";
        }
        catch (OperationCanceledException) { status.Text = "Transfer cancelled."; }
        catch (Exception ex) { status.Text = "Transfer failed."; MessageBox.Show(this, ex.Message, "Transfer error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally
        {
            clock.Stop(); running = false; paused = false;
            cts?.Dispose(); cts = null; resumeSignal = null;
            SetRunningUi(false); RefreshDrives();
        }
    }

    private async Task WaitIfPausedAsync(CancellationToken token)
    {
        while (paused)
        {
            token.ThrowIfCancellationRequested();
            var signal = resumeSignal?.Task;
            if (signal != null) await signal.WaitAsync(token);
        }
    }

    private static async Task<byte[]> HashFileAsync(string path, CancellationToken token)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.Asynchronous | FileOptions.SequentialScan);
        using var sha = SHA256.Create();
        return await sha.ComputeHashAsync(stream, token);
    }

    private void TogglePause()
    {
        if (!running) return;
        if (!paused)
        {
            paused = true;
            resumeSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            pauseButton.Text = "Resume";
            status.Text = "Paused.";
        }
        else
        {
            paused = false;
            resumeSignal?.TrySetResult(true);
            pauseButton.Text = "Pause";
            status.Text = "Transferring…";
        }
    }

    private void UpdateProgress()
    {
        var percent = totalBytes == 0 ? 0 : (int)Math.Clamp(copiedBytes * 1000L / totalBytes, 0, 1000);
        if (InvokeRequired) { BeginInvoke(new Action(() => UpdateProgressUi(percent))); return; }
        UpdateProgressUi(percent);
    }

    private void UpdateProgressUi(int percent)
    {
        progress.Value = percent;
        var seconds = Math.Max(clock.Elapsed.TotalSeconds, 0.1);
        var mbps = copiedBytes / 1024d / 1024d / seconds;
        speed.Text = $"{mbps:0.0} MB/s  •  {FormatBytes(copiedBytes)} / {FormatBytes(totalBytes)}";
        status.Text = paused ? "Paused." : "Transferring…";
    }

    private void SetRunningUi(bool value)
    {
        fileButton.Enabled = folderButton.Enabled = driveBox.Enabled = !value;
        verifyBox.Enabled = !value;
        startButton.Enabled = !value;
        pauseButton.Enabled = cancelButton.Enabled = value;
        pauseButton.Text = "Pause";
        if (value) status.Text = "Transferring…";
    }

    private static string FormatBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double n = bytes; int i = 0;
        while (n >= 1024 && i < units.Length - 1) { n /= 1024; i++; }
        return $"{n:0.##} {units[i]}";
    }

    private sealed record DriveChoice(string Root, string Display)
    {
        public override string ToString() => Display;
    }
}
