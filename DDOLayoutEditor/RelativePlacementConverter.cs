using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace DDOLayoutEditor
{
    // http://richardwilburn.wordpress.com/2009/01/28/docking-wpf-controls-to-right-or-bottom-of-a-canvas/
    public class RelativePlacementConverter : IMultiValueConverter
    {
        #region IMultiValueConverter Members

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double? screenWidth = values[0] as double?; //parent width
            double? menuWidth = values[1] as double?; //own width

            if (screenWidth != null && menuWidth != null)
            {
                return (screenWidth - menuWidth);
            }

            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
