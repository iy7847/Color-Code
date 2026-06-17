using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;

namespace ColorCodePicker
{
    public partial class EyedropperOverlay : Window
    {
        private MainWindow _parentWindow;

        public EyedropperOverlay(MainWindow parent)
        {
            InitializeComponent();
            _parentWindow = parent;

            // 다중 모니터 전체 영역 덮기
            this.Left = SystemParameters.VirtualScreenLeft;
            this.Top = SystemParameters.VirtualScreenTop;
            this.Width = SystemParameters.VirtualScreenWidth;
            this.Height = SystemParameters.VirtualScreenHeight;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetCursorPos(ref Win32Point pt);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Win32Point
        {
            public Int32 X;
            public Int32 Y;
        };

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            var color = GetColorAtCursor();
            _parentWindow.SetRgbFromEyedropper(color.R, color.G, color.B);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var color = GetColorAtCursor();
            _parentWindow.SetRgbFromEyedropper(color.R, color.G, color.B);
            this.Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private System.Windows.Media.Color GetColorAtCursor()
        {
            Win32Point w32Mouse = new Win32Point();
            GetCursorPos(ref w32Mouse);

            using (Bitmap bmp = new Bitmap(1, 1))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.CopyFromScreen(w32Mouse.X, w32Mouse.Y, 0, 0, new System.Drawing.Size(1, 1));
                }
                var pixel = bmp.GetPixel(0, 0);
                return System.Windows.Media.Color.FromArgb(pixel.A, pixel.R, pixel.G, pixel.B);
            }
        }
    }
}
