using System.IO.Compression;
using StArray.ModManager.Manager;
using StArray.ModManager.Resources;

namespace StArray.ModManager.Runtime;

internal sealed class ModInstallPackage(string stagingDirectory, string modDirectory, string preferredName)
    : IDisposable
{
    public string ModDirectory { get; } = modDirectory;
    public string StagingDirectory { get; } = stagingDirectory;

    public static ModInstallPackage Prepare(string modsDirectory, ModInstallFile selected)
    {
        string extension = Path.GetExtension(selected.Name);
        bool isZip = extension.Equals(".zip", StringComparison.OrdinalIgnoreCase);
        if (!IsSafeName(selected.Name)
            || (!isZip && !extension.Equals(".dll", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException(L10n.Get("ModInstall_Unsupported"));

        // Keep staging on the same volume as the final destination. The scanner only sees
        // immediate children of mods/ and cannot discover packages inside this directory.
        string staging = Path.Combine(Path.GetFullPath(modsDirectory), ".starray-install",
            Guid.NewGuid().ToString("N"));
        string extracted = Path.Combine(staging, "extracted");
        Directory.CreateDirectory(extracted);
        try
        {
            if (isZip)
            {
                using var archive = new ZipArchive(selected.Content, ZipArchiveMode.Read, leaveOpen: true);
                archive.ExtractToDirectory(extracted);
            }
            else
            {
                using var entry = File.Create(Path.Combine(extracted, selected.Name));
                selected.Content.CopyTo(entry);
            }

            string modDirectory = FindModDirectory(extracted);
            string preferredName = modDirectory == extracted
                ? Path.GetFileNameWithoutExtension(selected.Name)
                : Path.GetFileName(modDirectory);
            string content = Path.Combine(staging, "content");
            Directory.Move(modDirectory, content);
            return new ModInstallPackage(staging, content, preferredName);
        }
        catch
        {
            DeleteStagingDirectory(staging);
            throw;
        }
    }

    public IEnumerable<string> GetEntryCandidates() => GetDlls(ModDirectory)
        .OrderByDescending(path => string.Equals(Path.GetFileNameWithoutExtension(path),
            preferredName, StringComparison.OrdinalIgnoreCase))
        .ThenBy(path => path, StringComparer.OrdinalIgnoreCase);

    private static string FindModDirectory(string directory)
    {
        while (!GetDlls(directory).Any())
        {
            var children = Directory.EnumerateDirectories(directory)
                .Where(path => Path.GetFileName(path) != "__MACOSX")
                .Take(2).ToArray();
            if (children.Length != 1)
                throw new InvalidDataException(L10n.Get("ModInstall_NoEntry"));
            directory = children[0];
        }

        return directory;
    }

    private static IEnumerable<string> GetDlls(string directory) => Directory.EnumerateFiles(directory)
        .Where(path => Path.GetExtension(path).Equals(".dll", StringComparison.OrdinalIgnoreCase)
            && !Path.GetFileNameWithoutExtension(path).Equals("StArray.ModManager", StringComparison.OrdinalIgnoreCase));

    internal static bool IsSafeName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name == name.TrimEnd(' ', '.')
        && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
        && !name.Contains('/') && !name.Contains('\\');

    public void Dispose() => DeleteStagingDirectory(stagingDirectory);

    internal static void DeleteStagingDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Logger.Warn(nameof(ModInstallPackage), $"Could not remove installation staging: {exception.Message}");
        }
    }
}
