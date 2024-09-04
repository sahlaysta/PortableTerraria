using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// File open dialogs, also supporting the folder browser dialog.
    /// </summary>
    internal static class GuiFileDialogs
    {

        public static string ShowOpenFileDialog(
            IWin32Window owner, string title, string initialDirectory, string filter)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (title != null)
                    openFileDialog.Title = title;
                if (initialDirectory != null)
                    openFileDialog.InitialDirectory = initialDirectory;
                if (filter != null)
                    openFileDialog.Filter = filter;
                DialogResult dialogResult = openFileDialog.ShowDialog(owner);
                return dialogResult == DialogResult.OK ? openFileDialog.FileName : null;
            }
        }

        public static string ShowSaveFileDialog(
            IWin32Window owner, string title, string initialDirectory, string filter)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                if (title != null)
                    saveFileDialog.Title = title;
                if (initialDirectory != null)
                    saveFileDialog.InitialDirectory = initialDirectory;
                if (filter != null)
                    saveFileDialog.Filter = filter;
                DialogResult dialogResult = saveFileDialog.ShowDialog(owner);
                return dialogResult == DialogResult.OK ? saveFileDialog.FileName : null;
            }
        }

        public static string ShowOpenFolderDialog(IWin32Window owner, string title, string initialDirectory)
        {
            IntPtr handle = owner == null ? IntPtr.Zero : owner.Handle;
            if (handle != IntPtr.Zero)
            {
                return ShowFolderDialog(handle, title, initialDirectory);
            }
            else
            {
                string path = null;
                UsingDefaultDialog(dialogHandle =>
                {
                    path = ShowFolderDialog(dialogHandle, title, initialDirectory);
                });
                return path;
            }
        }

        private static string ShowFolderDialog(IntPtr hwndOwner, string title, string initialDirectory)
        {
            try
            {
                return ShowNativeFolderDialog(hwndOwner, title, initialDirectory);
            }
            catch (NativeDialogInitializationException e)
            {
                Console.Error.WriteLine(e);
                using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
                {
                    folderBrowserDialog.ShowNewFolderButton = false;
                    if (title != null)
                        folderBrowserDialog.Description = title;
                    if (initialDirectory != null)
                        folderBrowserDialog.SelectedPath = initialDirectory;
                    DialogResult dialogResult = folderBrowserDialog.ShowDialog(new IWin32WindowWrapper(hwndOwner));
                    return dialogResult == DialogResult.OK ? folderBrowserDialog.SelectedPath : null;
                }
            }
        }

        private static string ShowNativeFolderDialog(IntPtr hwndOwner, string title, string initialDirectory)
        {
            NativeFileOpenDialog nativeFileOpenDialog;
            try
            {
                nativeFileOpenDialog = (NativeFileOpenDialog)new FileOpenDialogRCW();
            }
            catch (Exception e) { throw new NativeDialogInitializationException(e); }

            try
            {

                try
                {
                    if (title != null)
                    {
                        nativeFileOpenDialog.SetTitle(title);
                    }

                    if (initialDirectory != null)
                    {
                        IntPtr ppidl;
                        uint rgfInOut = 0;
                        IShellItem iShellItem;
                        if (SHILCreateFromPath(initialDirectory, out ppidl, ref rgfInOut) == 0
                            && SHCreateShellItem(IntPtr.Zero, IntPtr.Zero, ppidl, out iShellItem) == 0)
                        {
                            nativeFileOpenDialog.SetFolder(iShellItem);
                        }
                    }

                    nativeFileOpenDialog.SetOptions(FOS.FOS_PICKFOLDERS | FOS.FOS_FORCEFILESYSTEM);
                }
                catch (Exception e) { throw new NativeDialogInitializationException(e); }

                if (nativeFileOpenDialog.Show(hwndOwner) != 0)
                {
                    return null;
                }
                else
                {
                    IShellItem iShellItem;
                    nativeFileOpenDialog.GetResult(out iShellItem);
                    string path;
                    iShellItem.GetDisplayName(SIGDN.SIGDN_FILESYSPATH, out path);
                    return path;
                }

            }
            finally
            {
                Marshal.ReleaseComObject(nativeFileOpenDialog);
            }
        }

        private class NativeDialogInitializationException : Exception
        {
            public NativeDialogInitializationException(Exception innerException) : base(null, innerException) { }
        }

        private struct IWin32WindowWrapper : IWin32Window
        {

            private readonly IntPtr value;

            public IWin32WindowWrapper(IntPtr value)
            {
                this.value = value;
            }

            IntPtr IWin32Window.Handle { get { return value; } }

        }

        private class DialogWrapper : CommonDialog
        {

            private readonly Action<IntPtr> block;

            public DialogWrapper(Action<IntPtr> block)
            {
                this.block = block;
            }

            public override void Reset() { }

            protected override bool RunDialog(IntPtr hwndOwner)
            {
                block(hwndOwner);
                return true;
            }

        }

        private static void UsingDefaultDialog(Action<IntPtr> block)
        {
            using (DialogWrapper dialogWrapper = new DialogWrapper(block))
            {
                dialogWrapper.ShowDialog();
            }
        }

        [DllImport("shell32.dll")]
        private static extern int SHILCreateFromPath([MarshalAs(UnmanagedType.LPWStr)] string pszPath, out IntPtr ppidl, ref uint rgfInOut);

        [DllImport("shell32.dll")]
        private static extern int SHCreateShellItem(IntPtr pidlParent, IntPtr psfParent, IntPtr pidl, out IShellItem ppsi);

        [ComImport]
        [Guid(IIDGuid.IFileOpenDialog)]
        [CoClass(typeof(FileOpenDialogRCW))]
        private interface NativeFileOpenDialog : IFileOpenDialog
        { }

        [ComImport]
        [Guid(IIDGuid.IFileSaveDialog)]
        [CoClass(typeof(FileSaveDialogRCW))]
        private interface NativeFileSaveDialog : IFileSaveDialog
        { }

        [ComImport]
        [ClassInterface(ClassInterfaceType.None)]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        [Guid(CLSIDGuid.FileOpenDialog)]
        private class FileOpenDialogRCW
        { }

        [ComImport]
        [ClassInterface(ClassInterfaceType.None)]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        [Guid(CLSIDGuid.FileSaveDialog)]
        private class FileSaveDialogRCW
        { }

        private class IIDGuid
        {
            private IIDGuid() { } // Avoid FxCop violation AvoidUninstantiatedInternalClasses
            // IID GUID strings for relevant COM interfaces
            public const string IModalWindow = "b4db1657-70d7-485e-8e3e-6fcb5a5c1802";
            public const string IFileDialog = "42f85136-db7e-439c-85f1-e4075d135fc8";
            public const string IFileOpenDialog = "d57c7288-d4ad-4768-be02-9d969532d960";
            public const string IFileSaveDialog = "84bccd23-5fde-4cdb-aea4-af64b83d78ab";
            public const string IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
            public const string IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
            public const string IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
        }

        private class CLSIDGuid
        {
            private CLSIDGuid() { } // Avoid FxCop violation AvoidUninstantiatedInternalClasses
            public const string FileOpenDialog = "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7";
            public const string FileSaveDialog = "C0B4E2F3-BA21-4773-8DBA-335EC946EB8B";
        }

        [ComImport()]
        [Guid(IIDGuid.IModalWindow)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IModalWindow
        {

            [PreserveSig]
            int Show([In] IntPtr parent);
        }

        private enum SIATTRIBFLAGS
        {
            SIATTRIBFLAGS_AND = 0x00000001, // if multiple items and the attributes together.
            SIATTRIBFLAGS_OR = 0x00000002, // if multiple items or the attributes together.
            SIATTRIBFLAGS_APPCOMPAT = 0x00000003, // Call GetAttributes directly on the ShellFolder for multiple attributes
        }

        [ComImport]
        [Guid(IIDGuid.IShellItemArray)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItemArray
        {
            // Not supported: IBindCtx

            void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, out IntPtr ppvOut);

            void GetPropertyStore([In] int Flags, [In] ref Guid riid, out IntPtr ppv);

            void GetPropertyDescriptionList([In] ref PROPERTYKEY keyType, [In] ref Guid riid, out IntPtr ppv);

            void GetAttributes([In] SIATTRIBFLAGS dwAttribFlags, [In] uint sfgaoMask, out uint psfgaoAttribs);

            void GetCount(out uint pdwNumItems);

            void GetItemAt([In] uint dwIndex, [MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            void EnumItems([MarshalAs(UnmanagedType.Interface)] out IntPtr ppenumShellItems);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PROPERTYKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [ComImport()]
        [Guid(IIDGuid.IFileDialog)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
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
        }

        [ComImport()]
        [Guid(IIDGuid.IFileOpenDialog)]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileOpenDialog : IFileDialog
        {

#pragma warning disable CS0108

            [PreserveSig]
            int Show([In] IntPtr parent);

            void SetFileTypes([In] uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);

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

            void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, FileDialogCustomPlace fdcp);

            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            void Close([MarshalAs(UnmanagedType.Error)] int hr);

            void SetClientGuid([In] ref Guid guid);

            void ClearClientData();

            void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

            void GetResults([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppenum);

            void GetSelectedItems([MarshalAs(UnmanagedType.Interface)] out IShellItemArray ppsai);

#pragma warning restore CS0108

        }

        [ComImport(),
        Guid(IIDGuid.IFileSaveDialog),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileSaveDialog : IFileDialog
        {

#pragma warning disable CS0108

            [PreserveSig]
            int Show([In] IntPtr parent);

            void SetFileTypes([In] uint cFileTypes, [In] ref COMDLG_FILTERSPEC rgFilterSpec);

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

            void AddPlace([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, FileDialogCustomPlace fdcp);

            void SetDefaultExtension([In, MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            void Close([MarshalAs(UnmanagedType.Error)] int hr);

            void SetClientGuid([In] ref Guid guid);

            void ClearClientData();

            void SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);

            void SetSaveAsItem([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            void SetProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore);

            void SetCollectedProperties([In, MarshalAs(UnmanagedType.Interface)] IntPtr pList, [In] int fAppendDefault);

            void GetProperties([MarshalAs(UnmanagedType.Interface)] out IntPtr ppStore);

            void ApplyProperties([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pStore, [In, ComAliasName("ShellObjects.wireHWND")] ref IntPtr hwnd, [In, MarshalAs(UnmanagedType.Interface)] IntPtr pSink);

#pragma warning restore CS0108

        }

        [ComImport,
        Guid(IIDGuid.IFileDialogEvents),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialogEvents
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

        [ComImport,
        Guid(IIDGuid.IShellItem),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler([In, MarshalAs(UnmanagedType.Interface)] IntPtr pbc, [In] ref Guid bhid, [In] ref Guid riid, out IntPtr ppv);

            void GetParent([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);

            void GetDisplayName([In] SIGDN sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);

            void GetAttributes([In] uint sfgaoMask, out uint psfgaoAttribs);

            void Compare([In, MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, out int piOrder);
        }

        private enum SIGDN : uint
        {
            SIGDN_NORMALDISPLAY = 0x00000000,           // SHGDN_NORMAL
            SIGDN_PARENTRELATIVEPARSING = 0x80018001,   // SHGDN_INFOLDER | SHGDN_FORPARSING
            SIGDN_DESKTOPABSOLUTEPARSING = 0x80028000,  // SHGDN_FORPARSING
            SIGDN_PARENTRELATIVEEDITING = 0x80031001,   // SHGDN_INFOLDER | SHGDN_FOREDITING
            SIGDN_DESKTOPABSOLUTEEDITING = 0x8004c000,  // SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
            SIGDN_FILESYSPATH = 0x80058000,             // SHGDN_FORPARSING
            SIGDN_URL = 0x80068000,                     // SHGDN_FORPARSING
            SIGDN_PARENTRELATIVEFORADDRESSBAR = 0x8007c001,     // SHGDN_INFOLDER | SHGDN_FORPARSING | SHGDN_FORADDRESSBAR
            SIGDN_PARENTRELATIVE = 0x80080001           // SHGDN_INFOLDER
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto, Pack = 4)]
        private struct COMDLG_FILTERSPEC
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pszSpec;
        }

        [Flags]
        private enum FOS : uint
        {
            FOS_OVERWRITEPROMPT = 0x00000002,
            FOS_STRICTFILETYPES = 0x00000004,
            FOS_NOCHANGEDIR = 0x00000008,
            FOS_PICKFOLDERS = 0x00000020,
            FOS_FORCEFILESYSTEM = 0x00000040, // Ensure that items returned are filesystem items.
            FOS_ALLNONSTORAGEITEMS = 0x00000080, // Allow choosing items that have no storage.
            FOS_NOVALIDATE = 0x00000100,
            FOS_ALLOWMULTISELECT = 0x00000200,
            FOS_PATHMUSTEXIST = 0x00000800,
            FOS_FILEMUSTEXIST = 0x00001000,
            FOS_CREATEPROMPT = 0x00002000,
            FOS_SHAREAWARE = 0x00004000,
            FOS_NOREADONLYRETURN = 0x00008000,
            FOS_NOTESTFILECREATE = 0x00010000,
            FOS_HIDEMRUPLACES = 0x00020000,
            FOS_HIDEPINNEDPLACES = 0x00040000,
            FOS_NODEREFERENCELINKS = 0x00100000,
            FOS_DONTADDTORECENT = 0x02000000,
            FOS_FORCESHOWHIDDEN = 0x10000000,
            FOS_DEFAULTNOMINIMODE = 0x20000000
        }

        private enum FDE_SHAREVIOLATION_RESPONSE
        {
            FDESVR_DEFAULT = 0x00000000,
            FDESVR_ACCEPT = 0x00000001,
            FDESVR_REFUSE = 0x00000002
        }

        private enum FDE_OVERWRITE_RESPONSE
        {
            FDEOR_DEFAULT = 0x00000000,
            FDEOR_ACCEPT = 0x00000001,
            FDEOR_REFUSE = 0x00000002
        }
    }
}