
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using CoreAudioApi;
using Decelerate.Annotations;
using Decelerate.Properties;

namespace Decelerate
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public TimeSpan TimeElapsed
        {
            get { return _timeElapsed; }
            set
            {
                _timeElapsed = value;
                OnPropertyChanged();
            }
        }

        public TimeSpan TimeRemaining
        {
            get { return _timeRemaining; }
            set
            {
                _timeRemaining = value;
                OnPropertyChanged();
            }
        }

        public MainWindow()
        {
            InitializeComponent();

            var deviceEnumerator = new MMDeviceEnumerator();
            _mmDevice = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            _mmDevice.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;

            InitializeControls();

            TimeElapsed = new TimeSpan(0);
            TimeRemaining = new TimeSpan(0);
        }

        private void SaveControlValues()
        {
            Settings.Default.EndVolume = (int) EndVolumeSlider.Value;
            Settings.Default.DelayTime = (int) DelayTimeSlider.Value;
            Settings.Default.FadeTime = (int) FadeTimeSlider.Value;

            Settings.Default.DoNothingAtEnd = AtEndDoNothingButton.IsChecked ?? false;
            Settings.Default.HibernateAtEnd = AtEndHibernateButton.IsChecked ?? false;
            Settings.Default.ShutdownAtEnd = AtEndShutdownButton.IsChecked ?? false;

            Settings.Default.Save();
        }

        private void Run()
        {
            _startVolume = MasterVolume;
            _running = true;

            UpdateControlEnabledState();
            SaveControlValues();

            var minutesToDelay = (int) DelayTimeSlider.Value;
            var totalTime = minutesToDelay + FadeTimeSlider.Value;

            StartStopwatchTimer((int)totalTime);

            if (minutesToDelay > 0)
            {
                _delayTimer = new Timer {Interval = minutesToDelay*60*1000};

                _delayTimer.Elapsed += (source, elapsedEventArgs) =>
                {
                    ((Timer)source).Stop();
                    Dispatcher.Invoke(StartFade);
                };

                _delayTimer.Start();
            }
            else
            {
                StartFade();
            }
        }

        private void Stop()
        {
            _running = false;

            if (_delayTimer != null && _delayTimer.Enabled)
            {
                _delayTimer.Stop();
            }

            if (_fadeTimer != null && _fadeTimer.Enabled)
            {
                _fadeTimer.Stop();
            }

            if (_stopwatchTimer != null && _stopwatchTimer.Enabled)
            {
                _stopwatchTimer.Stop();
            }

            TimeElapsed = new TimeSpan(0);
            TimeRemaining = new TimeSpan(0);

            UpdateControlEnabledState();
        }

        private void Reset()
        {
            Stop();
            MasterVolume = _startVolume;
        }

        private void StartStopwatchTimer(double totalTime)
        {
            TimeElapsed = new TimeSpan(0);
            TimeRemaining = TimeSpan.FromMinutes(totalTime);

            _stopwatchTimer = new Timer {Interval = 1000};

            _stopwatchTimer.Elapsed += StopwatchTimerTick;

            _stopwatchTimer.Start();
        }

        private void StopwatchTimerTick(object source, ElapsedEventArgs elapsedEventArgs)
        {
            TimeElapsed = TimeElapsed.Add(TimeSpan.FromSeconds(1));
            TimeRemaining = TimeRemaining.Subtract(TimeSpan.FromSeconds(1));
           
            if (TimeRemaining.TotalSeconds <= 1)
            {
                ((Timer) source).Stop();
            }
        }

        private void StartFade()
        {
            var minutesToFade = (int) Math.Round(FadeTimeSlider.Value);
            var startVolume = (int) Math.Round(StartVolumeSlider.Value);
            var endVolume = (int) Math.Round(EndVolumeSlider.Value);

            var fadeStartTime = DateTime.UtcNow;

            // Time (minutes) between each time we decrease the volume by 1%
            var volChangeInterval = (double) minutesToFade / (startVolume - endVolume);

            _fadeTimer = new Timer {Interval = volChangeInterval * 60 * 1000};

            _fadeTimer.Elapsed += (source, elapsedEventArgs) =>
            {
                var timeSinceStart = DateTime.UtcNow.Subtract(fadeStartTime).TotalMilliseconds;

                MasterVolume = (int) Math.Round
                    (startVolume - (timeSinceStart * (startVolume - endVolume) / (minutesToFade * 60 * 1000)));

                if (MasterVolume <= endVolume)
                {
                    Dispatcher.Invoke(FinishFade);
                }
            };

            _fadeTimer.Start();
        }

        private void FinishFade()
        {
            _fadeTimer.Stop();

            if (AtEndShutdownButton.IsChecked == true)
            {
                Process.Start("shutdown", "/s /t 0");
            }

            if (AtEndHibernateButton.IsChecked == true)
            {
                Process.Start("shutdown", "/h /t 0");
            }

            if (AtEndRestoreVolumeCheckbox.IsChecked == true)
            {
                MasterVolume = _startVolume;
            }

            _running = false;
            UpdateControlEnabledState();
        }

        private void InitializeControls()
        {
            StartVolumeSlider.Value = MasterVolume;

            EndVolumeSlider.Value = Settings.Default.EndVolume;
            DelayTimeSlider.Value = Settings.Default.DelayTime;
            FadeTimeSlider.Value = Settings.Default.FadeTime;

            AtEndDoNothingButton.IsChecked = Settings.Default.DoNothingAtEnd;
            AtEndShutdownButton.IsChecked = Settings.Default.ShutdownAtEnd;

            UpdateControlEnabledState();
        }

        private void UpdateControlEnabledState()
        {
            FadeTimeSlider.IsEnabled = !_running;
            DelayTimeSlider.IsEnabled = !_running;
            StartVolumeSlider.IsEnabled = !_running;
            EndVolumeSlider.IsEnabled = !_running;

            GoButton.IsEnabled = !_running;
            StopButton.IsEnabled = _running;
            ResetButton.IsEnabled = _running;
        }

        private void StartVolumeSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MasterVolume = (int) StartVolumeSlider.Value;
        }

        private void GoButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!_running)
            {
                //start
                if ((int) Math.Round(DelayTimeSlider.Value) == 0 && (int) Math.Round(FadeTimeSlider.Value) == 0)
                {
                    MessageBox.Show("Delay time and fade time can't both be zero.");
                }
                else
                {
                    Run();
                }
            }
        }

        private void StopButton_OnClick(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void ResetButton_OnClickButton_OnClick(object sender, RoutedEventArgs e)
        {
            Reset();
        }

        private void OnVolumeNotification(AudioVolumeNotificationData data)
        {
            Dispatcher.Invoke(() =>
            {
                StartVolumeSlider.Value = (int) (data.MasterVolume * 100);
            });
        }

        private int MasterVolume
        {
            get
            {
                return (int) (_mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            set
            {
                _mmDevice.AudioEndpointVolume.MasterVolumeLevelScalar = (value / 100.0f);
            }
        }

        private readonly MMDevice _mmDevice;
        private TimeSpan _timeElapsed;
        private TimeSpan _timeRemaining;
        private int _startVolume;
        private bool _running;

        private Timer _stopwatchTimer;
        private Timer _delayTimer;
        private Timer _fadeTimer;

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
