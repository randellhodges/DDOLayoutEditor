using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Xceed.Wpf.Toolkit;

namespace DDOLayoutEditor
{
    /// <summary>
    /// Interaction logic for SettingsEditor.xaml
    /// </summary>
    public partial class SettingsEditor : ChildWindow
    {
        public SettingsEditor()
        {
            InitializeComponent();
        }

        public void ConfigureUI()
        {
            // There is probably a better WPF way of binding all this, but for now, I am just going to set it.
            mainCanvas.Background = BrushCache.GetBrush(SystemColors.ControlColor);

            windowWidth.Minimum = (int)Settings.MinimumSize.Width;
            windowHeight.Minimum = (int)Settings.MinimumSize.Height;

            // Grid
            gridVisible.IsChecked = Settings.Grid.Visible;
            gridLineColor.SelectedColor = Settings.Grid.LineColor;
            gridTransparency.Value = (int)(Settings.Grid.Opacity * 100);

            gridCellWidth.Value = Settings.Grid.Width;
            gridCellHeight.Value = Settings.Grid.Height;

            // Snap
            snapEnabled.IsChecked = Settings.Snap.Enabled;
            snapHorizontal.Value = Settings.Snap.X;
            snapVertical.Value = Settings.Snap.Y;

            // Window
            windowBackgroundColor.SelectedColor = Settings.BackgroundColor;
            windowBackgroundImage.Text = Settings.BackgroundImagePath;
            windowBackgroundVisible.IsChecked = Settings.ShowBackgroundImage;

            windowWidth.Value = (int)Settings.Layout.Width;
            windowHeight.Value = (int)Settings.Layout.Height;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            bool changed = false;

            #region Determine if Something Changed

            #region Grid

            if (gridVisible.IsChecked != Settings.Grid.Visible)
            {
                Settings.Grid.Visible = gridVisible.IsChecked.GetValueOrDefault();
                changed = true;
            }

            if (gridLineColor.SelectedColor != Settings.Grid.LineColor)
            {
                Settings.Grid.LineColor = gridLineColor.SelectedColor.GetValueOrDefault(Helper.GetColor(Settings.DefaultLineColor));
                changed = true;
            }

            if (gridTransparency.Value != (int)(Settings.Grid.Opacity * 100))
            {
                Settings.Grid.Opacity = gridTransparency.Value.GetValueOrDefault() / 100.0;
                changed = true;
            }

            if (gridCellWidth.Value != Settings.Grid.Width)
            {
                Settings.Grid.Width = gridCellWidth.Value.GetValueOrDefault();
                changed = true;
            }
            
            if (gridCellHeight.Value != Settings.Grid.Height)
            {
                Settings.Grid.Height = gridCellHeight.Value.GetValueOrDefault();
                changed = true;
            }

            #endregion

            #region Snap

            if (snapEnabled.IsChecked != Settings.Snap.Enabled)
            {
                Settings.Snap.Enabled = snapEnabled.IsChecked.GetValueOrDefault();
                changed = true;
            }

            if (snapHorizontal.Value != Settings.Snap.X)
            {
                Settings.Snap.X = snapHorizontal.Value.GetValueOrDefault();
                changed = true;
            }

            if (snapVertical.Value != Settings.Snap.Y)
            {
                Settings.Snap.Y = snapVertical.Value.GetValueOrDefault();
                changed = true;
            }
            
            #endregion

            #region Window

            if (windowBackgroundColor.SelectedColor != Settings.BackgroundColor)
            {
                Settings.BackgroundColor = windowBackgroundColor.SelectedColor.GetValueOrDefault(Helper.GetColor(Settings.DefaultBackgroundColor));
                changed = true;
            }

            if (windowBackgroundImage.Text != Settings.BackgroundImagePath)
            {
                Settings.BackgroundImagePath = windowBackgroundImage.Text;
                changed = true;
            }

            if ((windowWidth.Value != (int)Settings.Layout.Width) || (windowHeight.Value != (int)Settings.Layout.Height))
            {
                Settings.Layout = new Size(windowWidth.Value.GetValueOrDefault(), windowHeight.Value.GetValueOrDefault());
                changed = true;
            }

            if (windowBackgroundVisible.IsChecked != Settings.ShowBackgroundImage)
            {
                Settings.ShowBackgroundImage = windowBackgroundVisible.IsChecked.GetValueOrDefault();
                changed = true;
            }

            #endregion

            #endregion

            if (changed)
                Settings.Save();

            Close();            
        }

        private void buttonBackgroundImage_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = String.Format("Image Files({0})|{0}|All files (*.*)|*.*", Helper.GetImageFilter()),
                InitialDirectory = Helper.GetDDOFolder(),
                CheckFileExists = true
            };

            if (dlg.ShowDialog().GetValueOrDefault())
            {
                windowBackgroundImage.Text = dlg.FileName;
                windowBackgroundVisible.IsChecked = true;
            }
        }

        private void buttonSizeToImage_Click(object sender, RoutedEventArgs e)
        {
            var image = Helper.GetImage(windowBackgroundImage.Text);
            if (image != null)
            {
                windowWidth.Value = (int)image.Width;
                windowHeight.Value = (int)image.Height;
            }
        }

        private void windowBackgroundImage_TextChanged(object sender, TextChangedEventArgs e)
        {
            var isEmpty = (windowBackgroundImage.Text.Trim().Length == 0);
            if (isEmpty)
                windowBackgroundVisible.IsChecked = false;
        }
    }
}
