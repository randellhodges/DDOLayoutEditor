using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DDOLayoutEditor
{
    internal class Group
    {
        public string ID { get; set; }
        public string DisplayName { get; set; }
        public bool ForceCollapse { get; set; }

        public List<ItemControl> Items { get; private set; }

        public Group()
        {
            Items = new List<ItemControl>();
        }
    }
}
