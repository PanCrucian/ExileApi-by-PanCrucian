using System;
using System.Security;
using Microsoft.Win32.SafeHandles;

namespace ExileCore.Shared.SomeMagic
{
    [SuppressUnmanagedCodeSecurity]
    public sealed class SafeMemoryHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public SafeMemoryHandle() : base(true)
        {
        }

        public SafeMemoryHandle(IntPtr handle) : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            return handle != IntPtr.Zero && Imports.CloseHandle(handle);
        }
    }
}
