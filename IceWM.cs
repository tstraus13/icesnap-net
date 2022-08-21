using System.Collections.Generic;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

namespace IceSnap
{
    ///
    /// IceWM Interface
    ///
    /// This can probably be enhanced but for
    /// simple windows snapping it will only
    /// incorporating a small peice of icesh.
    ///
    public static class IceWM
    {

        public static async Task Restore()
        {
            var args = new List<string>() { "-f", "restore" };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Restore command to focused window!");
        }

        public static async Task Move(int x, int y)
        {
            var args = new List<string>() { "-f", "move", x.ToString(), y.ToString() };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Move command to focused window!");
        }

        public static async Task SizeTo(string width, string height)
        {
            var args = new List<string>() { "-f", "sizeto", width, height };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing SizeTo command to focused window!");
        }

        public static async Task VerticalMax()
        {
            var args = new List<string>() { "-f", "vertical" };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Vertical command to focused window!");
        }

        public static async Task HorizontalMax()
        {
            var args = new List<string>() { "-f", "horizontal" };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Horizontal command to focused window!");
        }

        public static async Task Maximize()
        {
            await HorizontalMax();
            await VerticalMax();
        }

        public static async Task<Geometry> GetWindowGeometry()
        {
            var geometry = new Geometry();

            var args = new List<string>() { "-f", "getGeometry" };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Geometry command to focused window!");

            var geo = result.StdOut
                .Replace('x', '|')
                .Replace('+', '|')
                .Split('|');

            int.TryParse(geo[0], out geometry.Width);
            int.TryParse(geo[1], out geometry.Height);
            int.TryParse(geo[2], out geometry.OffsetX);
            int.TryParse(geo[3], out geometry.OffsetY);

            return geometry;
        }

        public static async Task<WindowProperties> GetWindowProperties(bool snappingUp = false)
        {
            var properties = new WindowProperties();
            var wasMaximized = false;

            properties.Geometry = await GetWindowGeometry();

            var args = new List<string>() { "-f", "properties" };

            var pInfo = SetupCommand(args);
            var result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Properties command to focused window!");

            var state = Regex.Match(result.StdOut, @"_NET_WM_STATE =(.+)");
            
            var stateValues = state
                .Groups[1].Value
                .Trim()
                .Replace(" ", "")
                .Split(",")
                .ToList();

            properties.State.MaximizedHorizontal = stateValues.Contains("_NET_WM_STATE_MAXIMIZED_HORZ");
            properties.State.MaximizedVertical = stateValues.Contains("_NET_WM_STATE_MAXIMIZED_VERT");

            // We need to get the state first because if the window is maximized
            // some WMs, like IceWM, provide the option to remove borders (left, right, and bottom) when Maximized.
            // We will need to restore to get the borders as the borders will exist when we snap
            if (properties.State.IsMaximized && !snappingUp)
            {
                await Restore();
                wasMaximized = true;
            }

            result = await ProcessAsync.RunAsync(pInfo);

            if (result.ExitCode != 0)
                throw new Exception("ERROR: Problem issuing Properties command to focused window!");

            var frame = Regex.Match(result.StdOut, @"_NET_FRAME_EXTENTS =(.+)");
            var frameValues = frame.Groups[1].Value.Trim().Replace(" ", "").Split(",");

            int.TryParse(frameValues[0], out properties.Border.Left);
            int.TryParse(frameValues[1], out properties.Border.Right);
            int.TryParse(frameValues[2], out properties.Border.Top);
            int.TryParse(frameValues[3], out properties.Border.Bottom);

            if (wasMaximized)
                await Maximize();

            return properties;
        }

        public static async Task<FocusedWindow> GetFocusedWindow(bool snappingUp = false)
        {
            var window = new FocusedWindow();
            var screens = await GetScreens();

            window.Properties = await GetWindowProperties(snappingUp);

            window.Screen = screens.Where(s => s.Geometry.OffsetX <= window.Properties.Geometry.OffsetX 
                && window.Properties.Geometry.OffsetX < (s.Geometry.OffsetX + s.Geometry.Width)
                && s.Geometry.OffsetY <= window.Properties.Geometry.OffsetX 
                && window.Properties.Geometry.OffsetX < (s.Geometry.OffsetX + s.Geometry.Width))
                .FirstOrDefault();

            window.IsSnappedLeft = window.Properties.State.MaximizedVertical
                && window.Properties.Geometry.Width == (window.Screen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right
                && window.Properties.Geometry.OffsetX == window.Screen.Geometry.OffsetX + window.Properties.Border.Left;

            if (window.IsSnappedLeft)
                window.CanSnapLeft = window.Properties.Geometry.OffsetX != window.Properties.Border.Left;
            else
                window.CanSnapLeft = true;

            window.IsSnappedRight = window.Properties.State.MaximizedVertical 
                && window.Properties.Geometry.Width == (window.Screen.Geometry.Width / 2) - window.Properties.Border.Left - window.Properties.Border.Right
                && window.Properties.Geometry.OffsetX == (window.Screen.Geometry.OffsetX + (window.Screen.Geometry.Width / 2)) + window.Properties.Border.Right;

            if (window.IsSnappedRight)
            {
                var maxXOffset = screens.Max(s => s.Geometry.OffsetX);
                var screen = screens.Where(s => s.Geometry.OffsetX == maxXOffset).FirstOrDefault();
                
                window.CanSnapRight = window.Properties.Geometry.OffsetX != (screen.Geometry.OffsetX + (screen.Geometry.Width / 2)) - window.Properties.Border.Right;
            }
            else
                window.CanSnapRight = true;

            window.IsSnappedBottom = window.Properties.Geometry.Width == window.Screen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right
                && window.Properties.Geometry.Height == 
                    (window.Screen.Taskbar != null && window.Screen.Taskbar.OnBottom ? ((window.Screen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top - window.Screen.Taskbar.Geometry.Height)
                    : ((window.Screen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top))
                && window.Properties.Geometry.OffsetX == window.Screen.Geometry.OffsetX + window.Properties.Border.Left
                && window.Properties.Geometry.OffsetY == window.Screen.Geometry.OffsetY + (window.Screen.Geometry.Height / 2) + window.Properties.Border.Top;

            if (window.IsSnappedBottom)
                window.CanSnapBottom = screens.Where(s => s.Geometry.OffsetX == window.Screen.Geometry.OffsetX 
                    && s.Geometry.OffsetY > window.Screen.Geometry.Height).Any();
            else
                window.CanSnapBottom = true;

            window.IsSnappedTop = (window.Properties.Geometry.Width == window.Screen.Geometry.Width - window.Properties.Border.Left - window.Properties.Border.Right
                && window.Properties.Geometry.Height == 
                    (window.Screen.Taskbar != null && window.Screen.Taskbar.OnTop ? ((window.Screen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top - window.Screen.Taskbar.Geometry.Height)
                    : ((window.Screen.Geometry.Height / 2) - window.Properties.Border.Bottom - window.Properties.Border.Top))
                && window.Properties.Geometry.OffsetX == window.Screen.Geometry.OffsetX + window.Properties.Border.Left
                && window.Properties.Geometry.OffsetY == window.Screen.Geometry.OffsetY + window.Properties.Border.Top)
                || window.Properties.State.IsMaximized;

            if (window.IsSnappedTop)
                window.CanSnapTop = screens.Where(s => s.Geometry.OffsetX == window.Screen.Geometry.OffsetX 
                    && s.Geometry.OffsetY < window.Screen.Geometry.OffsetY).Any();
            else
                window.CanSnapTop = true;

            return window;
        }

        public static async Task<List<Screen>> GetScreens()
        {
            var screens = new List<Screen>();

            var args = new List<string>() { "-f", "xinerama" };
            var pInfo = SetupCommand(args);
            var sresult = await ProcessAsync.RunAsync(pInfo);

            if (sresult.ExitCode != 0)
                throw new Exception("ERROR: Problem getting Screen Info!");

            
            Taskbar taskbar = new Taskbar();
            args = new List<string>() { "-T", "getGeometry" };
            pInfo = SetupCommand(args);
            var tresult = await ProcessAsync.RunAsync(pInfo);

            if (tresult.ExitCode != 0)
                throw new Exception("ERROR: Problem getting Taskbar Info!");

            var tbdata = tresult.StdOut.Trim().Replace("x", "|").Replace("+", "|").Split("|");
            
            int.TryParse(tbdata[0], out taskbar.Geometry.Width);
            int.TryParse(tbdata[1], out taskbar.Geometry.Height);
            int.TryParse(tbdata[2], out taskbar.Geometry.OffsetX);
            int.TryParse(tbdata[3], out taskbar.Geometry.OffsetY);

            var lines = sresult.StdOut.Split(Environment.NewLine)
                .ToList();
            lines.RemoveAll(s => string.IsNullOrEmpty(s));

            foreach (var line in lines)
            {
                var screen = new Screen();

                var data = line.Replace(" ", "")
                    .Replace(':', '|')
                    .Replace('x','|')
                    .Replace('+', '|')
                    .Split('|');

                int.TryParse(data[0], out screen.Identity);
                int.TryParse(data[1], out screen.Geometry.Width);
                int.TryParse(data[2], out screen.Geometry.Height);
                int.TryParse(data[3], out screen.Geometry.OffsetX);
                int.TryParse(data[4], out screen.Geometry.OffsetY);

                if (screen.Geometry.OffsetX == taskbar.Geometry.OffsetX)
                    screen.Taskbar = taskbar;

                screens.Add(screen);
            }

            return screens;
        }

        private static ProcessStartInfo SetupCommand(string argument)
        {
            var pInfo = new ProcessStartInfo("icesh", argument.Trim())
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            return pInfo;
        }

        private static ProcessStartInfo SetupCommand(List<string> arguments)
        {
            string combined = "";

            foreach (string arg in arguments)
            {
                combined = $"{combined} {arg}";
            }

            var pInfo = new ProcessStartInfo("icesh", combined.Trim())
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            return pInfo;
        }
    }

    public class FocusedWindow
    {
        public Screen Screen;
        public WindowProperties Properties;
        public bool IsSnappedQuarter;
        public bool IsSnappedTop;
        public bool CanSnapTop;
        public bool IsSnappedBottom;
        public bool CanSnapBottom;
        public bool IsSnappedLeft;
        public bool CanSnapLeft;
        public bool IsSnappedRight;
        public bool CanSnapRight;

        public FocusedWindow()
        {
            Screen = new Screen();
            Properties = new WindowProperties();
        }
    }

    public class Screen
    {
        public int Identity;

        public Taskbar Taskbar;

        public Geometry Geometry;

        public Screen()
        {
            Geometry = new Geometry();
        }
    }

    public class WindowProperties
    {
        public NetState State;

        public ThemeBorder Border;

        public Geometry Geometry;

        public WindowProperties()
        {
            State = new NetState();
            Border = new ThemeBorder();
            Geometry = new Geometry();
        }
    }

    public class Taskbar
    {
        public Geometry Geometry;

        public bool OnTop
        {
            get
            {
                if (Geometry.OffsetY == 0)
                    return true;
                else
                    return false;
            }
        }

        public bool OnBottom
        {
            get
            {
                if (!OnTop)
                    return true;
                else
                    return false;
            }
        }

        public Taskbar()
        {
            Geometry = new Geometry();
        }
    }

    public class ThemeBorder
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    public class NetState
    {
        public bool MaximizedHorizontal;
        public bool MaximizedVertical;

        public bool IsMaximized
        {
            get
            {
                return MaximizedVertical && MaximizedHorizontal;
            }
        }   
    }

    public class Geometry
    {
        public int Width;
        public int Height;

        public int OffsetX;
        public int OffsetY;
    }
}