
using CoreAudioApi;

namespace Decelerate
{
    class AudioController
    {
        public AudioController()
        {
            var deviceEnumerator = new MMDeviceEnumerator();

            _mmDevice = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);

            _mmDevice.AudioEndpointVolume.OnVolumeNotification += OnVolumeNotification;
        }

        /// <summary>
        /// Gets or sets volume as an integer percentage
        /// </summary>
        public int MasterVolume
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

        private void OnVolumeNotification(AudioVolumeNotificationData data)
        {
            
        }

        private readonly MMDevice _mmDevice;
    }
}
