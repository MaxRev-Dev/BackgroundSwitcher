
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using static PexelsClient;

namespace BackgroundSwitcher
{
    public sealed class Wallpaper
    {
        private Wallpaper() { }

        private const int SPI_SETDESKWALLPAPER = 20;
        private const int SPIF_UPDATEINIFILE = 0x01;
        private const int SPIF_SENDWININICHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SystemParametersInfo(int uAction, int uParam, string lpvParam, int fuWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int SendMessageTimeout(
                          IntPtr hWnd,      // handle to destination window
                          uint Msg,       // message
                          IntPtr wParam,  // first message parameter
                          IntPtr lParam,   // second message parameter
                            uint fuFlags,
                            uint uTimeout,
                            out IntPtr result

                          );
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, IntPtr ZeroOnly);


        public enum Style : int
        {
            Fill,
            Fit,
            Span,
            Stretch,
            Tile,
            Center
        }
        private static readonly string tmpPath = Path.GetTempPath();
        public static void Set(Photo photo, Style style, string keyword, bool isTmp = false)
        {
            string tempPath = null, folder = null, name = null;
            if (photo.Local != null)
                tempPath = photo.Local;
            else
            {
                folder = Path.Combine(isTmp ? tmpPath : Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                    "BackSwitchCache", keyword.Replace(" ", "_"));
                Directory.CreateDirectory(folder);
            }
            var url = photo.Url?.Trim('/');
            if (photo.Url != null)
            {
                name = url.Substring(url.LastIndexOf('/') + 1) + ".jpg";
            }
            tempPath = tempPath ?? Path.Combine(folder, $"{name}");
            if (!File.Exists(tempPath))
                using (var t = new MemoryStream())
                {
                    new System.Net.WebClient().OpenRead(photo.Src.Original).CopyTo(t);
                    File.WriteAllBytes(tempPath, t.ToArray());
                    Console.WriteLine("Original file dowloaded to \n" + tempPath);
                }
            Console.Title = "BackgroundSwitcher - " + name ?? tempPath.Substring(tempPath.LastIndexOf('/') + 1);
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);

            if (style == Style.Fill)
            {
                key.SetValue(@"WallpaperStyle", 10.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == Style.Fit)
            {
                key.SetValue(@"WallpaperStyle", 6.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == Style.Span) // Windows 8 or newer only!
            {
                key.SetValue(@"WallpaperStyle", 22.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == Style.Stretch)
            {
                key.SetValue(@"WallpaperStyle", 2.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }
            if (style == Style.Tile)
            {
                key.SetValue(@"WallpaperStyle", 0.ToString());
                key.SetValue(@"TileWallpaper", 1.ToString());
            }
            if (style == Style.Center)
            {
                key.SetValue(@"WallpaperStyle", 0.ToString());
                key.SetValue(@"TileWallpaper", 0.ToString());
            }

            SetWallpaperUsingActiveDesktop(tempPath);
        }

        public static void EnableActiveDesktop()
        {
            IntPtr result = IntPtr.Zero;
            SendMessageTimeout(FindWindow("Progman", IntPtr.Zero), 0x52c, IntPtr.Zero, IntPtr.Zero, 0, 500, out result);
        }

        private static Thread thread;
        public static void SetWallpaperUsingActiveDesktop(string path)
        {
            EnableActiveDesktop();

            void threadStarter()
            {
                WinAPI.IActiveDesktop _activeDesktop = WinAPI.ActiveDesktopWrapper.GetActiveDesktop();
                _activeDesktop.SetWallpaper(path, 0);
                _activeDesktop.ApplyChanges(WinAPI.AD_Apply.ALL | WinAPI.AD_Apply.FORCE);

                Marshal.ReleaseComObject(_activeDesktop);
            }
            thread = new Thread(threadStarter);
            thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA (REQUIRED!!!!)
            thread.Start();
            thread.Join(2000);

        }
        public static void Dispose()
        {
            thread?.Join(0);
        }

    }
}
