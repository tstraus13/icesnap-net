using System;
using System.Threading.Tasks;
using System.Linq;

namespace IceSnap
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try 
            {
                if (System.Diagnostics.Process.GetProcessesByName(System.IO.Path.GetFileNameWithoutExtension(System.Reflection.Assembly.GetEntryAssembly().Location)).Count() > 1)
                    throw new Exception("ERROR: Snap is in progress. Must wait for it to finish before running again.");

                switch (args[0])
                {
                    case "up":
                        await SnapUp();
                        break;
                    case "down":
                        await SnapDown();
                        break;
                    case "left":
                        await SnapLeft();
                        break;
                    case "right":
                        await SnapRight();
                        break;
                    default:
                        break;
                }

                return 0;
            }

            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return 1;
            }
        }

        public static async Task SnapLeft()
        {
            int offset;
            Screen toScreen;

            var window = await IceWM.GetFocusedWindow();
            var screens = await IceWM.GetScreens();

            if (window.IsSnappedLeft)
            {
                if (!window.CanSnapLeft)
                    return;

                var screen = screens
                    .Where(s => (s.Geometry.OffsetX + s.Geometry.Width) == window.Screen.Geometry.OffsetX)
                    .FirstOrDefault();

                if (screen == null)
                    return;

                toScreen = screen;
                offset = screen.Geometry.OffsetX + (screen.Geometry.Width / 2);
            }

            else
            {
                toScreen = window.Screen;
                offset = window.Screen.Geometry.OffsetX;
            }

            await IceWM.Restore();
            await IceWM.SizeTo($"{(toScreen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right}", "50%");
            await IceWM.Move(offset, 0);
            await IceWM.VerticalMax();
        }

        public static async Task SnapRight()
        {
            int offset;
            Screen toScreen;

            var window = await IceWM.GetFocusedWindow();
            var screens = await IceWM.GetScreens();

            if (window.IsSnappedRight)
            {
                if (!window.CanSnapRight)
                    return;

                var screen = screens
                    .Where(s => s.Geometry.OffsetX == (window.Screen.Geometry.OffsetX + window.Screen.Geometry.Width))
                    .FirstOrDefault();

                if (screen == null)
                    return;

                toScreen = screen;
                offset = screen.Geometry.OffsetX;
            }

            else
            {
                toScreen = window.Screen;
                offset = window.Screen.Geometry.OffsetX + (window.Screen.Geometry.Width / 2);
            }

            await IceWM.Restore();
            await IceWM.SizeTo($"{(toScreen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right}", "50%");
            await IceWM.Move(offset, 0);
            await IceWM.VerticalMax();
        }

        public static async Task SnapUp()
        {
            int offset_x;
            int offset_y;
            int size_y;
            int size_x;
            Screen toScreen;

            var window = await IceWM.GetFocusedWindow(true);
            var screens = await IceWM.GetScreens();

            if (window.IsSnappedTop)
            {
                if (!window.Properties.State.IsMaximized)
                {
                    await IceWM.Maximize();
                    return;
                }

                if (!window.CanSnapTop)
                    return;

                var screen = screens
                    .Where(s => s.Geometry.OffsetX == window.Screen.Geometry.OffsetX
                        && s.Geometry.OffsetY < window.Screen.Geometry.Height)
                    .FirstOrDefault();

                if (screen == null)
                    return;

                toScreen = screen;

                if (toScreen.Taskbar != null)
                {
                    offset_y = toScreen.Geometry.OffsetY + (toScreen.Geometry.Height / 2);
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top
                        - (window.Screen.Taskbar.OnTop ? window.Screen.Taskbar.Geometry.Height : 0);
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }

                else
                {
                    offset_y = toScreen.Geometry.OffsetY + (toScreen.Geometry.Height / 2);
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top;
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }
            }

            else if (!window.IsSnappedLeft && !window.IsSnappedRight)
            {
                if (!window.CanSnapTop)
                    return;

                toScreen = window.Screen;

                if (toScreen.Taskbar != null)
                {
                    offset_y = toScreen.Geometry.OffsetY;
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top 
                        - (window.Screen.Taskbar.OnTop ? window.Screen.Taskbar.Geometry.Height : 0);
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }

                else
                {
                    offset_y = toScreen.Geometry.OffsetY;
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top;
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }
            }

            else
            {
                toScreen = window.Screen;

                if (toScreen.Taskbar != null)
                {
                    offset_y = toScreen.Geometry.OffsetY;
                    
                    if (window.IsSnappedRight)
                        offset_x = toScreen.Geometry.OffsetX + toScreen.Geometry.Width / 2;
                    else
                        offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top 
                        - (window.Screen.Taskbar.OnTop ? window.Screen.Taskbar.Geometry.Height : 0);
                    size_x = (toScreen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right;
                }

                else
                {
                    offset_y = toScreen.Geometry.OffsetY;
                    
                    if (window.IsSnappedRight)
                        offset_x = toScreen.Geometry.OffsetX + toScreen.Geometry.Width / 2;
                    else
                        offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top;
                    size_x = (toScreen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right;
                }
            }

            await IceWM.Restore();
            await IceWM.SizeTo($"{size_x}", $"{size_y}");
            await IceWM.Move(offset_x, offset_y);
        }

        public static async Task SnapDown()
        {
            int offset_x;
            int offset_y;
            int size_y;
            int size_x;
            Screen toScreen;

            var window = await IceWM.GetFocusedWindow();
            var screens = await IceWM.GetScreens();

            if (window.IsSnappedBottom)
            {
                if (!window.CanSnapBottom)
                    return;

                var screen = screens
                    .Where(s => s.Geometry.OffsetX == window.Screen.Geometry.OffsetX
                        && s.Geometry.OffsetY > window.Screen.Geometry.Height)
                    .FirstOrDefault();

                if (screen == null)
                    return;

                toScreen = screen;

                if (toScreen.Taskbar != null)
                {
                    offset_y = toScreen.Geometry.OffsetY;
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top
                        - (window.Screen.Taskbar.OnBottom ? window.Screen.Taskbar.Geometry.Height : 0);
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }

                else
                {
                    offset_y = toScreen.Geometry.OffsetY;
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top;
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }
            }

            else if (!window.IsSnappedLeft && !window.IsSnappedRight)
            {
                if (!window.CanSnapBottom)
                    return;

                toScreen = window.Screen;

                if (toScreen.Taskbar != null)
                {
                    offset_y = toScreen.Geometry.Height / 2;
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top
                        - (window.Screen.Taskbar.OnBottom ? window.Screen.Taskbar.Geometry.Height : 0);
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }

                else
                {
                    offset_y = toScreen.Geometry.Height / 2;
                    offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top;
                    size_x = toScreen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right;
                }
            }

            else
            {
                toScreen = window.Screen;

                if (toScreen.Taskbar != null)
                {
                    offset_y = toScreen.Geometry.Height / 2;
                    
                    if (window.IsSnappedRight)
                        offset_x = toScreen.Geometry.OffsetX + toScreen.Geometry.Width / 2;
                    else
                        offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top
                        - (window.Screen.Taskbar.OnBottom ? window.Screen.Taskbar.Geometry.Height : 0);
                    size_x = (toScreen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right;
                }

                else
                {
                    offset_y = toScreen.Geometry.Height / 2;
                    
                    if (window.IsSnappedRight)
                        offset_x = toScreen.Geometry.OffsetX + toScreen.Geometry.Width / 2;
                    else
                        offset_x = toScreen.Geometry.OffsetX;

                    size_y = (toScreen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top;
                    size_x = (toScreen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right;
                }
            }

            await IceWM.Restore();
            await IceWM.SizeTo($"{size_x}", $"{size_y}");
            await IceWM.Move(offset_x, offset_y);
        }
    }
}
