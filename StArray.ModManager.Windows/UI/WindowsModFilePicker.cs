using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using StArray.ModManager.Manager;
using StArray.ModManager.Resources;

namespace StArray.ModManager.Windows.UI;

internal static class WindowsModFilePicker
{
    private const uint OfnNoChangeDir = 0x00000008;
    private const uint OfnPathMustExist = 0x00000800;
    private const uint OfnFileMustExist = 0x00001000;
    private const uint OfnExplorer = 0x00080000;

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenFileName
    {
        public uint Size;
        public nint Owner;
        public nint Instance;
        public nint Filter;
        public nint CustomFilter;
        public uint MaxCustomFilter;
        public uint FilterIndex;
        public nint File;
        public uint MaxFile;
        public nint FileTitle;
        public uint MaxFileTitle;
        public nint InitialDirectory;
        public nint Title;
        public uint Flags;
        public ushort FileOffset;
        public ushort ExtensionOffset;
        public nint DefaultExtension;
        public nint CustomData;
        public nint Hook;
        public nint TemplateName;
        public nint Reserved;
        public uint ReservedFlags;
        public uint FlagsEx;
    }

    [DllImport("comdlg32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileNameW(ref OpenFileName options);

    [DllImport("comdlg32.dll", ExactSpelling = true)]
    private static extern uint CommDlgExtendedError();

    public static Task<ModInstallFile?> PickAsync()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException();

        var result = new TaskCompletionSource<ModInstallFile?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                result.SetResult(Pick());
            }
            catch (Exception exception)
            {
                result.SetException(exception);
            }
        }) { IsBackground = true, Name = "Mod file picker" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return result.Task;
    }

    private static unsafe ModInstallFile? Pick()
    {
        string title = L10n.Get("ModInstall_PickerTitle");
        string filter = L10n.Get("ModInstall_FileFilter") + "\0*.zip;*.dll\0\0";
        var file = new char[32_768];
        using var process = Process.GetCurrentProcess();
        fixed (char* titlePointer = title, filterPointer = filter, filePointer = file)
        {
            var options = new OpenFileName
            {
                Size = (uint)sizeof(OpenFileName),
                Owner = process.MainWindowHandle,
                Title = (nint)titlePointer,
                Filter = (nint)filterPointer,
                FilterIndex = 1,
                File = (nint)filePointer,
                MaxFile = (uint)file.Length,
                Flags = OfnExplorer | OfnNoChangeDir | OfnFileMustExist | OfnPathMustExist
            };
            if (!GetOpenFileNameW(ref options))
            {
                uint error = CommDlgExtendedError();
                if (error == 0)
                    return null;
                throw new Win32Exception((int)error, L10n.Get("ModInstall_WindowsPickerError", error));
            }
        }

        string path = new(file, 0, Array.IndexOf(file, '\0'));
        return new ModInstallFile(Path.GetFileName(path), File.OpenRead(path));
    }
}
