using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace DDOLayoutEditor
{
    internal static class Helper
    {
        private static readonly DependencyProperty FullScreenProperty = DependencyProperty.RegisterAttached("FullScreenProperty", typeof(MyWindowState), typeof(Window));
        private static int _lastIndex = 0;

        private class MyWindowState
        {
            public bool Maximized;

            public WindowState PrevState;
            public WindowStyle PrevStyle;
            public ResizeMode PrevResizeMode;
        }

        public enum SnapMode
        {
            Normal,
            Decrease,
            Increase
        }

        public static double SnapX(double x, SnapMode mode = SnapMode.Normal)
        {
            switch (mode)
            {
                case SnapMode.Normal:
                    return (int)(Math.Round((double)x / Settings.Snap.X, MidpointRounding.AwayFromZero) * Settings.Snap.X);
                case SnapMode.Decrease:
                    return (int)(Math.Floor((double)x / Settings.Snap.X) * Settings.Snap.X);
                case SnapMode.Increase:
                    return (int)(Math.Ceiling((double)x / Settings.Snap.X) * Settings.Snap.X);
            }

            throw new ArgumentException("SnapMode is not supported.");
        }

        public static double SnapY(double y, SnapMode mode = SnapMode.Normal)
        {
            switch (mode)
            {
                case SnapMode.Normal:
                    return (int)(Math.Round((double)y / Settings.Snap.Y, MidpointRounding.AwayFromZero) * Settings.Snap.Y);
                case SnapMode.Decrease:
                    return (int)(Math.Floor((double)y / Settings.Snap.Y) * Settings.Snap.Y);
                case SnapMode.Increase:
                    return (int)(Math.Ceiling((double)y / Settings.Snap.Y) * Settings.Snap.Y);
            }

            throw new ArgumentException("SnapMode is not supported.");
        }

        public static double BoundX(double x, FrameworkElement element)
        {
            if (x < 0)
                return 0;

            var parentWidth = ((FrameworkElement)element.Parent).ActualWidth;
            var right = (x + element.ActualWidth);

            if (right > parentWidth)
            {
                var over = (right - parentWidth);
                x -= over;
            }

            return x;
        }

        public static double BoundY(double y, FrameworkElement element)
        {
            if (y < 0)
                return 0;

            var parentHeight = ((FrameworkElement)element.Parent).ActualHeight;
            var bottom = (y + element.ActualHeight);

            if (bottom > parentHeight)
            {
                var over = (bottom - parentHeight);
                y -= over;
            }

            return y;
        }

        public static Point SnapPoint(Point referencePoint, double referenceWidth = 0)
        {
            var point = new Point(referencePoint.X, referencePoint.Y);

            bool controlDown = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            bool shiftDown = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

            if (!controlDown)
            {
                point.X = Helper.SnapX(point.X);
                point.Y = Helper.SnapY(point.Y);

                // If shift is pressed, don't snap based on the left top corner, snap based on the right top corner
                if (shiftDown && (referenceWidth > 0))
                {
                    // Point now contains where the top left should be, so add the ActualWidth to find where the top right is
                    double r = (point.X + referenceWidth);

                    // Ok, now let's find the closest point to this point
                    double k = (Math.Round((double)r / Settings.Snap.X, MidpointRounding.AwayFromZero) * Settings.Snap.X);

                    // Ok, that's the top right, now subtract the width so we can go back to the top left
                    point.X = (int)(k - referenceWidth);
                }
            }

            return point;
        }

        public static string GetDDOFolder(string subfolder = null)
        {
            var dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Dungeons and Dragons Online");
            if (subfolder != null)
                dir = System.IO.Path.Combine(dir, subfolder);

            // ui\layouts

            // Spin up if for some reason the folder doesn't exist
            while (!System.IO.Directory.Exists(dir))
                dir = System.IO.Directory.GetParent(dir).FullName;

            return dir;
        }

        public static Color GetColor(string htmlColorCode)
        {
            var tempColor = System.Drawing.ColorTranslator.FromHtml(htmlColorCode);
            return new Color { A = 255, R = tempColor.R, G = tempColor.G, B = tempColor.B }; 
        }

        public static string GetColorCode(Color color)
        {
            return String.Format("#{0:X2}{1:X2}{2:X2}", color.R, color.G, color.B);
        }

        public static string GetValue(XmlNode node, string attributeName, string defaultValue = null)
        {
            var attr = node.Attributes[attributeName];
            if (attr != null)
                return attr.Value;

            return defaultValue;
        }

        public static void BringToFront(FrameworkElement elem)
        {
            Canvas.SetZIndex(elem, ++_lastIndex);
        }

        public static Brush CreateGridBrush(Rect bounds, Size tileSize)
        {
            var gridColor = Brushes.Black;
            var gridThickness = 1.0;
            var tileRect = new Rect(tileSize);

            var gridTile = new DrawingBrush
            {
                Stretch = Stretch.None,
                TileMode = TileMode.Tile,
                Viewport = tileRect,
                ViewportUnits = BrushMappingMode.Absolute,
                Drawing = new GeometryDrawing
                {
                    Pen = new Pen(gridColor, gridThickness),
                    Geometry = new GeometryGroup
                    {
                        Children = new GeometryCollection
                {
                    new LineGeometry(tileRect.TopLeft, tileRect.BottomRight),
                    new LineGeometry(tileRect.BottomLeft, tileRect.TopRight)
                }
                    }
                }
            };

            var offsetGrid = new DrawingBrush
            {
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top,
                Transform = new TranslateTransform(bounds.Left, bounds.Top),
                Drawing = new GeometryDrawing
                {
                    Geometry = new RectangleGeometry(new Rect(bounds.Size)),
                    Brush = gridTile
                }
            };

            return offsetGrid;
        }

        public static void ToggleFullScreen(Window window)
        {
            var state = (MyWindowState)window.GetValue(FullScreenProperty);
            if (state == null)
            {
                state = new MyWindowState();
                window.SetValue(FullScreenProperty, state);
            }

            if (state.Maximized)
            {
                state.Maximized = false;

                // Restore the previous state
                window.WindowStyle = state.PrevStyle;

                //Topmost = false;
                window.WindowState = state.PrevState;
                window.ResizeMode = state.PrevResizeMode;
            }
            else
            {
                state.Maximized = true;

                // Store the previous state
                state.PrevState = window.WindowState;
                state.PrevStyle = window.WindowStyle;
                state.PrevResizeMode = window.ResizeMode;

                // Max it!
                window.WindowStyle = WindowStyle.None;

                //Topmost = true;
                window.WindowState = WindowState.Maximized;
                window.ResizeMode = ResizeMode.NoResize;
            }
        }

        public static BitmapImage GetImage(string path)
        {
            if (System.IO.File.Exists(path))
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(path);
                image.EndInit();

                return image;
            }

            return null;
        }

        // http://siderite.blogspot.com/2010/07/openfiledialog-image-filter-string.html
        public static string GetImageFilter() 
        { 
            StringBuilder sb = new StringBuilder(); 
            
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders(); 
            foreach (ImageCodecInfo info in codecs) 
            { 
                if (sb.Length > 0)
                    sb.Append(";"); 
                
                sb.Append(info.FilenameExtension.ToLower()); 
            } 
            
            return sb.ToString(); 
        }

        public static KeyValuePair<string, string> GetPair(string key, string value)
        {
            return new KeyValuePair<string, string>(key, value);
        }

        public static void SetValues(XmlDocument doc, string nodePath, params KeyValuePair<string, string>[] args)
        {
            XmlNode node = doc.SelectSingleNode(nodePath);

            foreach (KeyValuePair<string, string> v in args)
            {
                var attr = node.Attributes[v.Key];
                if (attr == null)
                {
                    attr = doc.CreateAttribute(v.Key);
                    node.Attributes.Append(attr);
                }

                attr.Value = v.Value;
            }
        }
    }
}
