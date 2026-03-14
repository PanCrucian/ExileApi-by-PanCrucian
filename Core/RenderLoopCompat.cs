using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SharpDX.Windows
{
    public class RenderForm : Form
    {
        private int _lastClientWidth;
        private int _lastClientHeight;

        public event EventHandler UserResized;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _lastClientWidth = ClientSize.Width;
            _lastClientHeight = ClientSize.Height;
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);

            if (!IsHandleCreated)
                return;

            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
                return;

            if (ClientSize.Width == _lastClientWidth && ClientSize.Height == _lastClientHeight)
                return;

            _lastClientWidth = ClientSize.Width;
            _lastClientHeight = ClientSize.Height;
            UserResized?.Invoke(this, EventArgs.Empty);
        }
    }

    public static class RenderLoop
    {
        public static void Run(RenderForm form, Action renderCallback)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            if (renderCallback == null) throw new ArgumentNullException(nameof(renderCallback));

            EventHandler handler = null;
            handler = (sender, args) =>
            {
                while (IsApplicationIdle())
                {
                    if (form.IsDisposed || !form.Created)
                    {
                        Application.Idle -= handler;
                        return;
                    }

                    renderCallback();
                }
            };

            Application.Idle += handler;

            try
            {
                Application.Run(form);
            }
            finally
            {
                Application.Idle -= handler;
            }
        }

        private static bool IsApplicationIdle()
        {
            return !PeekMessage(out _, IntPtr.Zero, 0, 0, 0);
        }

        [DllImport("user32.dll")]
        private static extern bool PeekMessage(out NativeMessage lpMsg, IntPtr hWnd, uint wMsgFilterMin,
            uint wMsgFilterMax, uint wRemoveMsg);

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            public IntPtr Handle;
            public uint Message;
            public IntPtr WParam;
            public IntPtr LParam;
            public uint Time;
            public int PointX;
            public int PointY;
        }
    }
}
