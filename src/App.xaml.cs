/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OneTouchMonitor.Data;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Globalization;
using Windows.Media;
using Windows.UI.Core.Preview;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace OneTouchMonitor
{
    public enum AppPageType : int
    {
        MainControls,
        MainSetup
    }

    sealed partial class App : Application
    {
        private Frame rootFrame = default(Frame);
        private List<Tuple<AppPageType, Type>> pagesList = new()
        {
            new (AppPageType.MainControls, typeof(MainPage)),
            new (AppPageType.MainSetup, typeof(SettingPage))
        };

        public App() {
            ApplicationLanguages.PrimaryLanguageOverride = Thread.CurrentThread.CurrentUICulture.TwoLetterISOLanguageName;
            this.InitializeComponent();
            this.Suspending += OnSuspending;
            this.EnteredBackground += App_EnteredBackground;
            this.LeavingBackground += App_LeavingBackground;
            ConfigLoad();
            RequestedTheme = Config.Instance.Theme;
        }

        private void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
        }
        private void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
        }

        private async void ConfigLoad() {
            await Config.Load().ContinueWith((a) => {
                Config.Init();
            }).ConfigureAwait(false);
        }

        protected override void OnLaunched(LaunchActivatedEventArgs e) {
            Frame frame = GetFrame();

            if (e.PrelaunchActivated == false) {
                if (frame.Content == null) {
                    NavigateTo(AppPageType.MainControls, frame, e);
                }
                Window.Current.Activate();
            }
        }

        private Frame GetFrame() {
            if (rootFrame != null) return rootFrame;
            Frame f = Window.Current.Content as Frame;
            if (f != null) { rootFrame = f; return f; }
            f = new Frame();
            f.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = f;
            rootFrame = f;
            return f;
        }

        public void OnNavigatedTo(AppPageType type) => NavigateTo(type, null, null);
        private void NavigateTo(AppPageType type, Frame frame = default, LaunchActivatedEventArgs e = default) {
            try {
                if (frame == default)
                    frame = GetFrame();
                var a = (from i in pagesList where i.Item1 == type select i).FirstOrDefault();
                if (a == default) return;
                if (e != default) frame.Navigate(a.Item2, e.Arguments);
                else frame.Navigate(a.Item2);

                Size size;
                string title;

                switch (type) {
                    case AppPageType.MainControls: { size = MainPage.Size; title = MainPage.Title; break; }
                    case AppPageType.MainSetup: { size = SettingPage.Size; title = SettingPage.Title; break; }
                    default: return;
                }
                ApplicationView.PreferredLaunchViewSize = size;
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(size);
                ApplicationView.GetForCurrentView().TryResizeView(size);
                ApplicationView.GetForCurrentView().Title = title;
                if (e == default)
                    Window.Current.Activate();
            } catch { }
        }

        void OnNavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void OnSuspending(object sender, SuspendingEventArgs e) {
            var deferral = e.SuspendingOperation.GetDeferral();
            SystemMediaTransportControls.GetForCurrentView().IsPlayEnabled = true;
            SystemMediaTransportControls.GetForCurrentView().IsPauseEnabled = true;
            deferral.Complete();
        }
    }
}
