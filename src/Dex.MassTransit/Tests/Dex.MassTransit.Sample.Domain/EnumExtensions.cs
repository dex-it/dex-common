using System;

namespace Dex.MassTransit.Sample.Domain
{
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