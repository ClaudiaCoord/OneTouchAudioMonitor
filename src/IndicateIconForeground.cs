/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using Windows.UI;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace OneTouchMonitor
{
    public class IndicateIconForeground : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is bool b) {
                if (parameter is string s) {
                    return b ?
                        new SolidColorBrush((Color)XamlBindingHelper.ConvertValue(typeof(Color), s)) :
                        new SolidColorBrush(Colors.SlateGray);
                }
                return b ?
                    new SolidColorBrush(Colors.LightGreen) :
                    new SolidColorBrush(Colors.SlateGray);
            }
            return new SolidColorBrush(Colors.SlateGray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) =>
            new SolidColorBrush(Colors.DarkGray);
    }
}
