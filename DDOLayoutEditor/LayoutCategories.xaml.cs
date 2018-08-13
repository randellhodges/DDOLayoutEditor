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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Collections.ObjectModel;

using Xceed.Wpf.Toolkit;
using System.ComponentModel;

namespace DDOLayoutEditor
{
    /// <summary>
    /// Interaction logic for LayoutCategories.xaml
    /// </summary>
    public partial class LayoutCategories : ChildWindow
    {
        public LayoutCategories()
        {
            InitializeComponent();
            Closed += LayoutCategories_Closed;
        }

        public void ConfigureUI()
        {
            // There is probably a better WPF way of binding all this, but for now, I am just going to set it.
            MainList.Background = BrushCache.GetBrush(SystemColors.ControlColor);
        }

        private void LayoutCategories_Closed(object sender, EventArgs e)
        {
            var layoutItems = MainList.ItemsSource as IEnumerable<LayoutItem>;
            MainList.ItemsSource = null;

            // Overkill?
            if (layoutItems != null)
            {
                foreach (LayoutItem layoutItem in layoutItems)
                    layoutItem.ReleaseResources();
            }
        }

        private void Window_PreviewKeyUp_1(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        internal void SetData(List<Group> groups)
        {
            ObservableCollection<LayoutItem> layoutItems = new ObservableCollection<LayoutItem>();

            foreach (Group group in groups)
            {
                if (group.Items != null && group.Items.Count > 0)
                {
                    if (!group.ForceCollapse && group.Items.Count == 1)
                    {
                        LayoutItem layoutItem = new LayoutItem(group.Items[0]);
                        layoutItems.Add(layoutItem);
                    }
                    else
                    {
                        LayoutItem layoutItem = new LayoutItem(null) { Name = group.DisplayName };
                        layoutItems.Add(layoutItem);

                        foreach (ItemControl item in group.Items)
                            layoutItem.Children.Add(new LayoutItem(item));
                    }
                }
            }

            MainList.ItemsSource = layoutItems.OrderBy(f => f.Name);
        }

        public class LayoutItem : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private string _name;
            private bool _visible;

            private ItemControl _item;

            public string Name
            {
                get { return _item == null ? _name : _item.DisplayName; }
                set 
                { 
                    _name = value;
                    OnPropertyChanged("Name");
                }
            }

            public bool IsVisible
            {
                get
                {
                    if (_item == null)
                    {
                        return _visible;
                    }
                    else
                    {
                        return _item.Visibility == Visibility.Visible;
                    }
                }
                set
                {
                    if (_item == null)
                    {
                        _visible = value;

                        foreach (LayoutItem control in Children)
                            control.IsVisible = value;
                    }
                    else
                    {
                        _item.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                    }

                    OnPropertyChanged("IsVisible");
                }
            }

            public LayoutItem(ItemControl item)
            {
                _item = item;
                Children = new ObservableCollection<LayoutItem>();
            }

            public ObservableCollection<LayoutItem> Children { get; set; }

            public void ReleaseResources()
            {
                if (_item != null)
                    _item = null;

                if (Children.Count > 0)
                {
                    foreach (LayoutItem item in Children)
                        item.ReleaseResources();

                    Children.Clear();                    
                }
            }

            protected void OnPropertyChanged(string propertyName)
            {
                var handler = PropertyChanged;
                if (handler != null)
                    handler(this, new PropertyChangedEventArgs(propertyName));
            }
            
        }
    }
}
