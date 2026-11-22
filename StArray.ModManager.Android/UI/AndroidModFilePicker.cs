using System.Numerics;
using ImGuiNET;
using StArray.ModManager.Manager;
using StArray.ModManager.Resources;

namespace StArray.ModManager.Android.UI;

/// <summary>Browses local files with the host application's existing storage access.</summary>
internal sealed class AndroidModFilePicker
{
    private sealed record DirectoryListing(string[] Folders, string[] Files);

    private readonly string _imports;
    private readonly string _downloads;
    private TaskCompletionSource<string?>? _selection;
    private Task<DirectoryListing>? _listing;
    private string _directory;
    private string? _selectedFile;
    private bool _openPopup;
    private bool _resetListScroll;

    public AndroidModFilePicker(string modsPath)
    {
        string mods = Path.GetFullPath(modsPath);
        _imports = Path.Combine(Path.GetDirectoryName(mods) ?? mods, "imports");
        string? storage = Environment.GetEnvironmentVariable("EXTERNAL_STORAGE");
        _downloads = Path.Combine(!string.IsNullOrEmpty(storage) && Path.IsPathFullyQualified(storage)
            ? storage
            : "/sdcard", "Download");
        _directory = _imports;
    }

    public async Task<ModInstallFile?> PickAsync()
    {
        if (_selection != null)
            throw new InvalidOperationException(L10n.Get("ModInstall_InProgress"));

        var selection = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _selection = selection;
        _openPopup = true;
        Navigate(_directory);

        string? path = await selection.Task.ConfigureAwait(false);
        return path == null ? null : new ModInstallFile(Path.GetFileName(path), File.OpenRead(path));
    }

    public void Render()
    {
        if (_selection == null)
            return;

        string title = L10n.Get("ModInstall_PickerTitle") + "###AndroidModFilePicker";
        if (_openPopup)
        {
            _openPopup = false;
            ImGui.OpenPopup(title);
        }

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f));
        ImGui.SetNextWindowSize(viewport.Size * 0.9f, ImGuiCond.Appearing);
        bool open = true;
        if (ImGui.BeginPopupModal(title, ref open))
        {
            RenderDirectory();
            ImGui.BeginDisabled(_selectedFile == null);
            if (ImGui.Button(L10n.Get("ModInstall_Confirm")))
            {
                Finish(_selectedFile);
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndDisabled();

            ImGui.SameLine();
            if (ImGui.Button(L10n.Get("ModInstall_Cancel")))
            {
                Finish(null);
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }

        if (!open || !ImGui.IsPopupOpen(title))
            Finish(null);
    }

    private void RenderDirectory()
    {
        if (ImGui.Button(L10n.Get("ModInstall_Downloads")))
            Navigate(_downloads);
        ImGui.SameLine();
        if (ImGui.Button(L10n.Get("ModInstall_ImportFolder")))
            Navigate(_imports);

        string? parent = Path.GetDirectoryName(_directory);
        ImGui.BeginDisabled(parent == null);
        if (ImGui.Button(L10n.Get("ModInstall_ParentFolder")) && parent != null)
            Navigate(parent);
        ImGui.EndDisabled();

        ImGui.SameLine();
        if (ImGui.Button(L10n.Get("ModInstall_Refresh")))
            Navigate(_directory);

        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(_directory);
        ImGui.TextUnformatted(L10n.Get("ModInstall_ImportHint", _imports));
        ImGui.PopTextWrapPos();

        float availableHeight = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeightWithSpacing();
        if (ImGui.BeginChild("ModFiles", new Vector2(0, Math.Max(1, availableHeight)), ImGuiChildFlags.None))
            RenderListing();
        ImGui.EndChild();
    }

    private void RenderListing()
    {
        if (_listing is not { IsCompleted: true } listing)
        {
            ImGui.TextUnformatted(L10n.Get("ModInstall_ReadingFolder"));
            return;
        }

        if (!listing.IsCompletedSuccessfully)
        {
            ImGui.PushTextWrapPos(0);
            string detail = listing.Exception?.GetBaseException().Message ?? _directory;
            ImGui.TextUnformatted(L10n.Get("ModInstall_FolderReadError", detail));
            ImGui.PopTextWrapPos();
            return;
        }

        if (_resetListScroll)
        {
            ImGui.SetScrollY(0);
            _resetListScroll = false;
        }

        DirectoryListing entries = listing.Result;
        if (entries.Folders.Length == 0 && entries.Files.Length == 0)
            ImGui.TextUnformatted(L10n.Get("ModInstall_EmptyFolder"));
        foreach (string folder in entries.Folders)
        {
            ImGui.PushID(folder);
            bool entered = ImGui.Selectable(Path.GetFileName(folder) + "/");
            ImGui.PopID();
            if (entered)
            {
                Navigate(folder);
                return;
            }
        }

        foreach (string file in entries.Files)
        {
            ImGui.PushID(file);
            if (ImGui.Selectable(Path.GetFileName(file), _selectedFile == file))
                _selectedFile = file;
            ImGui.PopID();
        }
    }

    private void Navigate(string directory)
    {
        _directory = directory;
        _selectedFile = null;
        _resetListScroll = true;
        _listing = Task.Run(() => ReadDirectory(directory, directory == _imports));
    }

    private static DirectoryListing ReadDirectory(string directory, bool create)
    {
        if (create)
            Directory.CreateDirectory(directory);

        return new DirectoryListing(
            Directory.GetDirectories(directory).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            Directory.EnumerateFiles(directory)
                .Where(path => Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase)
                    || Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                .Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private void Finish(string? path)
    {
        _selection?.TrySetResult(path);
        _selection = null;
        _listing = null;
        _selectedFile = null;
    }
}
