/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace OneTouchMonitor.Utils
{
    public class MenuFlyoutExtension
    {
        public static List<MenuFlyoutItem> GetMenuFlyoutItems(DependencyObject obj)
        { return (List<MenuFlyoutItem>)obj.GetValue(MenuFlyoutItemsProperty); }

        public static void SetMenuFlyoutItems(DependencyObject obj, List<MenuFlyoutItem> value)
        { obj.SetValue(MenuFlyoutItemsProperty, value); }

        public static readonly DependencyProperty MenuFlyoutItemsProperty =
            DependencyProperty.Register("MenuFlyoutItems", typeof(List<MenuFlyoutItem>), typeof(MenuFlyoutExtension),
            new PropertyMetadata(new List<MenuFlyoutItem>(), (sender, e) =>
            {
                if (sender is MenuFlyoutSubItem menu) {
                    menu.Items.Clear();
                    if ((e != null) && (e.NewValue is List<MenuFlyoutItem> items)) {
                        foreach (var item in items) menu.Items.Add(item);
                    }
                }
            }));
    }
}
