/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using OneTouchMonitor.Data;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace OneTouchMonitor.Utils
{
    public class IndicateIconForeground : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is bool b) {
                if (parameter is string s) {
                    return b ?
                        new SolidColorBrush((Color)XamlBindingHelper.ConvertValue(typeof(Color), s)) :
                        DefaultColor();
                }
                return b ?
                    new SolidColorBrush(Colors.LightGreen) :
                    DefaultColor();
            }
            return DefaultColor();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            DefaultColor();

        private SolidColorBrush DefaultColor()
        {
            return (Config.Instance.Theme == Windows.UI.Xaml.ApplicationTheme.Light) ?
                new SolidColorBrush(Colors.LightGray) :
                new SolidColorBrush(Colors.SlateGray);
        }
    }
}
