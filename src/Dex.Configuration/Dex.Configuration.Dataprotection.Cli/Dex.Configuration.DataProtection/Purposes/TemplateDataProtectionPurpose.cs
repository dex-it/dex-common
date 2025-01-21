using System;
using System.Collections.Generic;
using Dex.Configuration.DataProtection.ApplicationNames;

namespace Dex.Configuration.DataProtection.Purposes
{
    public sealed class TemplateDataProtectionPurpose : DataProtectionPurpose<TemplateApplicationName>
    {
        protected override Dictionary<TemplateApplicationName, Tuple<string, string>> Constants { get; } =
            new Dictionary<TemplateApplicationName, Tuple<string, string>>
            {
                {
                    TemplateApplicationName.AuditWriter,
                    new Tuple<string, string>("9618B659-35F4-4945-8490-DCE303150ED9",
                        "4FE46270-2B01-4FCF-87AD-2F651A86F4EA")
                }
            };

        protected override string CalculateSalt(TemplateApplicationName applicationName,
            Tuple<string, string> constantValues)
        {
            switch (applicationName)
            {
                case TemplateApplicationName.AuditWriter:
                    return constantValues.Item1.Substring(3, 5) + constantValues.Item2.Substring(16, 7);
                default:
                    throw new InvalidOperationException(ApplicationNameWrong);
            }
        }
    }
}