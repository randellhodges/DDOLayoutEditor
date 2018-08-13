using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;

namespace DDOLayoutEditor
{
    internal class SettingsChangedArgs
    {
    }

    internal delegate void SettingsChangedEventHandler(SettingsChangedArgs e);

    internal class SettingGroup
    {
        public string Name { get; set; }
        public List<SettingItem> ItemSettings { get; set; }

        public bool ForceCollapse { get; set; }
    }

    internal class SettingItem
    {
        public string Name { get; set; }
        public string LayoutID { get; set; }
        public string ID { get; set; }

        public Color BackgroundColor { get; set; }
        public Color TextColor { get; set; }

        public bool CanResize { get; set; }
        public Size MinSize { get; set; }
    }

    internal class Snap
    {
        public bool Enabled { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
    }

    internal class Grid
    {
        public bool Visible { get; set; }

        public Color LineColor { get; set; }
        public double Opacity { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }
    }

    internal class Settings
    {
        internal static event SettingsChangedEventHandler SettingsChanged;

        public const string DefaultLineColor = "#D3D3D3";
        public const string DefaultBackgroundColor = "#FF00FF";

        public static Size MinimumSize { get; } = new Size(640, 480);
        public static List<SettingGroup> Groups { get; set; }
        public static Size Layout { get; set; }
        public static Color BackgroundColor { get; set; }
        public static string BackgroundImagePath { get; set; }        
        public static bool ShowBackgroundImage { get; set; }
        public static Snap Snap { get; set; }
        public static Grid Grid { get; set; }

        static Settings()
        {
            Layout = new Size(800, 600);

            Grid = new Grid { Visible = false, Height = 10, Width = 10, LineColor = Helper.GetColor(DefaultLineColor), Opacity = 1.0 };
            Snap = new Snap { Enabled = false, X = 5, Y = 5 };
        }

        private static string GetSettingsFilePath()
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string filePath = Path.Combine(path, "settings.xml");
            return filePath;
        }

        private static string GetGroupsFilePath()
        {
            string path = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            string filePath = Path.Combine(path, "groups.xml");
            return filePath;
        }

        public static void Load()
        {
            string settingsFile = GetSettingsFilePath();
            
            if (File.Exists(settingsFile))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(settingsFile);

                XmlNode node;

                #region Load Snap

                node = doc.SelectSingleNode("/settings/snap");
                if (node != null)
                {
                    Snap.X = Math.Max(1, int.Parse(Helper.GetValue(node, "x", "5")));
                    Snap.Y = Math.Max(1, int.Parse(Helper.GetValue(node, "y", "5")));

                    Snap.Enabled = bool.Parse(Helper.GetValue(node, "enabled", "false"));
                }

                #endregion

                #region Load Grid

                node = doc.SelectSingleNode("/settings/grid");
                if (node != null)
                {
                    Grid.Height = Math.Max(1, int.Parse(Helper.GetValue(node, "height", "5")));
                    Grid.Width = Math.Max(1, int.Parse(Helper.GetValue(node, "width", "5")));

                    Grid.LineColor = Helper.GetColor(Helper.GetValue(node, "lineColor", DefaultLineColor));
                    Grid.Visible  = bool.Parse(Helper.GetValue(node, "visible", "false"));

                    double opacity = double.Parse(Helper.GetValue(node, "opacity", "1.0"));

                    if (opacity < 0) opacity = 0;
                    if (opacity > 1) opacity = 1;

                    Grid.Opacity = opacity;
                }

                #endregion

                #region Load Layout

                node = doc.SelectSingleNode("/settings/layout");
                if (node != null)
                {
                    int height = int.Parse(Helper.GetValue(node, "height", "1280"));
                    int width = int.Parse(Helper.GetValue(node, "width", "800"));

                    Layout = new Size(width, height);
                    BackgroundColor = Helper.GetColor(Helper.GetValue(node, "backgroundColor", DefaultBackgroundColor));

                    var imageSrc = Helper.GetValue(node, "backgroundImage");
                    if (!String.IsNullOrWhiteSpace(imageSrc))
                    {
                        string appPath = Path.GetDirectoryName(settingsFile);

                        BackgroundImagePath = Path.Combine(appPath, imageSrc);

                        bool displayBackground = bool.Parse(Helper.GetValue(node, "displayBackground", "false"));
                        ShowBackgroundImage = displayBackground && System.IO.File.Exists(BackgroundImagePath);
                    }
                }

                #endregion
            }

            var groupsFile = GetGroupsFilePath();
            var groups = new List<SettingGroup>();

            if (File.Exists(groupsFile))
            {
                var doc = new XmlDocument();
                doc.Load(groupsFile);

                #region Load Groups

                foreach (XmlNode groupNode in doc.SelectNodes("/groups/group"))
                {
                    var groupCanResize = bool.Parse(Helper.GetValue(groupNode, "allowResize", "false"));

                    var groupBackgroundColor = Helper.GetColor(groupNode.Attributes["color"].Value);
                    var groupTextColor = Helper.GetColor(Helper.GetValue(groupNode, "textColor", "#000000"));

                    var forceCollapse = bool.Parse(Helper.GetValue(groupNode, "forceCollapse", "false"));

                    SettingGroup group = new SettingGroup()
                    {
                        Name = groupNode.Attributes["name"].Value,
                        ItemSettings = new List<SettingItem>(),
                        ForceCollapse = forceCollapse
                    };

                    var minSize = new Size(
                        int.Parse(Helper.GetValue(groupNode, "minWidth", "0")),
                        int.Parse(Helper.GetValue(groupNode, "minHeight", "0"))
                    );

                    foreach (XmlNode itemNode in groupNode.SelectNodes("item"))
                    {
                        SettingItem item = new SettingItem
                        {
                            ID = itemNode.Attributes["id"].Value,
                            Name = Helper.GetValue(itemNode, "name", null),

                            LayoutID = itemNode.Attributes["layoutID"].Value,
                            CanResize = groupCanResize,

                            TextColor = groupTextColor,
                            BackgroundColor = groupBackgroundColor
                        };

                        // Override with item specific values
                        var itemBackgroundColor = Helper.GetValue(itemNode, "color");
                        if (itemBackgroundColor != null)
                            item.BackgroundColor = Helper.GetColor(itemBackgroundColor);

                        var itemResize = Helper.GetValue(itemNode, "allowResize");
                        if (itemResize != null)
                            item.CanResize = bool.Parse(itemResize);

                        var itemTextColor = Helper.GetValue(itemNode, "textColor");
                        if (itemTextColor != null)
                            item.TextColor = Helper.GetColor(itemTextColor);

                        var minWidth = minSize.Width;
                        var minHeight = minSize.Height;

                        var minWidthS = Helper.GetValue(itemNode, "minWidth");
                        if (!String.IsNullOrWhiteSpace(minWidthS))
                            minWidth = double.Parse(minWidthS);

                        var minHeightS = Helper.GetValue(itemNode, "minHeight");
                        if (!String.IsNullOrWhiteSpace(minWidthS))
                            minHeight = double.Parse(minHeightS);

                        item.MinSize = new Size(minWidth, minHeight);

                        group.ItemSettings.Add(item);
                    }

                    groups.Add(group);
                }

                #endregion
            }

            // Easy sort by name
            groups = (from g in groups orderby g.Name select g).ToList();
            Groups = groups;           
        }

        public static void Save()
        {
            Task.Factory.StartNew(() =>
            {
                string filePath = GetSettingsFilePath();

                if (File.Exists(filePath))
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);

                    // Layout
                    Helper.SetValues(doc, "/settings/layout",
                        Helper.GetPair("height",  Layout.Height.ToString()),
                        Helper.GetPair("width", Layout.Width.ToString()),
                        Helper.GetPair("backgroundColor", Helper.GetColorCode(BackgroundColor)),
                        Helper.GetPair("backgroundImage", BackgroundImagePath)
                    );

                    // Grid
                    Helper.SetValues(doc, "/settings/grid",
                        Helper.GetPair("height", Grid.Height.ToString()),
                        Helper.GetPair("width", Grid.Width.ToString()),
                        Helper.GetPair("visible", Grid.Visible.ToString()),
                        Helper.GetPair("lineColor", Helper.GetColorCode(Grid.LineColor)),
                        Helper.GetPair("opacity", Grid.Opacity.ToString())
                    );

                    // Snap
                    Helper.SetValues(doc, "/settings/snap",
                        Helper.GetPair("x", Snap.X.ToString()),
                        Helper.GetPair("y", Snap.Y.ToString()),
                        Helper.GetPair("enabled", Snap.Enabled.ToString())
                    );

                    doc.Save(filePath);
                }
            });
            
            OnSettingsChanged();
        }

        protected static void OnSettingsChanged()
        {
            SettingsChanged?.Invoke(new SettingsChangedArgs());
        }
    }
}
