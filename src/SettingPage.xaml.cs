/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using OneTouchMonitor.Data;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace OneTouchMonitor
{
    public sealed partial class SettingPage : Page, INotifyPropertyChanged
    {
        public static Size Size = new Size(340, 400);
        public static string Title => Config.GetString("TITLE2");

        public ObservableCollection<AudioDevice> ListAudioIn { get; } = new();
        public ObservableCollection<AudioDevice> ListAudioOut { get; } = new();
        public ObservableCollection<BtDevice> ListBtOut { get; } = new();

        private AudioDevice itemAudioIn = default;
        public AudioDevice ItemAudioIn {
            get => itemAudioIn;
            set {
                itemAudioIn = value;
                if ((value != null) && !Config.Instance.AudioInSelectedDevices.Contains(value))
                    Config.Instance.AudioInSelectedDevices.Add(value);
                OnPropertyChanged();
            }
        }
        private AudioDevice itemAudioOut = default;
        public AudioDevice ItemAudioOut {
            get => itemAudioOut;
            set {
                itemAudioOut = value;
                if ((value != null) && !Config.Instance.AudioOutSelectedDevices.Contains(value))
                    Config.Instance.AudioOutSelectedDevices.Add(value);
                OnPropertyChanged();
            }
        }
        private BtDevice itemBtOut = default;
        public BtDevice ItemBtOut {
            get => itemBtOut;
            set {
                itemBtOut = value;
                if ((value != null) && !Config.Instance.BtSelectedDevices.Contains(value))
                    Config.Instance.BtSelectedDevices.Add(value);
                OnPropertyChanged();
            }
        }
        public bool ThemeSelector {
            get => Config.Instance.Theme == ApplicationTheme.Dark;
            set {
                Config.Instance.Theme = value ? ApplicationTheme.Dark : ApplicationTheme.Light;
                RequestedTheme = Config.Instance.EleTheme;
                OnPropertyChanged();
            }
        }
        public bool IsSound {
            get => Config.Instance.IsSound;
            set {
                Config.Instance.IsSound = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void OnPropertyChanged(params string[] names) {
            foreach (var name in names)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public SettingPage() {
            this.InitializeComponent();
            DataContext = this;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            RequestedTheme = Config.Instance.EleTheme;

            foreach (var a in Config.Instance.AudioInAllDevices)
                ListAudioIn.Add(a);
            foreach (var a in Config.Instance.AudioOutAllDevices)
                ListAudioOut.Add(a);
            foreach (var a in Config.Instance.BtAllDevices)
                ListBtOut.Add(a);

            OnPropertyChanged(
                nameof(ListAudioIn),
                nameof(ListAudioOut),
                nameof(ListBtOut));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
        }

        private void Button_ClickReturn(object sender, RoutedEventArgs _) =>
            ((App)App.Current).OnNavigatedTo(AppPageType.MainControls);

        private async void Button_ClickSave(object sender, RoutedEventArgs _) =>
            await Config.Save().ConfigureAwait(false);

        private void Button_ClickErase(object sender, RoutedEventArgs _) {
            Config.Instance.BtSelectedDevices.Clear();
            Config.Instance.AudioInSelectedDevices.Clear();
            Config.Instance.AudioOutSelectedDevices.Clear();
            ItemAudioIn = ItemAudioOut = default;
            ItemBtOut = default;

                OnPropertyChanged(
                    nameof(ListAudioIn),
                    nameof(ListAudioOut),
                    nameof(ListBtOut));
        }

        private void ComboAudioIn_Loaded(object sender, RoutedEventArgs _) {
            if ((sender is ComboBox cb) && (Config.Instance.AudioInSelectedDevices.Count > 0)) {
                var a = DeviceFind(ListAudioIn, Config.Instance.AudioInSelectedDevices[0].Name);
                if (a != null) cb.SelectedIndex = ListAudioIn.IndexOf(a);
            }
        }
        private void ComboAudioOut_Loaded(object sender, RoutedEventArgs _) {
            if ((sender is ComboBox cb) && (Config.Instance.AudioOutSelectedDevices.Count > 0)) {
                var a = DeviceFind(ListAudioOut, Config.Instance.AudioOutSelectedDevices[0].Name);
                if (a != null) cb.SelectedIndex = ListAudioOut.IndexOf(a);
            }
        }
        private void ComboBtOut_Loaded(object sender, RoutedEventArgs _) {
            if ((sender is ComboBox cb) && (Config.Instance.BtSelectedDevices.Count > 0)) {
                var a = DeviceFind(ListBtOut, Config.Instance.BtSelectedDevices[0].Name);
                if (a != null) cb.SelectedIndex = ListBtOut.IndexOf(a);
            }
        }
        private T1 DeviceFind<T1>(ObservableCollection<T1> list, string name) where T1 : IDevice =>
            (from i in list where i.Name.Equals(name) select i).FirstOrDefault();
    }
}
