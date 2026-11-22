using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StArray.ModManager.Manager;
using StArray.ModManager.Resources;

namespace StArray.ModManager.Runtime;

/// <summary>Applies queued replacements before any installed mods have been loaded.</summary>
internal static class ModUpdateQueue
{
    private const string ManifestName = "update.json";
    private static readonly EnumerationOptions AllEntries = new() { AttributesToSkip = 0, IgnoreInaccessible = false };

    private sealed record Update(string ModId, string OldFolder, string OldEntry, string NewEntry,
        string OldEntryHash, string NewEntryHash);

    private static StringComparison PathComparison =>
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static string QueueDirectory(string mods) => Path.Combine(mods, ".starray-install", "updates");

    public static void Schedule(string modsDirectory, ModInstallPackage package, ModEntry installed, ModEntry replacement)
    {
        string mods = Path.TrimEndingDirectorySeparator(Path.GetFullPath(modsDirectory));
        string installedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(installed.FolderPath));
        if (installed.EntryPoint == null || replacement.EntryPoint == null
            || !string.Equals(Path.GetDirectoryName(installedPath), mods, PathComparison)
            || !Directory.Exists(installedPath))
            throw new IOException(L10n.Get("ModInstall_UpdateTargetInvalid", installed.Id));

        string oldEntry = Path.GetFileName(installed.EntryPoint);
        string newEntry = Path.GetFileName(replacement.EntryPoint);
        if (!IsDllName(oldEntry) || !IsDllName(newEntry))
            throw new InvalidDataException(L10n.Get("ModInstall_NoEntry"));

        var update = new Update(installed.Id, Path.GetFileName(installedPath), oldEntry, newEntry,
            HashFile(Path.Combine(installedPath, oldEntry)), HashFile(Path.Combine(package.ModDirectory, newEntry)));
        string newFolder = GetDestinationName(update);
        EnsureDestinationAvailable(mods, installedPath, newFolder);

        string queue = QueueDirectory(mods);
        Directory.CreateDirectory(queue);
        string requestId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(update.ModId)));
        string request = Path.Combine(queue, requestId);
        if (Directory.Exists(request))
            throw new InvalidOperationException(L10n.Get("ModInstall_UpdateAlreadyQueued", installed.Id));

        File.WriteAllText(Path.Combine(package.StagingDirectory, ManifestName), JsonSerializer.Serialize(update));
        Directory.Move(package.StagingDirectory, request);
    }

    public static void Apply(string modsDirectory)
    {
        string mods = Path.TrimEndingDirectorySeparator(Path.GetFullPath(modsDirectory));
        string queue = QueueDirectory(mods);
        string[] requests;
        try
        {
            if (!Directory.Exists(queue))
                return;
            requests = Directory.GetDirectories(queue);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Logger.Error(nameof(ModUpdateQueue), exception.Message);
            return;
        }

        foreach (string request in requests)
        {
            try
            {
                ApplyOne(mods, request);
            }
            catch (Exception exception)
            {
                Logger.Error(nameof(ModUpdateQueue), L10n.Get("ModInstall_Failed", exception.Message));
            }
        }
    }

    private static void ApplyOne(string mods, string request)
    {
        var update = JsonSerializer.Deserialize<Update>(File.ReadAllText(Path.Combine(request, ManifestName)))
            ?? throw new InvalidDataException(L10n.Get("ModInstall_NoEntry"));
        string newFolder = GetDestinationName(update);

        string oldPath = Path.Combine(mods, update.OldFolder);
        string newPath = Path.Combine(mods, newFolder);
        string staged = Path.Combine(request, "content");
        string ready = Path.Combine(request, "ready");
        string backup = Path.Combine(request, "backup");
        bool backedUp = Directory.Exists(backup);

        // Moving ready into place completes the update. Also recognize an interrupted
        // cleanup from older requests, where content was moved into place directly.
        if (backedUp && !Directory.Exists(ready) && Directory.Exists(newPath))
        {
            VerifyEntry(newPath, update.NewEntry, update.NewEntryHash, update.ModId);
            ModInstallPackage.DeleteStagingDirectory(request);
            return;
        }

        try
        {
            string previous = backedUp ? backup : oldPath;
            VerifyEntry(previous, update.OldEntry, update.OldEntryHash, update.ModId);
            VerifyEntry(staged, update.NewEntry, update.NewEntryHash, update.ModId);
            EnsureDestinationAvailable(mods, backedUp ? null : oldPath, newFolder);

            if (!backedUp || !Directory.Exists(ready))
                PrepareMergedDirectory(previous, staged, request, ready);
            VerifyEntry(ready, update.NewEntry, update.NewEntryHash, update.ModId);

            if (!backedUp)
                Directory.Move(oldPath, backup);
            Directory.Move(ready, newPath);
        }
        catch
        {
            if (Directory.Exists(backup))
            {
                try
                {
                    Directory.Move(backup, oldPath);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Logger.Error(nameof(ModUpdateQueue), L10n.Get("ModInstall_UpdateBackup", backup, exception.Message));
                }
            }

            // Release the queue slot after a successful rollback so another package can be tried.
            if (Directory.Exists(oldPath) && !Directory.Exists(backup))
                ModInstallPackage.DeleteStagingDirectory(request);
            throw;
        }

        ModInstallPackage.DeleteStagingDirectory(request);
        Logger.Info(nameof(ModUpdateQueue), L10n.Get("ModInstall_UpdateApplied", update.ModId));
    }

    private static void PrepareMergedDirectory(string previous, string package, string request, string ready)
    {
        string working = Path.Combine(request, "merge");
        if (Directory.Exists(working))
            Directory.Delete(working, recursive: true);
        if (Directory.Exists(ready))
            Directory.Delete(ready, recursive: true);

        Directory.CreateDirectory(working);
        CopyMissingEntries(new DirectoryInfo(package), working, working);
        CopyMissingEntries(new DirectoryInfo(previous), working, working);
        Directory.Move(working, ready);
    }

    private static void CopyMissingEntries(DirectoryInfo source, string destination, string root)
    {
        foreach (FileSystemInfo entry in source.EnumerateFileSystemInfos("*", AllEntries))
        {
            string target = Path.Combine(destination, entry.Name);
            FileAttributes attributes = entry.Attributes;
            bool isDirectory = IsRegularDirectory(attributes);
            FileAttributes? targetAttributes = GetAttributesIfPresent(target);

            if (targetAttributes is { } existing)
            {
                // Replacing a directory with a file would discard unrelated children.
                if (IsRegularDirectory(existing) != isDirectory)
                    throw new IOException(L10n.Get("ModInstall_UpdatePathConflict", Path.GetRelativePath(root, target)));
                if (!isDirectory)
                    continue;
            }

            if (isDirectory)
            {
                Directory.CreateDirectory(target);
                CopyMissingEntries((DirectoryInfo)entry, target, root);
            }
            else if ((attributes & FileAttributes.ReparsePoint) == 0)
            {
                File.Copy(entry.FullName, target);
            }
            else if (entry.LinkTarget is { } link)
            {
                // Preserve links themselves without traversing their targets.
                if ((attributes & FileAttributes.Directory) != 0)
                    Directory.CreateSymbolicLink(target, link);
                else
                    File.CreateSymbolicLink(target, link);
            }
            else
            {
                throw new IOException(L10n.Get("ModInstall_UpdatePathConflict", Path.GetRelativePath(root, target)));
            }
        }
    }

    private static bool IsRegularDirectory(FileAttributes attributes) =>
        (attributes & (FileAttributes.Directory | FileAttributes.ReparsePoint)) == FileAttributes.Directory;

    private static FileAttributes? GetAttributesIfPresent(string path)
    {
        try
        {
            return File.GetAttributes(path);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
    }

    private static string GetDestinationName(Update update)
    {
        if (string.IsNullOrWhiteSpace(update.ModId)
            || !ModInstallPackage.IsSafeName(update.OldFolder)
            || !IsDllName(update.OldEntry) || !IsDllName(update.NewEntry))
            throw new InvalidDataException(L10n.Get("ModInstall_NoEntry"));

        string newFolder = Path.GetFileNameWithoutExtension(update.NewEntry);
        if (!ModInstallPackage.IsSafeName(newFolder))
            throw new InvalidDataException(L10n.Get("ModInstall_NoEntry"));
        return newFolder;
    }

    private static bool IsDllName(string name) =>
        ModInstallPackage.IsSafeName(name) && Path.GetExtension(name).Equals(".dll", StringComparison.OrdinalIgnoreCase);

    private static void EnsureDestinationAvailable(string mods, string? current, string folder)
    {
        string destination = Path.Combine(mods, folder);
        if (!string.Equals(destination, current, PathComparison)
            && (Directory.Exists(destination) || File.Exists(destination)))
            throw new IOException(L10n.Get("ModInstall_FolderExists", folder));
    }

    private static void VerifyEntry(string folder, string entry, string hash, string id)
    {
        string path = Path.Combine(folder, entry);
        if (!File.Exists(path) || HashFile(path) != hash)
            throw new InvalidDataException(L10n.Get("ModInstall_UpdateTargetInvalid", id));
    }

    private static string HashFile(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }
}
