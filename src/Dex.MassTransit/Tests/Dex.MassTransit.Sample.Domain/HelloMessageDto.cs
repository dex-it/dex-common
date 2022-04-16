using System;
using System.Text.Json.Serialization;
using MassTransit;

namespace Dex.MassTransit.Sample.Domain
{
    public class HelloMessageDto : IConsumer
    {
        public string Hi { get; set; }
        
        public Uri TestUri { get; set; }
        
        public MobileDevice[]? Devices {get; set; }
        
        // for example
        public MobileDevice? SingleDevice { get; set; }
    }
    
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
    
    public enum MobilePlatform
    {
        None,
        Android,
        IOS,
        Huawei,
    }

    public static class EnumExtensions
    {
        public static MobilePlatform Validate(this MobilePlatform mobilePlatform)
        {
            if (mobilePlatform != MobilePlatform.None && Enum.IsDefined(mobilePlatform))
            {
                return mobilePlatform;
            }

            throw new ArgumentOutOfRangeException(nameof(mobilePlatform));
        }
    }
}