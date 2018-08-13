using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace DDOLayoutEditor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // The currently opened layout file
        private string _fileName;

        // Hierarchial Grouping of ItemControls in a group
        private List<Group> _groups;

        // Flat list of all ItemControls
        private List<ItemControl> _items;

        // Flag to see if the layout has been updated at least once.
        // Needed since we do some stuff with ActualWidth/Height and need it calculated
        private bool _layoutUpdated;

        // Hack used for resizing the window to our content size (yes, could have used size to content, but we still want to be able to resize the main window too)
        private double _windowHeightOffset;
        private double _windowWidthOffset;

        // Background brushes
        private ImageBrush _backgroundImageBrush;
        private SolidColorBrush _backgroundColorBrush;

        // The base title before we started modifying it
        private string _baseTitle;

        [Flags]
        private enum SnapLinePositions
        {
            Top = 0x1,
            Left = 0x2,
            Bottom = 0x4,
            Right = 0x8
        }

        public MainWindow()
        {
            InitializeComponent();

            _baseTitle = Title;

            ResizingAdorner.ResizeEvent += ResizingAdorner_ResizeEvent;
            ResizingAdorner.MouseEvent += ResizingAdorner_MouseEvent;

            Settings.SettingsChanged += Settings_SettingsChanged;
            saveLabel.Visibility = System.Windows.Visibility.Hidden;
        }

        private void Settings_SettingsChanged(SettingsChangedArgs e)
        {
            ApplySettings();
        }

        private void ApplySettings()
        {
            #region Fix Background Brush (Grid)

            DrawingBrush drawingBrush = Resources["GridBrush"] as DrawingBrush;
            drawingBrush.Opacity = Settings.Grid.Opacity;

            // Probably some better WPF way of doing this...oh well
            if (Settings.Grid.Width > 0 && Settings.Grid.Height > 0)
            {
                double widthOffset = 1.0 / Settings.Grid.Width;
                double heightOffset = 1.0 / Settings.Grid.Height;

                Brush lineBrush = BrushCache.GetBrush(Settings.Grid.LineColor);
                drawingBrush.Viewport = new Rect(new Size(Settings.Grid.Width, Settings.Grid.Height));

                DrawingGroup drawingGroup = drawingBrush.Drawing as DrawingGroup;
                drawingGroup.Children.Clear();

                drawingGroup.Children.Add(new GeometryDrawing
                {
                    Geometry = Geometry.Parse(String.Format("M0,0 L1,0 1,{0}, 0,{0}Z", heightOffset)),
                    Brush = lineBrush
                });

                drawingGroup.Children.Add(new GeometryDrawing
                {
                    Geometry = Geometry.Parse(String.Format("M0,0 L0,1 {0},1, {0},0Z", widthOffset)),
                    Brush = lineBrush
                });
            }

            #endregion

            _backgroundColorBrush = BrushCache.GetBrush(Settings.BackgroundColor);
            _backgroundImageBrush = null;

            if (Settings.BackgroundImagePath != null)
            {
                var image = Helper.GetImage(Settings.BackgroundImagePath);
                if (image != null)
                    _backgroundImageBrush = new ImageBrush(image);
            }

            backgroundButton.Visibility = _backgroundImageBrush != null ? Visibility.Visible : Visibility.Collapsed;

            Height = Settings.Layout.Height + _windowHeightOffset;
            Width = Settings.Layout.Width + _windowWidthOffset;

            ShowGrid(Settings.Grid.Visible);
            ShowBackground(Settings.ShowBackgroundImage);

            UpdateTitle();
        }

        private void UpdateTitle()
        {
            if (_selection != null)
            {
                Title = String.Format(
                    "{0}, Window: {1}x{2}, Size: {3}x{4}, Location: {5}, {6}",
                    _baseTitle,
                    mainCanvas.ActualWidth, mainCanvas.ActualHeight,
                    _selection.ActualWidth, _selection.ActualHeight,
                    Canvas.GetLeft(_selection), Canvas.GetTop(_selection)
                );
            }
            else
            {
                Title = String.Format("{0}, Window: {1}x{2}", _baseTitle, mainCanvas.ActualWidth, mainCanvas.ActualHeight);
            }
        }

        private void Window_Initialized_1(object sender, EventArgs e)
        {
            // Force these guys to the top
            Canvas.SetZIndex(lineX, Int32.MaxValue - 1);
            Canvas.SetZIndex(lineY, Int32.MaxValue - 1);

            // Load our settings
            Settings.Load();
        }

        private void Window_PreviewKeyDown_1(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Alt)
            {
                Key key = (e.Key == Key.System ? e.SystemKey : e.Key);

                if (key == Key.Enter)
                {
                    e.Handled = true;
                    Helper.ToggleFullScreen(this);
                }
            }
            else
            {
                if (_isItemSelected && (_selection != null))
                {
                    // If snapping is disabled or the control key is pressed, move 1px at a time
                    if (!Settings.Snap.Enabled || ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))))
                    {
                        #region Move by a single pixel

                        switch (e.Key)
                        {
                            case Key.Up:
                                Canvas.SetTop(_selection, Helper.BoundY(Canvas.GetTop(_selection) - 1, _selection));
                                break;
                            case Key.Down:
                                Canvas.SetTop(_selection, Helper.BoundY(Canvas.GetTop(_selection) + 1, _selection));
                                break;
                            case Key.Left:
                                Canvas.SetLeft(_selection, Helper.BoundX(Canvas.GetLeft(_selection) - 1, _selection));
                                break;
                            case Key.Right:
                                Canvas.SetLeft(_selection, Helper.BoundX(Canvas.GetLeft(_selection) + 1, _selection));
                                break;
                        }

                        #endregion
                    }
                    else
                    {
                        #region Move by Snaps

                        switch (e.Key)
                        {
                            case Key.Up:
                                Canvas.SetTop(_selection, Helper.BoundY(Helper.SnapY(Canvas.GetTop(_selection) - 1, Helper.SnapMode.Decrease), _selection));
                                break;
                            case Key.Down:
                                Canvas.SetTop(_selection, Helper.BoundY(Helper.SnapY(Canvas.GetTop(_selection) + 1, Helper.SnapMode.Increase), _selection));
                                break;
                            case Key.Left:
                                Canvas.SetLeft(_selection, Helper.BoundX(Helper.SnapX(Canvas.GetLeft(_selection) - 1, Helper.SnapMode.Decrease), _selection));
                                break;
                            case Key.Right:
                                Canvas.SetLeft(_selection, Helper.BoundX(Helper.SnapX(Canvas.GetLeft(_selection) + 1, Helper.SnapMode.Increase), _selection));
                                break;
                        }

                        #endregion                        
                    }

                    UpdateTitle();
                    e.Handled = true;
                }
            }
        }

        private void Window_LayoutUpdated_1(object sender, EventArgs e)
        {
            // If I put this in Loaded event, I get a resize flicker.
            if (_layoutUpdated == false)
            {
                _windowHeightOffset = (ActualHeight - mainCanvas.ActualHeight);
                _windowWidthOffset = (ActualWidth - mainCanvas.ActualWidth);

                // Apply the non-category settings
                ApplySettings();

                // Since I am resizing the application based on the mainCanvas and I need to wait until a layout event has fired, otherwise
                // I can't get Actual dimensions, I can't rely on the startup location of the window, thus we have to manully center it on the screen.
                CenterWindowOnScreen();

                // We only need to do this once on startup, so flip the flag to keep us from doing it again
                _layoutUpdated = true;
            }
        }

        private void CenterWindowOnScreen()
        {
            double screenWidth = System.Windows.SystemParameters.WorkArea.Width;
            double screenHeight = System.Windows.SystemParameters.WorkArea.Height;

            double windowWidth = this.ActualWidth;
            double windowHeight = this.ActualHeight;

            this.Left = (screenWidth / 2) - (windowWidth / 2);
            this.Top = (screenHeight / 2) - (windowHeight / 2);
        }

        private void mainCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            lineX.Y1 = mainCanvas.ActualHeight;
            lineY.X2 = mainCanvas.ActualWidth;
        }

        private void mainCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            lineX.Y1 = mainCanvas.ActualHeight;
            lineY.X2 = mainCanvas.ActualWidth;

            UpdateTitle();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            var sb = ((System.Windows.Media.Animation.Storyboard)FindResource("Animate"));
            
            sb.Stop();
            saveLabel.Visibility = System.Windows.Visibility.Hidden;

            FileParser.Save(_fileName, _groups);

            sb.Seek(TimeSpan.Zero);
            sb.Begin(saveLabel);
        }

        private void openButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".layout";
            dlg.Filter = "Layout files (.layout)|*.layout";
            dlg.InitialDirectory = Helper.GetDDOFolder(@"ui\layouts");
            dlg.CheckFileExists = true;

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                StopDragging();
                RemoveSelector();

                CloseCategoriesWindow();

                if (_groups != null)
                {
                    foreach (Group group in _groups)
                        if (group.Items != null)
                            foreach (ItemControl item in group.Items)
                                mainCanvas.Children.Remove(item);

                    _items.Clear();
                    _groups.Clear();
                }

                if (System.IO.File.Exists(dlg.FileName))
                {
                    _fileName = dlg.FileName;

                    _groups = FileParser.Load(_fileName);
                    _items = new List<ItemControl>();

                    foreach (Group group in _groups)
                    {
                        foreach (ItemControl item in group.Items)
                        {
                            _items.Add(item);
                            mainCanvas.Children.Add(item);
                        }
                    }
                    
                    saveButton.IsEnabled = true;
                    categoriesButton.IsEnabled = true;

                    ShowCategoriesWindow();
                }
            }
        }

        private void settingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (windowSettings.WindowState != Xceed.Wpf.Toolkit.WindowState.Open)
            {
                // We don't want this opened
                CloseCategoriesWindow();

                windowSettings.ConfigureUI();
                windowSettings.Show();
            }
        }

        private void categoriesButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCategoriesWindow();
        }

        private void gridButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.Grid.Visible = !Settings.Grid.Visible;
            ShowGrid(Settings.Grid.Visible);
        }

        private void backgroundButton_Click(object sender, RoutedEventArgs e)
        {
            Settings.ShowBackgroundImage = !Settings.ShowBackgroundImage;
            ShowBackground(Settings.ShowBackgroundImage);
        }

        private void CloseCategoriesWindow()
        {
            if (windowCategories.WindowState == Xceed.Wpf.Toolkit.WindowState.Open)
                windowCategories.Close();
        }

        private void ShowCategoriesWindow()
        {
            if (windowCategories.WindowState != Xceed.Wpf.Toolkit.WindowState.Open)
            {
                if (_groups != null && _groups.Count > 0)
                {
                    windowCategories.SetData(_groups);

                    windowCategories.ConfigureUI();
                    windowCategories.Show();
                }
            }
        }

        private void ShowGrid(bool show)
        {
            if (show)
            {
                gridButton.Content = "Grid ON";
                mainCanvas.Background = Resources["GridBrush"] as Brush;
            }
            else
            {
                gridButton.Content = "Grid OFF";
                mainCanvas.Background = Brushes.Transparent;
            }
        }

        private void ShowBackground(bool show)
        {
            if (_backgroundImageBrush == null)
            {
                Background = _backgroundColorBrush;
                return;
            }

            if (show)
            {
                backgroundButton.Content = "Background ON";
                Background = _backgroundImageBrush;
            }
            else
            {
                backgroundButton.Content = "Background OFF";
                Background = _backgroundColorBrush;
            }
        }

        #region Resizing / Moving Elements

        ItemControl _selection;
        AdornerLayer _adornerLayer;

        Point _originalMousePosition;
        Point _originalElementPosition;

        bool _isMousePressed;
        bool _isMouseDragging;
        bool _isItemSelected;
        
        private void StopDragging()
        {
            if (_isMousePressed)
            {
                _isMousePressed = false;
                _isMouseDragging = false;

                HideSnapLine();
            }
        }

        private void RemoveSelector()
        {
            if (_isItemSelected)
            {
                _isItemSelected = false;

                if (_selection != null)
                {
                    if (_adornerLayer != null)
                    {
                        var adorners = _adornerLayer.GetAdorners(_selection);
                        if (adorners != null && adorners.Length > 0)
                            _adornerLayer.Remove(adorners[0]);
                    }

                    _selection = null;
                    UpdateTitle();
                }
            } 
        }

        private void UpdateSnapLine(SnapLinePositions positions, FrameworkElement elem)
        {
            double? x = null;
            double? y = null;

            if ((positions & SnapLinePositions.Top) == SnapLinePositions.Top)
                y = Canvas.GetTop(elem);

            if ((positions & SnapLinePositions.Bottom) == SnapLinePositions.Bottom)
                y = Canvas.GetTop(elem) + elem.ActualHeight;

            if ((positions & SnapLinePositions.Left) == SnapLinePositions.Left)
                x = Canvas.GetLeft(elem);

            if ((positions & SnapLinePositions.Right) == SnapLinePositions.Right)
                x = Canvas.GetLeft(elem) + elem.ActualWidth;

            if (x.HasValue && y.HasValue)
            {
                lineX.X1 = lineX.X2 = x.Value;
                lineY.Y1 = lineY.Y2 = y.Value;
            }
        }

        private void ShowSnapLine()
        {
            if (lineX.IsVisible)
                return;

            lineX.Visibility = Visibility.Visible;
            lineY.Visibility = Visibility.Visible;
        }

        private void HideSnapLine()
        {
            if (!lineX.IsVisible)
                return;

            lineX.Visibility = Visibility.Collapsed;
            lineY.Visibility = Visibility.Collapsed;
        }

        private void ResizingAdorner_MouseEvent(MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Released)
                HideSnapLine();
        }

        private void ResizingAdorner_ResizeEvent(ResizeArgs e)
        {
            Point referencePoint = Mouse.GetPosition(null);
            Point point = Settings.Snap.Enabled ? Helper.SnapPoint(referencePoint) : referencePoint;

            var xDiff = referencePoint.X - point.X;
            var yDiff = referencePoint.Y - point.Y;

            var item = e.Element as ItemControl;

            switch (e.ResizeType)
            {
                case ResizeTypes.TopLeft:
                    {
                        #region Calc

                        double newX = e.Position.X - xDiff;
                        double newY = e.Position.Y - yDiff;

                        double newWidth = Math.Max(0, e.Size.Width + xDiff);
                        double newHeight = Math.Max(0, e.Size.Height + yDiff);

                        if (newWidth < item.MinWidth)
                        {
                            newX -= (item.MinWidth - newWidth);
                            newWidth = item.MinWidth;
                        }

                        if (newHeight < item.MinHeight)
                        {
                            newY -= (item.MinHeight - newHeight);
                            newHeight = item.MinHeight;
                        }
                        
                        e.Position = new Point(newX, newY);
                        e.Size = new Size(newWidth, newHeight);

                        UpdateSnapLine((SnapLinePositions.Top | SnapLinePositions.Left), e.Element);

                        #endregion
                    }
                    break;
                case ResizeTypes.TopRight:
                    {
                        #region Calc

                        double newY = e.Position.Y - yDiff;

                        double newWidth = Math.Max(0, e.Size.Width + xDiff);
                        double newHeight = Math.Max(0, e.Size.Height + yDiff);

                        if (newWidth < item.MinWidth)
                        {
                            newWidth = item.MinWidth;
                        }

                        if (newHeight < item.MinHeight)
                        {
                            newY -= (item.MinHeight - newHeight);
                            newHeight = item.MinHeight;
                        }

                        e.Position = new Point(e.Position.X, newY);
                        e.Size = new Size(newWidth, newHeight);

                        UpdateSnapLine((SnapLinePositions.Top | SnapLinePositions.Right), e.Element);

                        #endregion
                    }
                    break;
                case ResizeTypes.BottomLeft:
                    {
                        #region Calc

                        double newX = e.Position.X - xDiff;

                        double newWidth = Math.Max(0, e.Size.Width + xDiff);
                        double newHeight = Math.Max(0, e.Size.Height - yDiff);

                        if (newWidth < item.MinWidth)
                        {
                            newX -= (item.MinWidth - newWidth);
                            newWidth = item.MinWidth;
                        }

                        if (newHeight < item.MinHeight)
                        {
                            newHeight = item.MinHeight;
                        }

                        e.Position = new Point(newX, e.Position.Y);
                        e.Size = new Size(newWidth, newHeight);

                        UpdateSnapLine((SnapLinePositions.Bottom | SnapLinePositions.Left), e.Element);

                        #endregion
                    }
                    break;
                case ResizeTypes.BottomRight:
                    {
                        #region Calc

                        double newWidth = Math.Max(0, e.Size.Width - xDiff);
                        double newHeight = Math.Max(0, e.Size.Height - yDiff);

                        if (newWidth < item.MinWidth)
                            newWidth = item.MinWidth;

                        if (newHeight < item.MinHeight)
                            newHeight = item.MinHeight;

                        e.Size = new Size(newWidth, newHeight);

                        UpdateSnapLine((SnapLinePositions.Bottom | SnapLinePositions.Right), e.Element);

                        #endregion
                    }
                    break;
            }

            ShowSnapLine();
            UpdateTitle();
        }

        private void Window_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
        {
            RemoveSelector();
        }

        private void Window_MouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            StopDragging();
        }

        private void Window_MouseMove_1(object sender, MouseEventArgs e)
        {
            if (_isMousePressed)
            {
                Point p = e.GetPosition(mainCanvas);

                // Use the system parameters to determine when to flag dragging
                if (!_isMouseDragging && ((Math.Abs(p.X - _originalMousePosition.X) > SystemParameters.MinimumHorizontalDragDistance) || (Math.Abs(p.Y - _originalMousePosition.Y) > SystemParameters.MinimumVerticalDragDistance)))
                    _isMouseDragging = true;

                if (_isMouseDragging)
                {
                    Point point = new Point((int)(p.X - (_originalMousePosition.X - _originalElementPosition.X)), (int)(p.Y - (_originalMousePosition.Y - _originalElementPosition.Y)));

                    bool controlDown = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
                    bool shiftDown = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

                    SnapLinePositions snapLines = (SnapLinePositions.Top | SnapLinePositions.Left);

                    if (controlDown && shiftDown)
                    {
                        var left = point.X;
                        var top = point.Y;

                        var bottom = (top + _selection.ActualHeight);
                        var right = (left + _selection.ActualWidth);

                        var moveLeft = left;
                        var moveTop = top;

                        foreach (ItemControl item in _items)
                        {
                            if (item.IsVisible && item != _selection)
                            {
                                var itemLeft = Canvas.GetLeft(item);
                                var itemTop = Canvas.GetTop(item);

                                var itemBottom = (itemTop + item.ActualHeight);
                                var itemRight = (itemLeft + item.ActualWidth);

                                #region Opposite Sides

                                // Left/Right
                                if (Math.Abs(left - itemRight) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Right;
                                    snapLines |= SnapLinePositions.Left;

                                    moveLeft = itemRight;
                                }

                                // Right/Left
                                if (Math.Abs(right - itemLeft) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Left;
                                    snapLines |= SnapLinePositions.Right;

                                    moveLeft = (itemLeft - _selection.ActualWidth);
                                }

                                // Bottom/Top
                                if (Math.Abs(bottom - itemTop) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Top;
                                    snapLines |= SnapLinePositions.Bottom;

                                    moveTop = (itemTop - _selection.ActualHeight);
                                }

                                // Top/Bottom
                                if (Math.Abs(top - itemBottom) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Bottom;
                                    snapLines |= SnapLinePositions.Top;

                                    moveTop = itemBottom;
                                }

                                #endregion

                                #region Same Sides

                                // Right/Right
                                if (Math.Abs(itemRight - right) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Left;
                                    snapLines |= SnapLinePositions.Right;

                                    moveLeft = (itemRight - _selection.ActualWidth);
                                }

                                // Left/Left
                                if (Math.Abs(itemLeft - point.X) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Right;
                                    snapLines |= SnapLinePositions.Left;

                                    moveLeft = itemLeft;
                                }

                                // Bottom/Bottom
                                if (Math.Abs(itemBottom - bottom) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Top;
                                    snapLines |= SnapLinePositions.Bottom;

                                    moveTop = (itemBottom - _selection.ActualHeight);
                                }

                                // Top/Top
                                if (Math.Abs(itemTop - top) < 10)
                                {
                                    snapLines &= ~SnapLinePositions.Bottom;
                                    snapLines |= SnapLinePositions.Top;

                                    moveTop = itemTop;
                                }

                                #endregion
                            }
                        }

                        point = new Point(moveLeft, moveTop);
                    }
                    else
                    {
                        if (Settings.Snap.Enabled)
                            point = Helper.SnapPoint(point, _selection.ActualWidth);
                    }

                    ShowSnapLine();
                    UpdateSnapLine(snapLines, _selection);

                    Canvas.SetLeft(_selection, Helper.BoundX(point.X, _selection));
                    Canvas.SetTop(_selection, Helper.BoundY(point.Y, _selection));

                    UpdateTitle();
                }
            }
        }

        private void Window_MouseLeave_1(object sender, MouseEventArgs e)
        {
            StopDragging();
        }

        private void mainCanvas_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            RemoveSelector();
            
            _selection = e.Source as ItemControl;

            if (_selection != null)
            {
                e.Handled = true;

                Helper.BringToFront(_selection);

                _isItemSelected = true;
                _isMousePressed = true;

                _originalMousePosition = e.GetPosition(mainCanvas);
                _originalElementPosition = new Point(Canvas.GetLeft(_selection), Canvas.GetTop(_selection));

                _adornerLayer = AdornerLayer.GetAdornerLayer(_selection);
                _adornerLayer.Add(new ResizingAdorner(_selection, _selection.IsResizable));

                UpdateTitle();                
            }
        }

        private void mainCanvas_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopDragging();
        }

        #endregion        
    }
}
