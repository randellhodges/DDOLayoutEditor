using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace DDOLayoutEditor
{
    public static class BrushCache
    {
        private static Dictionary<Color, SolidColorBrush> _cache = new Dictionary<Color, SolidColorBrush>();

        public static SolidColorBrush GetBrush(Color color)
        {
            SolidColorBrush brush;
            if (_cache.TryGetValue(color, out brush))
                return brush;

            brush = new SolidColorBrush(color);
            _cache[color] = brush;

            return brush;
        }
    }
}
