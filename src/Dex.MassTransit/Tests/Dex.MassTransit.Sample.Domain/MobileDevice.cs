using System;
using System.Text.Json.Serialization;

namespace Dex.MassTransit.Sample.Domain
{
    public readonly struct MobileDevice
    {
        [JsonConstructor]
        public MobileDevice(MobilePlatform mobilePlatform, string deviceToken)
        {
            if (string.IsNullOrEmpty(deviceToken))
            {
                throw new ArgumentOutOfRangeException(nameof(deviceToken));
            }

            MobilePlatform = mobilePlatform.Validate();
            DeviceToken = deviceToken;
        }

        public MobilePlatform MobilePlatform { get; }

        public string DeviceToken { get; }
    }
}