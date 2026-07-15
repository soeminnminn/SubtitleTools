using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Runtime.InteropServices.Marshalling;

namespace SubtitleTools.UI.Controls
{
    internal partial class MpvPlayer : WindowsFormsHost
    {
        #region Declarations
        private readonly Panel host;
        #endregion

        #region Constructors
        static MpvPlayer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MpvPlayer), new FrameworkPropertyMetadata(typeof(MpvPlayer)));
        }

        public MpvPlayer()
        {
            this.host = new Panel()
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                BackColor = System.Drawing.Color.Black,
            };

            this.Child = this.host;
        }
        #endregion

        #region Properties
        #endregion

        #region Methods
        #endregion

        #region NativeMethods
        private const string MPVDLL = "libmpv-2.dll";

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        internal static partial nint LoadLibrary(string dllToLoad);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        internal static partial nint GetProcAddress(nint hModule, string procedureName);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FreeLibrary(nint hModule);

        [LibraryImport(MPVDLL, EntryPoint = "mpv_create")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial nint MPVCreate();


        [LibraryImport(MPVDLL, EntryPoint = "mpv_initialize")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVInitialize(nint mpvHandle);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_command")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVCommand(nint mpvHandle, nint utf8Strings);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_terminate_destroy")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVTerminateDestroy(nint mpvHandle);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_wait_event")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial nint MPVWaitEvent(nint mpvHandle, double wait);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_set_option", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVSetOption(nint mpvHandle, string name, int format, ref long data);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_set_option_string", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVSetOptionString(nint mpvHandle, string name, string value);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_get_property_string", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial nint MPVGetPropertyString(nint mpvHandle, string name);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_get_property", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVGetProperty(nint mpvHandle, string name, int format, ref double data);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_set_property", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(AnsiStringMarshaller))]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVSetProperty(nint mpvHandle, string name, int format, string data);


        [LibraryImport(MPVDLL, EntryPoint = "mpv_free")]
        [UnmanagedCallConv(CallConvs = new Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        internal static partial int MPVFree(nint data);
        #endregion
    }
}
