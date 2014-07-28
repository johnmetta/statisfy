using System;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;

namespace Statsify.Aggregator.Configuration
{
    public class EnumConfigurationConverter<T> : ConfigurationConverterBase
    {
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return value == null
                 ? null
                 : Enum.Parse(typeof(T), value.ToString(), true);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext ctx, Type type)
        {
            return type == typeof(string);
        }
    }
}