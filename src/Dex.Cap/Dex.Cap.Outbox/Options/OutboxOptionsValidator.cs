using System;
using Microsoft.Extensions.Options;

namespace Dex.Cap.Outbox.Options
{
    public class OutboxOptionsValidator : IValidateOptions<OutboxOptions>
    {
        public ValidateOptionsResult Validate(string name, OutboxOptions options)
        {
            if (options == null)
            {
                return ValidateOptionsResult.Fail("Configuration object is null.");
            }

            if (options.Retries is <= 0 or > 100)
            {
                return ValidateOptionsResult.Fail("Retries should be greater than 0 and less than 100");
            }

            if (options.ProcessorDelay < TimeSpan.FromSeconds(1) || options.ProcessorDelay > TimeSpan.FromHours(1))
            {
                return ValidateOptionsResult.Fail("ProcessorDelay should be greater or equal 1 second and less than 1 hour");
            }

            if (options.MessagesToProcess is <= 0 or > 10_000)
            {
                return ValidateOptionsResult.Fail("MessagesToProcess should be greater than 0 and less than 10_000");
            }

            return ValidateOptionsResult.Success;
        }
    }
}