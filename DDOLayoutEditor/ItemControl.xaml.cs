using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DDOLayoutEditor
{
    /// <summary>
    /// Interaction logic for ItemControl.xaml
    /// </summary>
    public partial class ItemControl : UserControl
    {
        private FrameworkElement _parent;

        public ItemControl()
        {
            InitializeComponent();

            Loaded += ItemControl_Loaded;
            Unloaded += ItemControl_Unloaded;

            LayoutUpdated += ItemControl_LayoutUpdated;
        }

        private bool _layedOut;

        public string ID { get; set; }
        public string LayoutID { get; set; }

        public string DisplayName
        {
            get { return MainToolTip.Content as string; }
            set
            {
                MainText.Text = value;
                MainToolTip.Content = value;
            }
        }

        public bool IsResizable { get; set; }

        public double RequestedTopRatio { get; set; }
        public double RequestedLeftRatio { get; set; }

        public double RequestedHeightRatio { get; set; }
        public double RequestedWidthRatio { get; set; }

        public double RequestedMinHeightRatio { get; set; }
        public double RequestedMinWidthRatio { get; set; }
        
        public Color BackgroundColor
        {
            get { return ((SolidColorBrush)MainCanvas.Background).Color; }
            set
            {
                if (value != null)
                {
                    var brush = BrushCache.GetBrush(value);

                    MainCanvas.Background = brush;
                    MainRect.Stroke = brush;
                }
            }
        }

        public Color TextColor
        {
            get { return ((SolidColorBrush)MainText.Foreground).Color; }
            set
            {
                if (value != null)
                {
                    MainText.Foreground = BrushCache.GetBrush(value);
                }

            }
        }

        private void ItemControl_LayoutUpdated(object sender, EventArgs e)
        {
            if (_layedOut == false)
            {
                if (MainViewbox.ActualWidth > 0)
                {
                    _layedOut = true;

                    var prevScale = MainViewbox.ActualWidth / ((FrameworkElement)MainViewbox.Child).ActualWidth;
                    var prevFontSize = (MainText.FontSize * prevScale);

                    if (prevFontSize < 8 && (ActualHeight > ActualWidth))
                    {
                        MainText.LayoutTransform = new RotateTransform(90);
                        MainText.UpdateLayout();
                    }
                }
            }
        }

        private void ItemControl_Loaded(object sender, RoutedEventArgs e)
        {
            var parent = Parent as FrameworkElement;

            if (parent != null)
            {
                _parent = parent;
                _parent.SizeChanged += parent_SizeChanged;

                Layout(parent.ActualWidth, parent.ActualHeight, RequestedWidthRatio, RequestedHeightRatio, RequestedLeftRatio, RequestedTopRatio);
            }
        }

        private void ItemControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parent != null)
            {
                _parent.SizeChanged -= parent_SizeChanged;
                _parent = null;
            }
        }

        private Rect CalculateRect(double width, double height)
        {
            return new Rect(
                new Point(
                    Canvas.GetLeft(this) / width,
                    Canvas.GetTop(this) / height
                ),
                new Size(
                    ActualWidth / width,
                    ActualHeight / height
                )
            );
        }

        private void Layout(double referenceWidth, double referenceHeight, double ratioWidth, double ratioHeight, double ratioX, double ratioY)
        {
            _layedOut = false;
            
            // Since minWidth and minHeight are a ratio of the canvas, reflect the adjustment here
            MinWidth = (RequestedMinWidthRatio * referenceWidth);
            MinHeight = (RequestedMinHeightRatio * referenceHeight);

            // Rescale the item based on currently calculated ratio
            Width = (ratioWidth * referenceWidth);
            Height = (ratioHeight * referenceHeight);

            // Finally move it based on the newly resized canvas, again, based on the recalculated ratio
            Canvas.SetTop(this, ratioY * referenceHeight);
            Canvas.SetLeft(this, ratioX * referenceWidth);
        }

        private void parent_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // We need to calculate the ratios of this items height/width and x/y against the previous size
            // so we can reapply that ratio to the new size of the parent
            var rect = CalculateRect(e.PreviousSize.Width, e.PreviousSize.Height);
            Layout(e.NewSize.Width, e.NewSize.Height, rect.Width, rect.Height, rect.X, rect.Y);
        }
        
        public Rect CalculateLayout()
        {
            // Get the items dimensions and location as a ratio of the canvas height/width
            var parent = this.Parent as FrameworkElement;
            return CalculateRect(parent.ActualWidth, parent.ActualHeight);
        }
    }
}
