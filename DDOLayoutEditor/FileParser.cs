using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xml;

using System.Diagnostics;

namespace DDOLayoutEditor
{
    internal static class FileParser
    {
        public static void Save(string path, List<Group> groups)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            foreach (Group group in groups)
            {
                if (group.Items != null)
                {
                    foreach (ItemControl item in group.Items)
                    {
                        bool hide = false;

                        // Probably a more WPF way of doing this, but whatever.  If it is invisible it may not have had its layout updated
                        // so we are going to show it, update its layout and then hide it again.
                        if (item.IsVisible == false)
                        {
                            item.Visibility = Visibility.Visible;
                            item.UpdateLayout();

                            hide = true;
                        }

                        Rect rect = item.CalculateLayout();

                        //Debug.Assert(item.Rect.Width == rect.Width, "Width doesn't match");
                        //Debug.Assert(item.Rect.Height == rect.Height, "Height doesn't match");
                        //Debug.Assert(item.Rect.X == rect.X, "X doesn't match");
                        //Debug.Assert(item.Rect.Y == rect.Y, "Y doesn't match");

                        XmlNode node = doc.SelectSingleNode(String.Format("/UI/Layouts/Layout[@ID='{0}']/Element[@ID='{1}']", item.LayoutID, item.ID));
                        if (node != null)
                        {
                            node.Attributes["Height"].Value = rect.Height.ToString("0.000000");
                            node.Attributes["Width"].Value = rect.Width.ToString("0.000000");

                            node.Attributes["X"].Value = rect.X.ToString("0.000000");
                            node.Attributes["Y"].Value = rect.Y.ToString("0.000000");
                        }

                        if (hide)
                            item.Visibility = Visibility.Hidden;
                    }
                }
            }

            doc.Save(path);
        }

        public static List<Group> Load(string path)
        {
            var groups = new List<Group>();
            var doc = new XmlDocument();

            doc.Load(path);

            foreach (SettingGroup settingGroup in Settings.Groups)
            {
                var group = new Group
                {
                    DisplayName = settingGroup.Name,
                    ForceCollapse = settingGroup.ForceCollapse
                };

                foreach (SettingItem settingItem in settingGroup.ItemSettings)
                {
                    XmlNode layoutNode = doc.SelectSingleNode(String.Format("/UI/Layouts/Layout[@ID='{0}']", settingItem.LayoutID));

                    if (layoutNode != null)
                    {
                        XmlNodeList itemNodes;

                        if (settingItem.ID == "*")
                            itemNodes = layoutNode.SelectNodes("Element");
                        else
                            itemNodes = layoutNode.SelectNodes(String.Format("Element[@ID='{0}']", settingItem.ID));

                        if (itemNodes != null)
                        {
                            foreach (XmlNode itemNode in itemNodes)
                            {
                                #region Parse the Child Element

                                var item = new ItemControl
                                {
                                    ID = itemNode.Attributes["ID"].Value,
                                    LayoutID = itemNode.ParentNode.Attributes["ID"].Value,
                                    IsResizable = settingItem.CanResize,

                                    BackgroundColor = settingItem.BackgroundColor,
                                    TextColor = settingItem.TextColor,

                                    RequestedMinWidthRatio = settingItem.MinSize.Width,
                                    RequestedMinHeightRatio = settingItem.MinSize.Height,

                                    RequestedTopRatio = double.Parse(itemNode.Attributes["Y"].Value),
                                    RequestedLeftRatio = double.Parse(itemNode.Attributes["X"].Value),

                                    RequestedHeightRatio = double.Parse(itemNode.Attributes["Height"].Value),
                                    RequestedWidthRatio = double.Parse(itemNode.Attributes["Width"].Value)
                                };

                                #region Create the Display Name

                                if (String.IsNullOrWhiteSpace(settingItem.Name))
                                {
                                    string displayName = item.ID;

                                    if (item.ID.StartsWith("UndockedShortcut"))
                                    {
                                        // Let's make this a little prettier for the user
                                        // UndockedShortcut2_Vertical_MarkerList
                                        int.TryParse(displayName.Split('_')[0].Replace("UndockedShortcut", ""), out int index);
                                        displayName = "Shortcut " + (index + 1);
                                    }
                                    else if (item.ID.StartsWith("UndockedHenchmen"))
                                    {
                                        // Let's make this a little prettier for the user
                                        // UndockedHenchmen0_Horizontal_MarkerList
                                        int.TryParse(displayName.Split('_')[0].Replace("UndockedHenchmen", ""), out int index);
                                        displayName = "Hireling " + (index + 1);
                                    }
                                    else
                                    {
                                        // Various names
                                        // FellowshipDisplay, ArenaScoring_SummaryPanel, Guild_Bank_Field, PendingChallengeButton
                                        displayName = displayName.Replace("_", " ");
                                        displayName = Regex.Replace(Regex.Replace(displayName, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
                                        displayName = Regex.Replace(displayName, @"[ ]{2,}", @" ", RegexOptions.None);
                                    }

                                    item.DisplayName = displayName;
                                }
                                else
                                {
                                    item.DisplayName = settingItem.Name;
                                }

                                item.Visibility = Visibility.Collapsed;

                                #endregion

                                group.Items.Add(item);

                                #endregion
                            }
                        }
                    }                    
                }

                #region Item Sorting

                if (group.Items.Count > 0)
                {
                    const string NumberFormat = "000000";

                    var sorted = group.Items.OrderBy(f =>
                    {
                        if (f.ID.StartsWith("ChatDisplay"))
                        {
                            int.TryParse(f.ID.Replace("ChatDisplay", ""), out int index);
                            return "ChatDisplay" + index.ToString(NumberFormat);
                        }

                        if (f.ID.StartsWith("UndockedShortcut"))
                        {
                            int.TryParse(f.ID.Split('_')[0].Replace("UndockedShortcut", ""), out int index);
                            return "ShortcutsBar" + index.ToString(NumberFormat);
                        }

                        if (f.ID.StartsWith("UndockedHenchmen"))
                        {
                            int.TryParse(f.ID.Split('_')[0].Replace("UndockedHenchmen", ""), out int index);
                            return "Hireling" + index.ToString(NumberFormat);
                        }

                        return f.Name;
                    });

                    var x = sorted.ToList();

                    group.Items.Clear();
                    group.Items.AddRange(x);
                }

                #endregion

                if (group.Items.Count > 0)
                    groups.Add(group);
                
            }

            return groups;
        }
    }
}
