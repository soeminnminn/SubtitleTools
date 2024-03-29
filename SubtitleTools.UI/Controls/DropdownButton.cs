﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SubtitleTools.UI.Controls
{
    public class DropdownButton : ToggleButton
    {
        public DropdownButton()
        {
            ContentTemplate = GetDataTemplate();
            Style = (Style)FindResource(ToolBar.ToggleButtonStyleKey);
        }

        private DataTemplate GetDataTemplate()
        {
            var template = new DataTemplate(typeof(DropdownButton));
            var path = new FrameworkElementFactory(typeof(Path));
            path.SetValue(Path.DataProperty, Geometry.Parse("F1 M 301.14,-189.041L 311.57,-189.041L 306.355,-182.942L 301.14,-189.041 Z"));
            path.SetValue(MarginProperty, new Thickness(4, 4, 0, 4));
            path.SetValue(WidthProperty, 5d);
            path.SetValue(Shape.FillProperty, Brushes.Black);
            path.SetValue(Shape.StretchProperty, Stretch.Uniform);
            path.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Right);
            path.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);

            var panel = new FrameworkElementFactory(typeof(StackPanel));
            panel.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));

            var binding = new Binding("Content") { Source = this };
            contentPresenter.SetBinding(ContentProperty, binding);

            panel.AppendChild(contentPresenter);
            panel.AppendChild(path);

            template.VisualTree = panel;
            return template;
        }

        private void DropdownMenu_Closed(object sender, RoutedEventArgs e)
        {
            IsChecked = false;
        }

        private ContextMenu dropdownMenu = null;
        public ContextMenu DropdownMenu
        {
            get
            {
                return dropdownMenu;
            }
            set
            {
                dropdownMenu = value;
                dropdownMenu.Closed += DropdownMenu_Closed;
            }
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);

            DropdownMenu.PlacementTarget = this;
            DropdownMenu.Placement = PlacementMode.Bottom;
            DropdownMenu.IsOpen = true;
        }
    }
}
