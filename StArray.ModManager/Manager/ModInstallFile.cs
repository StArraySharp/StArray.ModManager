namespace StArray.ModManager.Manager;

/// <summary>A file selected by the platform picker. The installer takes ownership of the stream.</summary>
public sealed record ModInstallFile(string Name, Stream Content);
