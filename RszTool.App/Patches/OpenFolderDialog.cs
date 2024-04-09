#if !NET5_0_OR_GREATER

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Microsoft.Win32;

public class OpenFolderDialog
{
    public bool ShowDialog()
    {
        var hwndOwner = NativeMethods.GetActiveWindow();
        var dialog = (IFileOpenDialog)new FileOpenDialog();
        Configure(dialog);

        var hr = dialog.Show(hwndOwner);
        if (hr == NativeMethods.ERROR_CANCELLED)
            return false;

        if (hr != NativeMethods.S_OK)
            return false;

        dialog.GetResult(out var item);
        item.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out var path);
        FolderName = path;
        return true;
    }

    public string? Title { get; set; }
    public string? OkButtonLabel { get; set; }
    public string? InitialDirectory { get; set; }
    public string? FolderName { get; set; }
    public bool ChangeCurrentDirectory { get; set; }

    private void Configure(IFileOpenDialog dialog)
    {
        dialog.SetOptions(CreateOptions());

        if (InitialDirectory != null && !string.IsNullOrEmpty(InitialDirectory))
        {
            var result = NativeMethods.SHCreateItemFromParsingName(InitialDirectory, IntPtr.Zero, typeof(IShellItem).GUID, out var item);
            switch (result)
            {
                case NativeMethods.S_OK:
                    if (item is not null)
                    {
                        dialog.SetFolder(item);
                    }
                    break;
                case NativeMethods.FILE_NOT_FOUND:
                    break;
                default:
                    throw new Win32Exception(result);
            }
        }

        if (Title is not null)
        {
            dialog.SetTitle(Title);
        }

        if (OkButtonLabel is not null)
        {
            dialog.SetOkButtonLabel(OkButtonLabel);
        }
    }

    private FOS CreateOptions()
    {
        var result = FOS.FOS_FORCEFILESYSTEM | FOS.FOS_PICKFOLDERS;
        if (!ChangeCurrentDirectory)
        {
            result |= FOS.FOS_NOCHANGEDIR;
        }

        return result;
    }
}

internal static class NativeMethods
{
#pragma warning disable IDE1006 // Naming Styles
    internal const int S_OK = 0x00000000;
    internal const int ERROR_CANCELLED = unchecked((int)0x800704C7);
    internal const int FILE_NOT_FOUND = unchecked((int)0x80070002);
#pragma warning restore IDE1006 // Naming Styles

    [DllImport("user32")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern IntPtr GetActiveWindow();

    [DllImport("shell32")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    internal static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);
}


internal enum SIGDN : uint
{
    SIGDN_NORMALDISPLAY = 0x00000000,               // SHGDN_NORMAL
    SIGDN_PARENTRELATIVEPARSING = 0x80018001,       // SHGDN_INFOLDER | SHGDN_FORPARSING
    SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,      // SHGDN_FORPARSING
    SIGDN_PARENTRELATIVEEDITING = 0x80031001,       // SHGDN_INFOLDER | SHGDN_FOREDITING
    SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,      // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
    SIGDN_FILESYSPATH = 0x80058000,                 // SHGDN_FORPARSING
    SIGDN_URL = 0x80068000,                         // SHGDN_FORPARSING
    SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001, // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
    SIGDN_PARENTRELATIVE = 0x80080001,               // SHGDN_INFOLDER
}


internal enum FDE_SHAREVIOLATION_RESPONSE
{
    FDESVR_DEFAULT = 0x00000000,
    FDESVR_ACCEPT = 0x00000001,
    FDESVR_REFUSE = 0x00000002,
}


internal enum FDE_OVERWRITE_RESPONSE
{
    FDEOR_DEFAULT = 0x00000000,
    FDEOR_ACCEPT = 0x00000001,
    FDEOR_REFUSE = 0x00000002,
}



internal static class IIDGuid
{
    // IID GUID strings for relevant COM interfaces
    internal const string IModalWindow = "b4db1657-70d7-485e-8e3e-6fcb5a5c1802";
    internal const string IFileDialog = "42f85136-db7e-439c-85f1-e4075d135fc8";
    internal const string IFileOpenDialog = "d57c7288-d4ad-4768-be02-9d969532d960";
    internal const string IFileSaveDialog = "84bccd23-5fde-4cdb-aea4-af64b83d78ab";
    internal const string IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
    internal const string IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
    internal const string IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
}


internal static class CLSIDGuid
{
    internal const string FileOpenDialog = "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7";
    internal const string FileSaveDialog = "C0B4E2F3-BA21-4773-8DBA-335EC946EB8B";
}


[ComImport,
Guid(IIDGuid.IShellItem),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItem
{
    void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);

    void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

    void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

    void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
}


[Flags]
internal enum FOS : uint
{
    /// <summary>
    /// When saving a file, prompt before overwriting an existing file of the same name. This is a default value for the Save dialog.
    /// </summary>
    FOS_OVERWRITEPROMPT = 0x00000002,

    /// <summary>
    /// In the Save dialog, only allow the user to choose a file that has one of the file name extensions specified through IFileDialog::SetFileTypes.
    /// </summary>
    FOS_STRICTFILETYPES = 0x00000004,

    /// <summary>
    /// Don't change the current working directory.
    /// </summary>
    FOS_NOCHANGEDIR = 0x00000008,

    /// <summary>
    /// Present an Open dialog that offers a choice of folders rather than files.
    /// </summary>
    FOS_PICKFOLDERS = 0x00000020,

    /// <summary>
    /// Ensures that returned items are file system items (SFGAO_FILESYSTEM). Note that this does not apply to items returned by IFileDialog::GetCurrentSelection.
    /// </summary>
    FOS_FORCEFILESYSTEM = 0x00000040,

    /// <summary>
    /// Enables the user to choose any item in the Shell namespace, not just those with SFGAO_STREAM or SFAGO_FILESYSTEM attributes. This flag cannot be combined with FOS_FORCEFILESYSTEM.
    /// </summary>
    FOS_ALLNONSTORAGEITEMS = 0x00000080,

    /// <summary>
    /// Do not check for situations that would prevent an application from opening the selected file, such as sharing violations or access denied errors.
    /// </summary>
    FOS_NOVALIDATE = 0x00000100,

    /// <summary>
    /// Enables the user to select multiple items in the open dialog. Note that when this flag is set, the IFileOpenDialog interface must be used to retrieve those items.
    /// </summary>
    FOS_ALLOWMULTISELECT = 0x00000200,

    /// <summary>
    /// The item returned must be in an existing folder. This is a default value.
    /// </summary>
    FOS_PATHMUSTEXIST = 0x00000800,

    /// <summary>
    /// The item returned must exist. This is a default value for the Open dialog.
    /// </summary>
    FOS_FILEMUSTEXIST = 0x00001000,

    /// <summary>
    /// Prompt for creation if the item returned in the save dialog does not exist. Note that this does not actually create the item.
    /// </summary>
    FOS_CREATEPROMPT = 0x00002000,

    /// <summary>
    /// In the case of a sharing violation when an application is opening a file, call the application back through OnShareViolation for guidance. This flag is overridden by FOS_NOVALIDATE.
    /// </summary>
    FOS_SHAREAWARE = 0x00004000,

    /// <summary>
    /// Do not return read-only items. This is a default value for the Save dialog.
    /// </summary>
    FOS_NOREADONLYRETURN = 0x00008000,

    /// <summary>
    /// Do not test whether creation of the item as specified in the Save dialog will be successful. If this flag is not set, the calling application must handle errors, such as denial of access, discovered when the item is created.
    /// </summary>
    FOS_NOTESTFILECREATE = 0x00010000,

    /// <summary>
    /// Hide the list of places from which the user has recently opened or saved items. This value is not supported as of Windows 7.
    /// </summary>
    FOS_HIDEMRUPLACES = 0x00020000,

    /// <summary>
    /// Hide items shown by default in the view's navigation pane. This flag is often used in conjunction with the IFileDialog::AddPlace method, to hide standard locations and replace them with custom locations.
    /// </summary>
    FOS_HIDEPINNEDPLACES = 0x00040000,

    /// <summary>
    /// Shortcuts should not be treated as their target items. This allows an application to open a .lnk file rather than what that file is a shortcut to.
    /// </summary>
    FOS_NODEREFERENCELINKS = 0x00100000,

    /// <summary>
    /// Do not add the item being opened or saved to the recent documents list (SHAddToRecentDocs).
    /// </summary>
    FOS_DONTADDTORECENT = 0x02000000,

    /// <summary>
    /// Include hidden and system items.
    /// </summary>
    FOS_FORCESHOWHIDDEN = 0x10000000,

    /// <summary>
    /// Indicates to the Save As dialog box that it should open in expanded mode. Expanded mode is the mode that is set and unset by clicking the button in the lower-left corner of the Save As dialog box that switches between Browse Folders and Hide Folders when clicked. This value is not supported as of Windows 7.
    /// </summary>
    FOS_DEFAULTNOMINIMODE = 0x20000000,

    /// <summary>
    /// Indicates to the Open dialog box that the preview pane should always be displayed.
    /// </summary>
    FOS_FORCEPREVIEWPANEON = 0x40000000,

    /// <summary>
    /// Indicates that the caller is opening a file as a stream (BHID_Stream), so there is no need to download that file.
    /// </summary>
    FOS_SUPPORTSTREAMABLEITEMS = 0x80000000,
}


[ComImport]
[ClassInterface(ClassInterfaceType.None)]
[Guid(CLSIDGuid.FileOpenDialog)]
internal class FileOpenDialog
{
}


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
internal struct COMDLG_FILTERSPEC
{
    [MarshalAs(UnmanagedType.LPWStr)]
    public string? PszName;

    [MarshalAs(UnmanagedType.LPWStr)]
    public string? PszSpec;
}


/* [ComImport()]
[Guid(IIDGuid.IFileDialog)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialog
{
    [PreserveSig]
    int Show([In] IntPtr parent);

    void SetFileTypes([In] uint cFileTypes, [In][MarshalAs(UnmanagedType.LPArray)] COMDLG_FILTERSPEC[] rgFilterSpec);

    void SetFileTypeIndex([In] uint iFileType);

    void GetFileTypeIndex(out uint piFileType);

    void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);

    void Unadvise([In] uint dwCookie);

    void SetOptions([In] FOS fos);

    void GetOptions(out FOS pfos);

    void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

    void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

    void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);

    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);

    void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

    void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);

    void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

    void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, int alignment);

    void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

    void Close([MarshalAs(UnmanagedType.Error)] int hr);

    void SetClientGuid([In] ref Guid guid);

    void ClearClientData();

    void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
} */


[ComImport()]
[Guid(IIDGuid.IFileOpenDialog)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileOpenDialog
{
    [PreserveSig]
    int Show([In] IntPtr parent);

    void SetFileTypes([In] uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);
    void SetFileTypeIndex([In] uint iFileType);
    void GetFileTypeIndex(out uint piFileType);
    // void Advise([In, MarshalAs(UnmanagedType.Interface)] IFileDialogEvents pfde, out uint pdwCookie);
    void Advise(); // incomplete signature
    void Unadvise([In] uint dwCookie);
    void SetOptions([In] FOS fos);
    void GetOptions(out FOS pfos);
    void SetDefaultFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
    void SetFolder([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);
    void GetFolder([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
    void GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
    void SetFileName([In, MarshalAs(UnmanagedType.LPWStr)] string pszName);
    void GetFileName([MarshalAs(UnmanagedType.LPWStr)] out string pszName);
    void SetTitle([In, MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
    void SetOkButtonLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszText);
    void SetFileNameLabel([In, MarshalAs(UnmanagedType.LPWStr)] string pszLabel);
    void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
    // void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, FileDialogCustomPlace fdcp);
    void AddPlace(); // incomplete signature
    void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);
    void Close([MarshalAs(UnmanagedType.Error)] int hr);
    void SetClientGuid([In] ref Guid guid);
    void ClearClientData();
    void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
    // void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);
    void GetResults([MarshalAs(UnmanagedType.Interface)] out object ppenum);
    // void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);
    void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out object ppsai);
}


/*
[ComImport,
Guid(IIDGuid.IFileDialogEvents),
InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IFileDialogEvents
{
    // NOTE: some of these callbacks are cancelable - returning S_FALSE means that
    // the dialog should not proceed (e.g. with closing, changing folder); to
    // support this, we need to use the PreserveSig attribute to enable us to return
    // the proper HRESULT
    [PreserveSig]
    int OnFileOk([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    [PreserveSig]
    int OnFolderChanging([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psiFolder);

    void OnFolderChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    void OnSelectionChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    void OnShareViolation([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out FDE_SHAREVIOLATION_RESPONSE pResponse);

    void OnTypeChange([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd);

    void OnOverwrite([In, MarshalAs(UnmanagedType.Interface)] IFileDialog pfd, [In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, out FDE_OVERWRITE_RESPONSE pResponse);
}



[ComImport]
[Guid(IIDGuid.IShellItemArray)]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellItemArray
{
    // Not supported: IBindCtx

    void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, out IntPtr ppvOut);

    void GetPropertyStore([In] int flags, [In] ref Guid riid, out IntPtr ppv);

    void GetPropertyDescriptionList([In] ref PROPERTYKEY keyType, [In] ref Guid riid, out IntPtr ppv);

    void GetAttributes([In] SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

    void GetCount(out uint pdwNumItems);

    void GetItemAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

    void EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
internal struct PROPERTYKEY
{
    public Guid Fmtid;
    public uint Pid;
}

internal enum SIATTRIBFLAGS
{
    SIATTRIBFLAGS_AND = 0x00000001, // if multiple items and the attributes together.
    SIATTRIBFLAGS_OR = 0x00000002, // if multiple items or the attributes together.
    SIATTRIBFLAGS_APPCOMPAT = 0x00000003, // Call GetAttributes directly on the ShellFolder for multiple attributes
} */

#endif
