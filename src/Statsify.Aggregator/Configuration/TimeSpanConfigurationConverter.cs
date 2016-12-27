using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace Statsify.Aggregator.Configuration
{
    public class TimeSpanConfigurationConverter : ConfigurationConverterBase
    {
        public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
        {
            return type == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return RetentionConfigurationElement.ParseTimeSpan((string)value);
        }
    }
}